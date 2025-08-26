using System;
using System.Reflection;

namespace CustomClanTroops.Utils
{
    public static class Reflector
    {
        public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static PropertyInfo P<T>(T instance, string propertyName)
        {
            return typeof(T).GetProperty(propertyName, Flags);
        }

        internal static FieldInfo F<T>(T instance, string fieldName)
        {
            return typeof(T).GetField(fieldName, Flags);
        }

        internal static MethodInfo M<T>(T instance, string methodName, params Type[] parameterTypes)
        {
            return typeof(T).GetMethod(methodName, Flags, null, parameterTypes, null);
        }
    }
}