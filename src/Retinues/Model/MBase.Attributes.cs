using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model
{
    public interface IModel
    {
        object Base { get; }
    }

    /// <summary>
    /// Non-generic attribute runtime interface used for dependency wiring.
    /// </summary>
    public interface IMAttribute
    {
        string Name { get; }
        void AddDependent(IMAttribute dependent);
        void MarkDirty();
    }

    public abstract partial class MBase<TBase> : IModel
        where TBase : class
    {
        // Pending dependents: key = target attribute name, value = list of dependents waiting
        readonly Dictionary<string, List<IMAttribute>> _pendingDependents = new(
            StringComparer.Ordinal
        );

        /// <summary>
        /// The underlying base instance.
        /// </summary>
        object IModel.Base => Base;

        /// <summary>
        /// Dictionary of attributes by name.
        /// </summary>
        protected readonly Dictionary<string, object> _attributes = new(StringComparer.Ordinal);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Factory Helper                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets or creates an attribute with the given name using the provided factory.
        /// </summary>
        MAttribute<T> GetOrCreateAttribute<T>(string name, Func<MAttribute<T>> create)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_attributes.TryGetValue(name, out var obj))
            {
                if (obj is not MAttribute<T> typed)
                    throw new InvalidOperationException(
                        $"Attribute '{name}' already exists with a different type ({obj.GetType()})."
                    );
                return typed;
            }

            var attr = create();
            _attributes[name] = attr;

            // If any other attributes previously requested to depend on this one, wire them now.
            if (_pendingDependents.TryGetValue(name, out var list))
            {
                if (attr is IMAttribute thisAttr)
                {
                    foreach (var dep in list)
                    {
                        try
                        {
                            thisAttr.AddDependent(dep);
                        }
                        catch { }
                    }
                }
                _pendingDependents.Remove(name);
            }

            return attr;
        }

        void RegisterDependencyNames(IMAttribute dependent, params string[] dependsOn)
        {
            if (dependsOn == null || dependsOn.Length == 0 || dependent == null)
                return;

            foreach (var target in dependsOn)
            {
                if (string.IsNullOrEmpty(target))
                    continue;

                if (_attributes.TryGetValue(target, out var obj) && obj is IMAttribute targetAttr)
                {
                    try
                    {
                        targetAttr.AddDependent(dependent);
                    }
                    catch { }

                    // If the dependency is already dirty, the dependent must be dirty too.
                    try
                    {
                        var dirtyProp = obj.GetType()
                            .GetProperty("IsDirty", BindingFlags.Instance | BindingFlags.Public);

                        if (dirtyProp?.GetValue(obj) is bool b && b)
                            dependent.MarkDirty();
                    }
                    catch { }
                }
                else
                {
                    if (!_pendingDependents.TryGetValue(target, out var list))
                    {
                        list = new List<IMAttribute>();
                        _pendingDependents[target] = list;
                    }
                    list.Add(dependent);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Factories                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets or creates an attribute that targets a field/property on the underlying base instance.
        /// </summary>
        protected MAttribute<T> Attribute<T>(
            string targetName,
            bool persistent = true,
            AttributePriority priority = AttributePriority.Medium,
            string[] dependsOn = null,
            [CallerMemberName] string name = null
        )
        {
            var attr = GetOrCreateAttribute(
                name ?? throw new ArgumentNullException(nameof(name)),
                () => new MAttribute<T>(this, name, targetName ?? name, persistent, priority)
            );

            if (attr is IMAttribute im)
                RegisterDependencyNames(im, dependsOn);

            return attr;
        }

        /// <summary>
        /// Gets or creates an attribute that gets/sets the value of a field or property
        /// on the underlying base instance, using the given expression to identify the member.
        /// </summary>
        protected MAttribute<TProp> Attribute<TProp>(
            Expression<Func<TBase, TProp>> expr,
            bool persistent = true,
            AttributePriority priority = AttributePriority.Medium,
            string[] dependsOn = null,
            [CallerMemberName] string name = null
        )
        {
            if (expr.Body is not MemberExpression member)
                throw new ArgumentException(
                    "Expression must be a simple member access.",
                    nameof(expr)
                );

            var targetName = member.Member.Name;

            var typedGetter = expr.Compile();
            TProp getter(object obj) => typedGetter((TBase)obj);

            Action<object, TProp> setter = (_, _) =>
                throw new InvalidOperationException($"Member '{targetName}' is read-only.");

            if (member.Member is System.Reflection.PropertyInfo prop && prop.CanWrite)
            {
                var instanceParam = Expression.Parameter(typeof(TBase), "instance");
                var valueParam = Expression.Parameter(typeof(TProp), "value");
                var castValue = Expression.Convert(valueParam, prop.PropertyType);
                var call = Expression.Call(instanceParam, prop.GetSetMethod(true), castValue);
                var typedSetter = Expression
                    .Lambda<Action<TBase, TProp>>(call, instanceParam, valueParam)
                    .Compile();
                setter = (obj, value) => typedSetter((TBase)obj, value);
            }
            else if (member.Member is System.Reflection.FieldInfo field)
            {
                var instanceParam = Expression.Parameter(typeof(TBase), "instance");
                var valueParam = Expression.Parameter(typeof(TProp), "value");
                var castValue = Expression.Convert(valueParam, field.FieldType);
                var assign = Expression.Assign(Expression.Field(instanceParam, field), castValue);
                var typedSetter = Expression
                    .Lambda<Action<TBase, TProp>>(assign, instanceParam, valueParam)
                    .Compile();
                setter = (obj, value) => typedSetter((TBase)obj, value);
            }

            var attr = GetOrCreateAttribute(
                name,
                () => new MAttribute<TProp>(this, name, getter, setter, persistent, priority)
            );

            if (attr is IMAttribute im)
                RegisterDependencyNames(im, dependsOn);

            return attr;
        }

        /// <summary>
        /// Gets or creates an attribute that gets/sets the value of a field or property
        /// on the underlying base instance, using the given delegates.
        /// </summary>
        protected MAttribute<T> Attribute<T>(
            Func<object, T> getter,
            Action<object, T> setter,
            bool persistent = true,
            AttributePriority priority = AttributePriority.Medium,
            string[] dependsOn = null,
            [CallerMemberName] string name = null
        )
        {
            var attr = GetOrCreateAttribute(
                name ?? throw new ArgumentNullException(nameof(name)),
                () => new MAttribute<T>(this, name, getter, setter, persistent, priority)
            );

            if (attr is IMAttribute im)
                RegisterDependencyNames(im, dependsOn);

            return attr;
        }

        /// <summary>
        /// Creates a stored attribute that lives inside the wrapper.
        /// </summary>
        protected MAttribute<T> Attribute<T>(
            T initialValue = default,
            bool persistent = true,
            AttributePriority priority = AttributePriority.Medium,
            string[] dependsOn = null,
            [CallerMemberName] string name = null
        )
        {
            if (Base is not MBObjectBase mbo)
                throw new InvalidOperationException(
                    "Stored attributes are only supported on WBase wrappers."
                );

            var storeKey = $"{GetType().FullName}:{mbo.StringId}:{name}";
            var attr = GetOrCreateAttribute(
                name,
                () =>
                    new MAttribute<T>(
                        this,
                        name,
                        () => MAttribute<T>.Store.GetOrInit(storeKey, initialValue),
                        value => MAttribute<T>.Store.Set(storeKey, value),
                        persistent,
                        priority
                    )
            );

            if (attr is IMAttribute im)
                RegisterDependencyNames(im, dependsOn);

            return attr;
        }
    }
}
