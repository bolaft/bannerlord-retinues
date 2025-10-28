using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Troops.Save
{
    public class TroopIndexEntry
    {
        [SaveableField(1)]
        public string Id; // "retinues_custom_0001"

        [SaveableField(2)]
        public bool IsKingdom;

        [SaveableField(3)]
        public bool IsElite;

        [SaveableField(4)]
        public bool IsRetinue;

        [SaveableField(5)]
        public bool IsMilitiaMelee;

        [SaveableField(6)]
        public bool IsMilitiaRanged;

        [SaveableField(7)]
        public string ParentId;

        [SaveableField(8)]
        public List<string> ChildrenIds = [];

        [SaveableField(9)]
        public List<int> Path = [];
    }
}
