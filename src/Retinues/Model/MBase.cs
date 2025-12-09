using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Retinues.Model
{
    public abstract class MBase<TBase>(TBase baseInstance)
        where TBase : class
    {
        /// <summary>
        /// Attribute cache for this wrapper.
        /// </summary>
        protected readonly Dictionary<string, object> _attributes = [];

        /// <summary>
        /// The underlying base instance.
        /// </summary>
        public TBase Base { get; } =
            baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));

        /// <summary>
        /// Gets or creates an attribute that gets/sets the value of a field or property
        /// on the underlying base instance.
        /// </summary>
        protected MAttribute<T> Attribute<T>(string name)
        {
            if (!_attributes.TryGetValue(name, out var obj))
            {
                var attr = new MAttribute<T>(Base, name);
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
        /// Gets or creates an attribute that gets/sets the value of a field or property
        /// on the underlying base instance, using the given expression to identify the member.
        /// </summary>
        protected MAttribute<TProp> Attribute<TProp>(Expression<Func<TBase, TProp>> expr)
        {
            if (expr.Body is not MemberExpression member)
                throw new ArgumentException(
                    "Expression must be a simple member access.",
                    nameof(expr)
                );

            var name = member.Member.Name;

            if (_attributes.TryGetValue(name, out var existing))
                return (MAttribute<TProp>)existing;

            // compile getter
            var typedGetter = expr.Compile();
            Func<object, TProp> getter = obj => typedGetter((TBase)obj);

            // compile setter if possible
            Action<object, TProp> setter = (_, _) =>
            {
                throw new InvalidOperationException($"Member '{name}' is read-only.");
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

            var attr = new MAttribute<TProp>(Base, getter, setter, name);
            _attributes[name] = attr;
            return attr;
        }

        /// <summary>
        /// Returns a string representation of this wrapper.
        /// </summary>
        public override string ToString()
        {
            return $"{GetType().Name}({Base})";
        }
    }
}
