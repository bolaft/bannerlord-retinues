using System.Collections.Generic;

namespace Retinues.Core.Game.Features.Doctrines
{
    public enum DoctrineStatus
    {
        Locked = 0,
        Unlockable = 1,  // feats missing and/or prereq not done
        InProgress = 2,  // feats complete; ready to acquire (pay cost / research)
        Unlocked = 3
    }

    public sealed class FeatDef
    {
        public string Id;
        public string Description;
        public int Target; // e.g., "win 5 battles" => 5
    }

    public sealed class DoctrineDef
    {
        public string Id;
        public string Name;
        public string Description;

        public int Column; // 0..3
        public int Row;    // 0..3
        public string PrerequisiteId; // Id of doctrine above in the column

        public int GoldCost;
        public int InfluenceCost;

        public List<FeatDef> Feats = [];
    }
}
