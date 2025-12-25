using System;
using System.Collections.Generic;
using Retinues.Utilities;

namespace Retinues.Model
{
    public partial class MAttribute<T>
    {
        internal static class Store
        {
            private static readonly object Sync = new();
            private static readonly Dictionary<string, object> Values = new(StringComparer.Ordinal);

            public static TValue GetOrInit<TValue>(string key, TValue initialValue)
            {
                lock (Sync)
                {
                    if (Values.TryGetValue(key, out var obj))
                    {
                        if (obj is TValue typed)
                            return typed;

                        throw new InvalidOperationException(
                            $"Stored attribute '{key}' already exists with different type ({obj?.GetType().FullName})."
                        );
                    }

                    Values[key] = initialValue;
                    return initialValue;
                }
            }

            public static void Set<TValue>(string key, TValue value)
            {
                lock (Sync)
                {
                    Values[key] = value;
                }
            }

            [StaticClearAction]
            public static void ClearAll()
            {
                lock (Sync)
                {
                    Values.Clear();
                }
            }
        }
    }
}
