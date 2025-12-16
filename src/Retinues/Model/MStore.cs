using System;
using System.Collections.Generic;

namespace Retinues.Model
{
    internal static class MStore
    {
        private static readonly object Sync = new();
        private static readonly Dictionary<string, object> Values = new(StringComparer.Ordinal);

        public static T GetOrInit<T>(string key, T initialValue)
        {
            lock (Sync)
            {
                if (Values.TryGetValue(key, out var obj))
                {
                    if (obj is T typed)
                        return typed;

                    throw new InvalidOperationException(
                        $"Stored attribute '{key}' already exists with different type ({obj?.GetType().FullName})."
                    );
                }

                Values[key] = initialValue;
                return initialValue;
            }
        }

        public static void Set<T>(string key, T value)
        {
            lock (Sync)
            {
                Values[key] = value;
            }
        }

        public static void ClearAll()
        {
            lock (Sync)
            {
                Values.Clear();
            }
        }
    }
}
