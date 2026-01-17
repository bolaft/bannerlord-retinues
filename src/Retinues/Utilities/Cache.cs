using System;
using System.Collections.Generic;

namespace Retinues.Utilities
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                          Cache                         //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Interface for cache entries that can be cleared by group.
    /// </summary>
    internal interface ICacheGroupEntry
    {
        void ClearLocal();
    }

    /// <summary>
    /// Simple cache of a computed value with optional key-based group clearing.
    /// </summary>
    public sealed class Cache<TOwner, TValue> : ICacheGroupEntry
    {
        private readonly Func<TOwner, TValue> _factory;
        private readonly string _key;

        private bool _hasValue;
        private TValue _value;

        public Cache(Func<TOwner, TValue> factory)
            : this(factory, null) { }

        public Cache(Func<TOwner, TValue> factory, string key)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _key = key;

            if (!string.IsNullOrEmpty(_key))
            {
                CacheRegistry.Register(_key, this);
            }
        }

        /// <summary>
        /// Get the cached value, computing it if needed.
        /// </summary>
        public TValue Get(TOwner owner)
        {
            if (!_hasValue)
            {
                _value = _factory(owner);
                _hasValue = true;
            }

            return _value;
        }

        /// <summary>
        /// Explicitly set the cached value.
        /// </summary>
        public void Set(TValue value)
        {
            _value = value;
            _hasValue = true;
        }

        /// <summary>
        /// Clear this cache. If a key was provided at construction,
        /// all caches sharing that key are cleared.
        /// </summary>
        public void Clear()
        {
            if (string.IsNullOrEmpty(_key))
            {
                ClearLocal();
            }
            else
            {
                CacheRegistry.ClearGroup(_key);
            }
        }

        /// <summary>
        /// Clear only this cache's local value.
        /// </summary>
        void ICacheGroupEntry.ClearLocal() => ClearLocal();

        /// <summary>
        /// Clear only this cache's local value.
        /// </summary>
        internal void ClearLocal()
        {
            _hasValue = false;
            _value = default;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                     Cache Registry                     //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Registry for cache entries grouped by key for coordinated clearing.
    /// </summary>
    internal static class CacheRegistry
    {
        /// <summary>
        /// Wrapper for weak references to cache entries.
        /// </summary>
        /// <param name="entry"></param>
        private sealed class Entry(ICacheGroupEntry entry)
        {
            public readonly WeakReference<ICacheGroupEntry> Reference = new(entry);
        }

        private static readonly Dictionary<string, List<Entry>> _groups = [];

        private static readonly object _lock = new();

        /// <summary>
        /// Register a cache entry under a group key for coordinated clearing.
        /// </summary>
        public static void Register(string key, ICacheGroupEntry entry)
        {
            if (string.IsNullOrEmpty(key) || entry == null)
            {
                return;
            }

            lock (_lock)
            {
                if (!_groups.TryGetValue(key, out var list))
                {
                    list = [];
                    _groups[key] = list;
                }

                list.Add(new Entry(entry));
            }
        }

        /// <summary>
        /// Clear all cache entries associated with the given group key.
        /// </summary>
        public static void ClearGroup(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            lock (_lock)
            {
                if (!_groups.TryGetValue(key, out var list))
                    return;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var entry = list[i];
                    if (!entry.Reference.TryGetTarget(out var target))
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    target.ClearLocal();
                }

                if (list.Count == 0)
                    _groups.Remove(key);
            }
        }
    }
}
