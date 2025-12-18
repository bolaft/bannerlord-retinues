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
        /// Persistence uses the wrapper property name.
        /// </summary>
        protected MAttribute<T> Attribute<T>(
            string targetName,
            bool persistent = false,
            bool serializable = false,
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
                    serializable: serializable,
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
            bool serializable = false,
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
                serializable: serializable,
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
            bool serializable = false,
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
                serializable: serializable,
                priority: priority,
                serializer: serializer
            );

            _attributes[name] = attr;
            return attr;
        }

        /// <summary>
        /// Serializes this instance into an XML dictionary (name -> payload).
        /// Only includes attributes with IsSerializable=true unless includeNonSerializable=true.
        /// </summary>
        public string Serialize(bool includeNonSerializable = false)
        {
            var data = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var kvp in _attributes)
            {
                var name = kvp.Key;
                var obj = kvp.Value;

                if (obj is not IPersistentAttribute attr)
                    continue;

                if (!includeNonSerializable && !attr.IsSerializable)
                    continue;

                // Payload only (no pv envelope).
                data[name] = attr.Serialize();
            }

            return Serialization.SerializeDictionary(data);
        }

        /// <summary>
        /// Deserializes an XML dictionary produced by Serialize().
        /// Only applies attributes with IsSerializable=true.
        /// </summary>
        public void Deserialize(string xml, bool clearDirty = true)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return;

            var data = Serialization.DeserializeDictionary(xml);
            if (data == null || data.Count == 0)
                return;

            using (MPersistence.BeginApplying())
            {
                foreach (var kvp in data)
                {
                    var name = kvp.Key;
                    var payload = kvp.Value;

                    if (!_attributes.TryGetValue(name, out var obj))
                        continue;

                    if (obj is not IPersistentAttribute attr)
                        continue;

                    if (!attr.IsSerializable)
                        continue;

                    attr.ApplySerialized(payload);

                    if (clearDirty)
                        attr.ClearDirty();
                }
            }
        }
    }
}
