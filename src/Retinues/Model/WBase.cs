using System;
using System.Collections.Generic;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model
{
    /// <summary>
    /// Base wrapper for MBObjectBase types with MBObjectManager helpers and identity equality.
    /// </summary>
    public abstract class WBase<TWrapper, TBase>(TBase baseInstance)
        : MBase<TBase>(baseInstance),
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
        //                     Static Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the wrapped object with the given StringId, or null if not found.
        /// </summary>
        public static TWrapper Get(string stringId)
        {
            if (stringId == null)
                throw new ArgumentNullException(nameof(stringId));

            var manager = MBObjectManager.Instance;
            if (manager == null)
                return null;

            var obj = manager.GetObject<TBase>(stringId);
            return Wrap(obj);
        }

        /// <summary>
        /// Returns the wrapped object for the given MBObjectBase, or null if the type is
        /// not compatible.
        /// </summary>
        public static TWrapper Get(TBase mbObject)
        {
            if (mbObject == null)
                return null;

            return Wrap(mbObject);
        }

        /// <summary>
        /// Returns all objects of the underlying type from MBObjectManager, wrapped.
        /// </summary>
        public static IEnumerable<TWrapper> All()
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

        /// <summary>
        /// Internal helper to construct the concrete wrapper around a base instance.
        /// Uses the concrete wrapper constructor W(TBase).
        /// </summary>
        private static TWrapper Wrap(TBase baseInstance)
        {
            if (baseInstance == null)
                return null;

            // Expect concrete wrappers to expose a constructor (TBase baseInstance).
            return (TWrapper)Activator.CreateInstance(typeof(TWrapper), baseInstance);
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
