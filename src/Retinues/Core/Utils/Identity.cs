namespace Retinues.Core.Utils
{
    /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */
    /*                              Identity & Hashes                             */
    /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */

    public abstract class StringIdentifier
    {
        public abstract string StringId { get; }

        public bool Equals(StringIdentifier other) => other is not null && StringId == other.StringId;

        public override bool Equals(object obj) => obj is StringIdentifier other && Equals(other);

        public override int GetHashCode() => StringId.GetHashCode();
    }
}
