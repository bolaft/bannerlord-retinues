using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

// ReSharper disable ClassNeverInstantiated.Global

namespace Retinues.Migration.Legacy
{
    // ─────────────────────────────────────────────────────────────────────────
    // Pure-data mirror of v1 TroopSaveData & related classes.
    // Field indices MUST match the v1 originals exactly – the BL save system
    // matches by type-ID (registered in LegacySaveDefinitions) and field index,
    // not by class name, so the types here can live in any namespace.
    // ─────────────────────────────────────────────────────────────────────────

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
    }

    public sealed class TroopBodySaveData
    {
        [SaveableField(1)]
        public float AgeMin;

        [SaveableField(2)]
        public float AgeMax;

        [SaveableField(3)]
        public float WeightMin;

        [SaveableField(4)]
        public float WeightMax;

        [SaveableField(5)]
        public float BuildMin;

        [SaveableField(6)]
        public float BuildMax;

        [SaveableField(7)]
        public float HeightMin;

        [SaveableField(8)]
        public float HeightMax;
    }

    public sealed class TroopSkillData
    {
        /// <summary>Semicolon-separated "skillId:value" pairs.</summary>
        [SaveableField(1)]
        public string Code;
    }

    public sealed class TroopEquipmentData
    {
        /// <summary>Serialized equipment-set codes.</summary>
        [SaveableField(1)]
        public List<string> Codes;

        /// <summary>Per-index civilian flags (may be null in old saves).</summary>
        [SaveableField(2)]
        public List<bool> Civilians;
    }

    // ─────────────────────────────────────────────────────────────────────────

    public sealed class FactionSaveData
    {
        [SaveableField(1)]
        public TroopSaveData RetinueElite;

        [SaveableField(2)]
        public TroopSaveData RetinueBasic;

        [SaveableField(3)]
        public TroopSaveData RootElite;

        [SaveableField(4)]
        public TroopSaveData RootBasic;

        [SaveableField(5)]
        public TroopSaveData MilitiaMelee;

        [SaveableField(6)]
        public TroopSaveData MilitiaMeleeElite;

        [SaveableField(7)]
        public TroopSaveData MilitiaRanged;

        [SaveableField(8)]
        public TroopSaveData MilitiaRangedElite;

        [SaveableField(9)]
        public TroopSaveData CaravanGuard;

        [SaveableField(10)]
        public TroopSaveData CaravanMaster;

        [SaveableField(11)]
        public TroopSaveData Villager;

        [SaveableField(12)]
        public TroopSaveData PrisonGuard; // legacy, unused

        [SaveableField(13)]
        public List<TroopSaveData> Civilians;

        [SaveableField(14)]
        public List<TroopSaveData> Bandits;

        [SaveableField(15)]
        public List<TroopSaveData> Heroes;

        [SaveableField(16)]
        public List<TroopSaveData> Mercenaries;
    }
}
