using System.Collections.Generic;

namespace Retinues.Core.Features.Doctrines
{
    public enum DoctrineStatus
    {
        Locked,
        Unlockable,
        InProgress,
        Unlocked,
    }

    public sealed class FeatDefinition
    {
        public string Key;
        public string Description;
        public int Target;
    }

    public sealed class DoctrineDefinition
    {
        public string Key;
        public string Name;
        public string Description;

        public int Column;
        public int Row;
        public string PrerequisiteKey;

        public int GoldCost;
        public int InfluenceCost;

        public List<FeatDefinition> Feats = [];
    }
}
