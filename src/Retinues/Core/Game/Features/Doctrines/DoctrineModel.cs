using System.Collections.Generic;

namespace Retinues.Core.Game.Features.Doctrines
{
    public enum DoctrineStatus
    {
        Locked,
        Unlockable,
        InProgress,
        Unlocked,
    }

    public sealed class FeatDef
    {
        public string Key; // = featType.FullName
        public string Description;
        public int Target;
    }

    public sealed class DoctrineDef
    {
        public string Key; // = doctrineType.FullName
        public string Name;
        public string Description;

        public int Column;
        public int Row;
        public string PrerequisiteKey; // = prerequisiteType.FullName

        public int GoldCost;
        public int InfluenceCost;

        public List<FeatDef> Feats = [];
    }
}
