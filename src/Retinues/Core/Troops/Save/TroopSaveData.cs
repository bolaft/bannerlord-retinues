using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Troops.Save
{
    public class TroopSaveData
    {
        [SaveableField(1)]
        public string StringId;

        [SaveableField(2)]
        public string VanillaStringId;

        [SaveableField(3)]
        public string Name;

        [SaveableField(4)]
        public int Level;

        [SaveableField(5)]
        public bool IsFemale;

        [SaveableField(6)]
        public string SkillCode;

        [SaveableField(7)]
        public string EquipmentCode;

        [SaveableField(8)]
        public List<TroopSaveData> UpgradeTargets = [];
    }
}
