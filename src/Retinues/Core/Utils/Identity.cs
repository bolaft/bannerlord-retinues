namespace Retinues.Core.Utils
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                    Identity & Hashes                   //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    [SafeClass]
    public abstract class StringIdentifier
    {
        public abstract string StringId { get; }

        public bool Equals(StringIdentifier other) =>
            other is not null && StringId == other?.StringId;

        public override bool Equals(object obj) => obj is StringIdentifier other && Equals(other);

        public override int GetHashCode() => StringId.GetHashCode();

        public override string ToString() => StringId;

        public static bool operator ==(StringIdentifier left, StringIdentifier right)
        {
            if (left is null || right is null)
                return left is null == right is null;
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(StringIdentifier left, StringIdentifier right) =>
            !(left == right);
    }
}
