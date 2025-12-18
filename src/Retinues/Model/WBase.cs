using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Retinues.Utilities;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model
{
    /// <summary>
    /// Base wrapper for MBObjectBase types with MBObjectManager helpers and identity equality.
    /// </summary>
    public abstract class WBase<TWrapper, TBase>(TBase baseInstance)
        : MPersistent<TBase>(baseInstance),
            IEquatable<WBase<TWrapper, TBase>>
        where TWrapper : WBase<TWrapper, TBase>
        where TBase : MBObjectBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// StringId forwarded from the underlying MBObjectBase.
        /// </summary>
        public string StringId => Base.StringId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Persistence                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string PersistenceKey => $"{typeof(TWrapper).FullName}:{StringId}";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets or creates an attribute using the caller member name as the key.
        /// If Base has a field/property with that name, binds to it.
        /// Otherwise creates a stored attribute that lives inside the wrapper.
        /// </summary>
        protected MAttribute<T> Attribute<T>(
            T initialValue = default,
            bool storedIfMissing = true,
            bool persistent = false,
            MPersistencePriority priority = MPersistencePriority.Normal,
            MSerializer<T> serializer = null,
            string targetName = null,
            [CallerMemberName] string name = null
        )
        {
            name ??= "<unknown>";
            targetName ??= name;

            if (_attributes.TryGetValue(name, out var obj))
            {
                if (obj is MAttribute<T> typed)
                    return typed;

                throw new InvalidOperationException(
                    $"Attribute '{name}' already exists with a different type ({obj.GetType()})."
                );
            }

            bool hasMember =
                Reflection.HasField(Base, targetName) || Reflection.HasProperty(Base, targetName);

            MAttribute<T> attr =
                hasMember
                    ? new MAttribute<T>(
                        baseInstance: Base,
                        persistenceName: name,
                        targetName: targetName,
                        ownerKey: PersistenceKey,
                        persistent: persistent,
                        priority: priority,
                        serializer: serializer
                    )
                : storedIfMissing
                    ? new MAttribute<T>(
                        baseInstance: Base,
                        getter: _ => MStore.GetOrInit(BuildStoredKey<T>(name), initialValue),
                        setter: (_, value) => MStore.Set(BuildStoredKey<T>(name), value),
                        persistenceName: name,
                        targetName: targetName,
                        ownerKey: PersistenceKey,
                        persistent: persistent,
                        priority: priority,
                        serializer: serializer
                    )
                : throw new InvalidOperationException(
                    $"No field or property '{targetName}' exists on type '{Base.GetType().Name}'."
                );

            _attributes[name] = attr;
            return attr;
        }

        private string BuildStoredKey<T>(string name)
        {
            // Include T to prevent collisions if you reuse the same name with different types.
            return $"{PersistenceKey}:{name}:{typeof(T).FullName}";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Static Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static TWrapper Get(string stringId) => Wrap(GetBase(stringId));

        public static TWrapper Get(TBase mbObject) => Wrap(mbObject);

        /// <summary>
        /// Returns the underlying MBObjectBase from MBObjectManager by StringId.
        /// </summary>
        private static TBase GetBase(string stringId)
        {
            if (stringId == null)
                throw new ArgumentNullException(nameof(stringId));

            var manager = MBObjectManager.Instance;
            if (manager == null)
                return null;

            return manager.GetObject<TBase>(stringId);
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
        private static TWrapper Wrap(TBase baseInstance)
        {
            if (baseInstance == null)
                return null;

            // Expect concrete wrappers to expose a constructor (TBase baseInstance).
            return (TWrapper)
                Activator.CreateInstance(typeof(TWrapper), new object[] { baseInstance });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equality                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines whether this instance is equal to another wrapper instance.
        /// </summary>
        public bool Equals(WBase<TWrapper, TBase> other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            // Identity comparison: same wrapper type + same StringId.
            var id = StringId;
            var otherId = other.StringId;

            if (id == null || otherId == null)
                return false;

            return string.Equals(id, otherId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether this instance is equal to another object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as WBase<TWrapper, TBase>);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            var id = StringId;
            return id != null ? StringComparer.Ordinal.GetHashCode(id) : 0;
        }

        /// <summary>
        /// Determines whether two wrapper instances are equal.
        /// </summary>
        public static bool operator ==(WBase<TWrapper, TBase> left, WBase<TWrapper, TBase> right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two wrapper instances are not equal.
        /// </summary>
        public static bool operator !=(WBase<TWrapper, TBase> left, WBase<TWrapper, TBase> right)
        {
            return !(left == right);
        }
    }
}
