namespace Retinues.Core.Utils
{
    public abstract class StringIdentifier
    {
        // =========================================================================
        // Identity & Hashs
        // =========================================================================

        public abstract string StringId { get; }

        public bool Equals(StringIdentifier other) =>
            other is not null && StringId == other.StringId;

        public override bool Equals(object obj) => obj is StringIdentifier other && Equals(other);

        public override int GetHashCode() => StringId.GetHashCode();
    }
}
