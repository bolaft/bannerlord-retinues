using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Troops.Save
{
    /// <summary>
    /// Serializable save data for a troop, including identity, stats, skills, equipment, and upgrade targets.
    /// Used for saving and loading custom troop state.
    /// </summary>
    public class TroopSaveData
    {
        /* ━━━━━━━━ Fields ━━━━━━━━ */

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
        public string EquipmentCode; // Legacy

        [SaveableField(8)]
        public List<TroopSaveData> UpgradeTargets = [];

        [SaveableField(9)]
        public int XpPool = 0; // Legacy

        [SaveableField(10)]
        public List<string> EquipmentCodes = [];

        [SaveableField(11)]
        public string CultureId;

        [SaveableField(12)]
        public float AgeMin;

        [SaveableField(13)]
        public float AgeMax;

        [SaveableField(14)]
        public float WeightMin;

        [SaveableField(15)]
        public float WeightMax;

        [SaveableField(16)]
        public float BuildMin;

        [SaveableField(17)]
        public float BuildMax;

        [SaveableField(18)]
        public float HeightMin;

        [SaveableField(19)]
        public float HeightMax;
    }
}
