using System;
using System.Collections.Generic;
using Retinues.Framework.Runtime;

namespace Retinues.Framework.Model.Attributes
{
    public partial class MAttribute<T>
    {
        internal static class Store
        {
            private static readonly object Sync = new();

            // We must keep type information even when the value is null, otherwise future GetOrInit()
            // calls cannot safely validate type and will throw.
            private sealed class Entry
            {
                public Type Type;
                public object Value;

                public Entry(Type type, object value)
                {
                    Type = type;
                    Value = value;
                }
            }

            private static readonly Dictionary<string, Entry> Values = new(StringComparer.Ordinal);

            public static TValue GetOrInit<TValue>(string key, TValue initialValue)
            {
                lock (Sync)
                {
                    var expected = typeof(TValue);

                    if (Values.TryGetValue(key, out var entry))
                    {
                        // If the stored type is missing or was previously "poisoned" by ValueTuple,
                        // treat it as untyped and upgrade it to the expected type.
                        if (entry.Type == null || entry.Type == typeof(ValueTuple))
                        {
                            entry.Type = expected;

                            // Some old paths stored boxed ValueTuple instead of null/default.
                            if (entry.Value is ValueTuple)
                                entry.Value = default(TValue);

                            return entry.Value is TValue tv ? tv : default;
                        }

                        if (entry.Type != expected)
                        {
                            throw new InvalidOperationException(
                                $"Stored attribute '{key}' already exists with different type ({entry.Type.FullName})."
                            );
                        }

                        // Null is valid for reference types; return default(TValue) in that case.
                        if (entry.Value == null)
                            return default;

                        return (TValue)entry.Value;
                    }

                    // Always record the expected type even when initialValue is null.
                    Values[key] = new Entry(expected, initialValue);
                    return initialValue;
                }
            }

            public static void Set<TValue>(string key, TValue value)
            {
                lock (Sync)
                {
                    var expected = typeof(TValue);

                    if (Values.TryGetValue(key, out var entry))
                    {
                        // Upgrade untyped entries.
                        if (entry.Type == null || entry.Type == typeof(ValueTuple))
                        {
                            entry.Type = expected;
                            entry.Value = value;
                            return;
                        }

                        // If someone tries to set with a different TValue than originally declared,
                        // keep the old behavior (throw) to catch bugs early.
                        if (entry.Type != expected)
                        {
                            throw new InvalidOperationException(
                                $"Stored attribute '{key}' already exists with different type ({entry.Type.FullName})."
                            );
                        }

                        entry.Value = value;
                        return;
                    }

                    Values[key] = new Entry(expected, value);
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
