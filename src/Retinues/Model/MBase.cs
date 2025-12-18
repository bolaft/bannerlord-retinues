using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Retinues.Utilities;

namespace Retinues.Model
{
    public interface IPersistent
    {
        string PersistenceKey { get; }
    }

    public abstract partial class MPersistent<TBase>(TBase baseInstance)
        : MBase<TBase>(baseInstance),
            IPersistent
        where TBase : class
    {
        public abstract string PersistenceKey { get; }
    }

    [SafeClass(IncludeDerived = true)]
    public abstract class MBase<TBase>(TBase baseInstance)
        where TBase : class
    {
        protected readonly Dictionary<string, object> _attributes = new(StringComparer.Ordinal);

        public TBase Base { get; } =
            baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));

        /// <summary>
        /// Gets or creates an attribute that targets a field/property on the underlying base instance.
        /// </summary>
        protected MAttribute<T> Attribute<T>(
            string targetName,
            bool persistent = false,
            MPersistencePriority priority = MPersistencePriority.Normal,
            MSerializer<T> serializer = null,
            [CallerMemberName] string name = null
        )
        {
            name ??= "<unknown>";

            if (!_attributes.TryGetValue(name, out var obj))
            {
                var ownerKey = (this as IPersistent)?.PersistenceKey;
                var attr = new MAttribute<T>(
                    baseInstance: Base,
                    persistenceName: name,
                    targetName: targetName,
                    ownerKey: ownerKey,
                    persistent: persistent,
                    priority: priority,
                    serializer: serializer
                );

                _attributes[name] = attr;
                return attr;
            }

            if (obj is not MAttribute<T> typed)
                throw new InvalidOperationException(
                    $"Attribute '{name}' already exists with a different type ({obj.GetType()})."
                );

            return typed;
        }

        /// <summary>
        /// Gets or creates an attribute that binds to a field/property on the underlying base instance,
        /// using the given expression to identify the target member. Persistence uses the wrapper property name.
        /// </summary>
        protected MAttribute<TProp> Attribute<TProp>(
            Expression<Func<TBase, TProp>> expr,
            bool persistent = false,
            MPersistencePriority priority = MPersistencePriority.Normal,
            MSerializer<TProp> serializer = null,
            [CallerMemberName] string name = null
        )
        {
            name ??= "<unknown>";

            if (expr.Body is not MemberExpression member)
                throw new ArgumentException(
                    "Expression must be a simple member access.",
                    nameof(expr)
                );

            var targetName = member.Member.Name;

            if (_attributes.TryGetValue(name, out var existing))
            {
                if (existing is not MAttribute<TProp> typed)
                    throw new InvalidOperationException(
                        $"Attribute '{name}' already exists with a different type ({existing.GetType()})."
                    );

                return typed;
            }

            var typedGetter = expr.Compile();
            TProp getter(object obj) => typedGetter((TBase)obj);

            Action<object, TProp> setter = (_, _) =>
            {
                throw new InvalidOperationException($"Member '{targetName}' is read-only.");
            };

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

            var ownerKey = (this as IPersistent)?.PersistenceKey;

            var attr = new MAttribute<TProp>(
                baseInstance: Base,
                getter: getter,
                setter: setter,
                persistenceName: name,
                targetName: targetName,
                ownerKey: ownerKey,
                persistent: persistent,
                priority: priority,
                serializer: serializer
            );

            _attributes[name] = attr;
            return attr;
        }

        /// <summary>
        /// Gets or creates an attribute that gets/sets via delegates.
        /// Persistence uses the wrapper property name.
        /// </summary>
        protected MAttribute<T> Attribute<T>(
            Func<object, T> getter,
            Action<object, T> setter,
            bool persistent = false,
            MPersistencePriority priority = MPersistencePriority.Normal,
            MSerializer<T> serializer = null,
            string targetName = null,
            [CallerMemberName] string name = null
        )
        {
            name ??= "<unknown>";
            targetName ??= name;

            if (_attributes.TryGetValue(name, out var obj))
            {
                if (obj is not MAttribute<T> typed)
                    throw new InvalidOperationException(
                        $"Attribute '{name}' already exists with a different type ({obj.GetType()})."
                    );

                return typed;
            }

            var ownerKey = (this as IPersistent)?.PersistenceKey;

            var attr = new MAttribute<T>(
                baseInstance: Base,
                getter: getter,
                setter: setter,
                persistenceName: name,
                targetName: targetName,
                ownerKey: ownerKey,
                persistent: persistent,
                priority: priority,
                serializer: serializer
            );

            _attributes[name] = attr;
            return attr;
        }
    }
}
