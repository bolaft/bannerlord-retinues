using System;
using Retinues.Framework.Runtime;
using TaleWorlds.ObjectSystem;

namespace Retinues.Framework.Model
{
    public interface IModel
    {
        object Base { get; }
    }

    /// <summary>
    /// Base class for model wrappers.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract partial class MBase<TBase>(TBase baseInstance)
        : IEquatable<MBase<TBase>>,
            IModel
        where TBase : class
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The underlying base instance.
        /// </summary>
        object IModel.Base => Base;

        public TBase Base { get; } =
            baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equality                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Equality based on the underlying base instance.
        /// </summary>
        public override bool Equals(object obj) => Equals(obj as MBase<TBase>);

        /// <summary>
        /// Equality based on the underlying base instance.
        /// </summary>
        public bool Equals(MBase<TBase> other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null)
                return false;

            // If the underlying base is an MBObjectBase, use its StringId for identity.
            if (Base is MBObjectBase mbo)
            {
                if (other.Base is not MBObjectBase otherMbo)
                    return false;

                var id = mbo.StringId;
                var otherId = otherMbo.StringId;
                if (id == null || otherId == null)
                    return false;

                return string.Equals(id, otherId, StringComparison.Ordinal);
            }

            // Otherwise fall back to comparing the underlying Base references.
            return ReferenceEquals(Base, other.Base);
        }

        /// <summary>
        /// Hash code based on the underlying base instance.
        /// </summary>
        public override int GetHashCode()
        {
            if (Base is MBObjectBase mbo)
            {
                var id = mbo.StringId;
                return id != null ? StringComparer.Ordinal.GetHashCode(id) : 0;
            }

            return Base != null ? Base.GetHashCode() : 0;
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(MBase<TBase> left, MBase<TBase> right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(MBase<TBase> left, MBase<TBase> right) => !(left == right);
    }
}
