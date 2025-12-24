using System;
using Retinues.Utilities;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model
{
    [SafeClass(IncludeDerived = true)]
    public abstract partial class MBase<TBase>(TBase baseInstance) : IEquatable<MBase<TBase>>
        where TBase : class
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public TBase Base { get; } =
            baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equality                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override bool Equals(object obj) => Equals(obj as MBase<TBase>);

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

        public override int GetHashCode()
        {
            if (Base is MBObjectBase mbo)
            {
                var id = mbo.StringId;
                return id != null ? StringComparer.Ordinal.GetHashCode(id) : 0;
            }

            return Base != null ? Base.GetHashCode() : 0;
        }

        public static bool operator ==(MBase<TBase> left, MBase<TBase> right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(MBase<TBase> left, MBase<TBase> right) => !(left == right);
    }
}
