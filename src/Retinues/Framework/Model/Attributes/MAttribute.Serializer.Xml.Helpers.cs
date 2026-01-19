using System;
using System.Collections.Generic;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Retinues.Framework.Model.Attributes
{
    /// <summary>
    /// Serialization support for MAttribute<T>.
    /// </summary>
    public partial class MAttribute<T>
    {
        /// <summary>
        /// Tries to get the value type of a Dictionary with string keys.
        /// </summary>
        static bool TryGetStringKeyedDictionaryValueType(Type t, out Type valueType)
        {
            valueType = null;

            if (t == null || !t.IsGenericType)
                return false;

            if (t.GetGenericTypeDefinition() != typeof(Dictionary<,>))
                return false;

            var args = t.GetGenericArguments();
            if (args.Length != 2)
                return false;

            if (args[0] != typeof(string))
                return false;

            valueType = args[1];
            return true;
        }

        /// <summary>
        /// Serializes a scalar-like object to string.
        /// </summary>
        static string SerializeScalarLike(object obj, Type targetType)
        {
            if (obj == null)
                return string.Empty;

            if (targetType == typeof(string))
                return (string)obj;

            if (targetType == typeof(TextObject))
                return ((TextObject)obj)?.Value ?? string.Empty;

            if (targetType == typeof(bool))
                return ((bool)obj) ? "true" : "false";

            if (targetType == typeof(int))
                return ((int)obj).ToString(Inv);

            if (targetType == typeof(float))
                return ((float)obj).ToString("R", Inv);

            if (targetType == typeof(double))
                return ((double)obj).ToString("R", Inv);

            if (targetType.IsEnum)
                return obj.ToString();

            if (typeof(MBObjectBase).IsAssignableFrom(targetType))
                return ((MBObjectBase)obj)?.StringId ?? string.Empty;

            return Convert.ToString(obj, Inv) ?? string.Empty;
        }
    }
}
