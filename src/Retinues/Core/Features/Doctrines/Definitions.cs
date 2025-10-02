using System.Collections.Generic;
using TaleWorlds.Localization;

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
        public TextObject Description;
        public int Target;
    }

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
