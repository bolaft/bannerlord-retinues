using System;
using System.Globalization;
using System.Reflection;

namespace Retinues.Core.Utils
{
    /// <summary>
    /// Reflection utility for accessing properties, fields, and methods on objects.
    /// Handles private, protected, and inherited members, with type conversion and common backing field patterns.
    /// </summary>
    public static class Reflector
    {
        public const BindingFlags Flags =
            BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.FlattenHierarchy;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Core Resolvers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static PropertyInfo ResolveProperty(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var pi = t.GetProperty(name, Flags);
                if (pi != null)
                    return pi;
            }
            return null;
        }

        private static FieldInfo ResolveField(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var fi = t.GetField(name, Flags);
                if (fi != null)
                    return fi;
            }
            return null;
        }

        private static MethodInfo ResolveMethod(Type type, string name, Type[] parameterTypes)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var mi =
                    parameterTypes != null
                        ? t.GetMethod(
                            name,
                            Flags,
                            binder: null,
                            types: parameterTypes,
                            modifiers: null
                        )
                        : t.GetMethod(name, Flags);
                if (mi != null)
                    return mi;
            }
            return null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the value of a property by name, with type conversion.
        /// Throws if the property or getter is missing.
        /// </summary>
        public static TReturn GetPropertyValue<TReturn>(object instance, string propertyName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            var type = instance.GetType();
            var pi =
                ResolveProperty(type, propertyName)
                ?? throw new MissingMemberException(type.FullName, propertyName);

            var getter =
                pi.GetGetMethod(true)
                ?? throw new MissingMethodException(
                    $"{type.FullName}.{propertyName} has no getter."
                );
            var val = getter.Invoke(instance, null);
            return (TReturn)ConvertIfNeeded(val, typeof(TReturn));
        }

        /// <summary>
        /// Gets the value of a property by name (untyped).
        /// Throws if the property or getter is missing.
        /// </summary>
        public static object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            var type = instance.GetType();
            var pi =
                ResolveProperty(type, propertyName)
                ?? throw new MissingMemberException(type.FullName, propertyName);

            var getter =
                pi.GetGetMethod(true)
                ?? throw new MissingMethodException(
                    $"{type.FullName}.{propertyName} has no getter."
                );
            return getter.Invoke(instance, null);
        }

        /// <summary>
        /// Sets the value of a property by name, using setter or common backing field names.
        /// Throws if neither setter nor backing field is found.
        /// </summary>
        public static void SetPropertyValue(object instance, string propertyName, object value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var pi =
                ResolveProperty(type, propertyName)
                ?? throw new MissingMemberException(type.FullName, propertyName);

            var targetType = pi.PropertyType;
            var converted = ConvertIfNeeded(value, targetType);

            // 1) Try setter, including non-public
            var setter = pi.GetSetMethod(true);
            if (setter != null)
            {
                setter.Invoke(instance, [converted]);
                return;
            }

            // 2) Fallback to common backing-field names
            var lc = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
            var candidates = new[]
            {
                $"<{propertyName}>k__BackingField",
                "_" + lc,
                "_" + propertyName,
                "m_" + lc,
                lc,
                propertyName,
            };

            foreach (var name in candidates)
            {
                var fi = ResolveField(type, name);
                if (fi != null)
                {
                    var fieldVal = ConvertIfNeeded(converted, fi.FieldType);
                    fi.SetValue(instance, fieldVal);
                    return;
                }
            }

            throw new MissingMethodException(
                $"{type.FullName}.{propertyName} has no setter and no recognizable backing field was found."
            );
        }

        /// <summary>
        /// Gets the value of a field by name, with type conversion.
        /// Throws if the field is missing.
        /// </summary>
        public static TReturn GetFieldValue<TReturn>(object instance, string fieldName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            var fi =
                ResolveField(instance.GetType(), fieldName)
                ?? throw new MissingMemberException(instance.GetType().FullName, fieldName);
            var val = fi.GetValue(instance);
            return (TReturn)ConvertIfNeeded(val, typeof(TReturn));
        }

        /// <summary>
        /// Sets the value of a field by name, with type conversion.
        /// Throws if the field is missing.
        /// </summary>
        public static void SetFieldValue(object instance, string fieldName, object value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            var fi =
                ResolveField(instance.GetType(), fieldName)
                ?? throw new MissingMemberException(instance.GetType().FullName, fieldName);
            fi.SetValue(instance, ConvertIfNeeded(value, fi.FieldType));
        }

        /// <summary>
        /// Invokes a method by name, with optional parameter types and arguments.
        /// Throws if the method is missing.
        /// </summary>
        public static object InvokeMethod(
            object instance,
            string methodName,
            Type[] parameterTypes,
            params object[] args
        )
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            var type = instance.GetType();

            MethodInfo mi;
            if (parameterTypes == null)
            {
                var inferred =
                    args?.Length > 0
                        ? Array.ConvertAll(args, a => a?.GetType() ?? typeof(object))
                        : Type.EmptyTypes;
                mi =
                    ResolveMethod(type, methodName, inferred)
                    ?? ResolveMethod(type, methodName, null)
                    ?? throw new MissingMethodException(type.FullName, methodName);
            }
            else
            {
                mi =
                    ResolveMethod(type, methodName, parameterTypes)
                    ?? throw new MissingMethodException(type.FullName, methodName);
            }

            return mi.Invoke(instance, args);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static object ConvertIfNeeded(object value, Type targetType)
        {
            if (targetType == typeof(void))
                return null;

            if (value == null)
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                    return null;
                // For non-nullable value types, let reflection throw later if needed
                return Activator.CreateInstance(targetType);
            }

            var vType = value.GetType();
            if (targetType.IsAssignableFrom(vType))
                return value;

            // Nullable<T>
            var underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
                return ConvertIfNeeded(value, underlying);

            // Enums
            if (targetType.IsEnum)
            {
                if (vType == typeof(string))
                    return Enum.Parse(targetType, (string)value, ignoreCase: true);
                return Enum.ToObject(
                    targetType,
                    Convert.ChangeType(
                        value,
                        Enum.GetUnderlyingType(targetType),
                        CultureInfo.InvariantCulture
                    )
                );
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }
}
