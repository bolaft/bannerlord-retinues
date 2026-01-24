using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Compatibility.Legacy.Save
{
    /// <summary>
    /// Minimal legacy faction troop roots schema used by earlier Retinues versions.
    /// Field ids match the legacy layout so old saves deserialize.
    /// </summary>
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
        public TroopSaveData PrisonGuard; // legacy/unused

        // NOTE: these are intentionally out-of-order to match legacy field ids.
        [SaveableField(16)]
        public List<TroopSaveData> Mercenaries;

        [SaveableField(14)]
        public List<TroopSaveData> Bandits;

        [SaveableField(13)]
        public List<TroopSaveData> Civilians;

        [SaveableField(15)]
        public List<TroopSaveData> Heroes;

        public FactionSaveData() { }
    }
}
