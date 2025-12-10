using System;
using System.Collections.Generic;

namespace Retinues.Utilities
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                          Cache                         //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        void ICacheGroupEntry.ClearLocal()
        {
            ClearLocal();
        }

        internal void ClearLocal()
        {
            _hasValue = false;
            _value = default;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                      CacheRegistry                     //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    internal static class CacheRegistry
    {
        private sealed class Entry
        {
            public readonly WeakReference<ICacheGroupEntry> Reference;

            public Entry(ICacheGroupEntry entry)
            {
                Reference = new WeakReference<ICacheGroupEntry>(entry);
            }
        }

        private static readonly Dictionary<string, List<Entry>> _groups =
            new Dictionary<string, List<Entry>>();

        private static readonly object _lock = new object();

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
                    list = new List<Entry>();
                    _groups[key] = list;
                }

                list.Add(new Entry(entry));
            }
        }

        public static void ClearGroup(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            lock (_lock)
            {
                if (!_groups.TryGetValue(key, out var list))
                {
                    return;
                }

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
                {
                    _groups.Remove(key);
                }
            }
        }
    }
}
