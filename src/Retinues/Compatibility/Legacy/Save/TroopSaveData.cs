using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace Retinues.Compatibility.Legacy.Save
{
    /// <summary>
    /// Minimal legacy troop save schema used by earlier Retinues versions.
    /// This is intentionally "field-id compatible" so old saves deserialize,
    /// even if we don't migrate every nested field.
    /// </summary>
    public sealed class TroopSaveData
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
        public string CultureId;

        [SaveableField(7)]
        public List<TroopSaveData> UpgradeTargets;

        // Nested types existed in older schemas; we keep placeholders so deserialization succeeds.
        [SaveableField(8)]
        public TroopEquipmentData EquipmentData;

        [SaveableField(9)]
        public TroopSkillData SkillData;

        [SaveableField(10)]
        public TroopBodySaveData BodyData;

        [SaveableField(11)]
        public int Race;

        [SaveableField(12)]
        public FormationClass FormationClassOverride = FormationClass.Unset;

        [SaveableField(13)]
        public TroopSaveData Captain;

        [SaveableField(14)]
        public bool IsCaptain;

        [SaveableField(15)]
        public bool CaptainEnabled;

        [SaveableField(16)]
        public bool IsMariner;

        public TroopSaveData() { }
    }
}
