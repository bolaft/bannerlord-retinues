using TaleWorlds.SaveSystem;

// ReSharper disable ClassNeverInstantiated.Global

namespace Retinues.Migration.Legacy
{
    // ─────────────────────────────────────────────────────────────────────────
    // Pure-data mirrors of the v1 staging & agent types.
    // These are registered so BL can deserialize old save partitions without
    // crashing, even though we do not actively use these values in migration.
    // ─────────────────────────────────────────────────────────────────────────

    public sealed class PendingTrainData
    {
        [SaveableField(1)]
        public string TroopId;

        [SaveableField(2)]
        public int Remaining;

        [SaveableField(3)]
        public float Carry;

        [SaveableField(4)]
        public string SkillId;

        [SaveableField(5)]
        public int PointsRemaining;

        [SaveableField(6)]
        public float PointsPerHour;
    }

    public sealed class PendingEquipData
    {
        [SaveableField(1)]
        public string TroopId;

        [SaveableField(2)]
        public int Remaining;

        [SaveableField(3)]
        public float Carry;

        [SaveableField(4)]
        public string ItemId;

        [SaveableField(5)]
        public int SlotValue;

        [SaveableField(6)]
        public int CategoryValue;

        [SaveableField(7)]
        public int EquipmentIndex;
    }

    public sealed class EquipmentPolicy
    {
        [SaveableField(1)]
        public bool FieldBattle;

        [SaveableField(2)]
        public bool SiegeDefense;

        [SaveableField(3)]
        public bool SiegeAssault;

        [SaveableField(4)]
        public bool GenderOverride;

        [SaveableField(5)]
        public bool NavalBattle;
    }

    /// <summary>Very-old save format troop data (pre-TroopSaveData era).</summary>
    public sealed class LegacyTroopSaveData
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
        public System.Collections.Generic.List<LegacyTroopSaveData> UpgradeTargets;

        [SaveableField(9)]
        public int XpPool;

        [SaveableField(10)]
        public System.Collections.Generic.List<string> EquipmentCodes;

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

        [SaveableField(20)]
        public int Race;
    }
}
