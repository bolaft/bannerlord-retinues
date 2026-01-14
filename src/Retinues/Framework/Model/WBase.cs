using System;
using System.Collections.Generic;
using Retinues.Framework.Runtime;
using TaleWorlds.ObjectSystem;

namespace Retinues.Framework.Model
{
    /// <summary>
    /// Base wrapper for MBObjectBase types with MBObjectManager helpers and identity equality.
    /// </summary>
    public abstract class WBase<TWrapper, TBase>(TBase baseInstance) : MBase<TBase>(baseInstance)
        where TWrapper : WBase<TWrapper, TBase>
        where TBase : MBObjectBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Identifier                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string StringId => Base.StringId;
        public string UniqueId => $"{Base.GetType().FullName}:{StringId}";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Dirty                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsDirty
        {
            get
            {
                foreach (var obj in _attributes.Values)
                {
                    if (obj is IMAttribute attr && attr.IsDirty)
                        return true;
                }

                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Cache                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static readonly object CacheSync = new();
        static readonly Dictionary<string, TWrapper> Cache = new(StringComparer.Ordinal);

        [StaticClearAction]
        public static void ClearCache()
        {
            lock (CacheSync)
                Cache.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Static Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static readonly Dictionary<Type, Func<string, MBObjectBase>> Resolvers = [];

        public static void RegisterResolver<T>(Func<string, T> resolver)
            where T : MBObjectBase
        {
            Resolvers[typeof(T)] = id => resolver(id);
        }

        public static TWrapper Get(string stringId) => Wrap(GetBase(stringId));

        public static TWrapper Get(TBase mbObject) => Wrap(mbObject);

        /// <summary>
        /// Returns the underlying MBObjectBase from MBObjectManager by StringId.
        /// </summary>
        private static TBase GetBase(string stringId)
        {
            if (stringId == null)
                return null;

            if (Resolvers.TryGetValue(typeof(TBase), out var r))
                return r(stringId) as TBase;

            var manager = MBObjectManager.Instance;
            if (manager == null)
                return null;

            // Fast path: works for most static MBObjects loaded from XML
            var obj = manager.GetObject<TBase>(stringId);
            if (obj != null)
                return obj;

            // Fallback: campaign/runtime objects (MobileParty, Hero, etc.) are often not in the id dictionary,
            // but they do exist in the type list.
            var list = manager.GetObjectTypeList<TBase>();
            if (list == null)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                if (o != null && o.StringId == stringId)
                    return o;
            }

            return null;
        }

        /// <summary>
        /// Returns all objects of the underlying type from MBObjectManager, wrapped.
        /// </summary>
        public static IEnumerable<TWrapper> All
        {
            get
            {
                var manager = MBObjectManager.Instance;
                if (manager == null)
                    yield break;

                var list = manager.GetObjectTypeList<TBase>();
                if (list == null)
                    yield break;

                for (int i = 0; i < list.Count; i++)
                {
                    var obj = list[i];
                    if (obj == null)
                        continue;

                    var wrapper = Wrap(obj);
                    if (wrapper != null)
                        yield return wrapper;
                }
            }
        }

        /// <summary>
        /// Internal helper to construct the concrete wrapper around a base instance.
        /// Uses the concrete wrapper constructor W(TBase).
        /// </summary>
        static TWrapper Wrap(TBase baseInstance)
        {
            if (baseInstance == null)
                return null;

            var id = baseInstance.StringId;
            if (string.IsNullOrEmpty(id))
                return (TWrapper)
                    Activator.CreateInstance(typeof(TWrapper), new object[] { baseInstance });

            lock (CacheSync)
            {
                if (Cache.TryGetValue(id, out var existing))
                    return existing;

                var created = (TWrapper)
                    Activator.CreateInstance(typeof(TWrapper), new object[] { baseInstance });

                Cache[id] = created;
                return created;
            }
        }
    }
}
