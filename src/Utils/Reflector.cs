using System;
using System.Globalization;
using System.Reflection;

namespace CustomClanTroops.Utils
{
    public static class Reflector
    {
        public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        // =========================================================================
        // Core Resolvers
        // =========================================================================

        private static PropertyInfo ResolveProperty(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var pi = t.GetProperty(name, Flags);
                if (pi != null) return pi;
            }
            return null;
        }

        private static FieldInfo ResolveField(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var fi = t.GetField(name, Flags);
                if (fi != null) return fi;
            }
            return null;
        }

        private static MethodInfo ResolveMethod(Type type, string name, Type[] parameterTypes)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var mi = parameterTypes != null
                    ? t.GetMethod(name, Flags, binder: null, types: parameterTypes, modifiers: null)
                    : t.GetMethod(name, Flags);
                if (mi != null) return mi;
            }
            return null;
        }

        // =========================================================================
        // Back-compat helpers
        // =========================================================================

        internal static PropertyInfo P<T>(T instance, string propertyName)
            => ResolveProperty(instance?.GetType() ?? typeof(T), propertyName);

        internal static FieldInfo F<T>(T instance, string fieldName)
            => ResolveField(instance?.GetType() ?? typeof(T), fieldName);

        internal static MethodInfo M<T>(T instance, string methodName, params Type[] parameterTypes)
            => ResolveMethod(instance?.GetType() ?? typeof(T), methodName, parameterTypes);

        // =========================================================================
        // Public API
        // =========================================================================

        public static TReturn GetPropertyValue<TReturn>(object instance, string propertyName)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var type = instance.GetType();
            var pi = ResolveProperty(type, propertyName)
                     ?? throw new MissingMemberException(type.FullName, propertyName);

            var getter = pi.GetGetMethod(true);
            if (getter == null)
                throw new MissingMethodException($"{type.FullName}.{propertyName} has no getter.");

            var val = getter.Invoke(instance, null);
            return (TReturn)ConvertIfNeeded(val, typeof(TReturn));
        }

        public static object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var type = instance.GetType();
            var pi = ResolveProperty(type, propertyName)
                     ?? throw new MissingMemberException(type.FullName, propertyName);

            var getter = pi.GetGetMethod(true);
            if (getter == null)
                throw new MissingMethodException($"{type.FullName}.{propertyName} has no getter.");

            return getter.Invoke(instance, null);
        }

        public static void SetPropertyValue(object instance, string propertyName, object value)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var pi = ResolveProperty(type, propertyName)
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
                propertyName
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
                $"{type.FullName}.{propertyName} has no setter and no recognizable backing field was found.");
        }

        public static TReturn GetFieldValue<TReturn>(object instance, string fieldName)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var fi = ResolveField(instance.GetType(), fieldName)
                     ?? throw new MissingMemberException(instance.GetType().FullName, fieldName);
            var val = fi.GetValue(instance);
            return (TReturn)ConvertIfNeeded(val, typeof(TReturn));
        }

        public static void SetFieldValue(object instance, string fieldName, object value)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var fi = ResolveField(instance.GetType(), fieldName)
                     ?? throw new MissingMemberException(instance.GetType().FullName, fieldName);
            fi.SetValue(instance, ConvertIfNeeded(value, fi.FieldType));
        }

        public static object InvokeMethod(object instance, string methodName, Type[] parameterTypes, params object[] args)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var type = instance.GetType();

            MethodInfo mi;
            if (parameterTypes == null)
            {
                var inferred = args?.Length > 0 ? Array.ConvertAll(args, a => a?.GetType() ?? typeof(object)) : Type.EmptyTypes;
                mi = ResolveMethod(type, methodName, inferred)
                     ?? ResolveMethod(type, methodName, null)
                     ?? throw new MissingMethodException(type.FullName, methodName);
            }
            else
            {
                mi = ResolveMethod(type, methodName, parameterTypes)
                     ?? throw new MissingMethodException(type.FullName, methodName);
            }

            return mi.Invoke(instance, args);
        }

        // =========================================================================
        // Helpers
        // =========================================================================

        private static object ConvertIfNeeded(object value, Type targetType)
        {
            if (targetType == typeof(void)) return null;

            if (value == null)
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                    return null;
                // For non-nullable value types, let reflection throw later if needed
                return Activator.CreateInstance(targetType);
            }

            var vType = value.GetType();
            if (targetType.IsAssignableFrom(vType)) return value;

            // Nullable<T>
            var underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
                return ConvertIfNeeded(value, underlying);

            // Enums
            if (targetType.IsEnum)
            {
                if (vType == typeof(string)) return Enum.Parse(targetType, (string)value, ignoreCase: true);
                return Enum.ToObject(targetType, Convert.ChangeType(value, Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture));
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }
}
