using System.Collections.Generic;
using TaleWorlds.Localization;

namespace OldRetinues.Doctrines
{
    /// <summary>
    /// Status of a doctrine: locked, unlockable, in progress, or unlocked.
    /// </summary>
    public enum DoctrineStatus
    {
        Locked,
        Unlockable,
        InProgress,
        Unlocked,
    }

    /// <summary>
    /// Definition for a feat, used for serialization and UI.
    /// </summary>
    public sealed class FeatDefinition
    {
        public string Key;
        public TextObject Description;
        public int Target;
    }

    /// <summary>
    /// Definition for a doctrine, used for serialization and UI.
    /// </summary>
    public sealed class DoctrineDefinition
    {
        public string Key;
        public TextObject Name;
        public TextObject Description;

        public int Column;
        public int Row;
        public string PrerequisiteKey;

        public int GoldCost;
        public int InfluenceCost;

        public List<FeatDefinition> Feats = [];
    }
}
