using System.Collections.Generic;
using TaleWorlds.SaveSystem;

// ReSharper disable ClassNeverInstantiated.Global

namespace Retinues.Migration.Legacy
{
    // ─────────────────────────────────────────────────────────────────────────
    // Pure-data mirror of v1 TroopCombatStats.
    // ─────────────────────────────────────────────────────────────────────────

    public sealed class TroopCombatStats
    {
        [SaveableField(1)]
        public string TroopId;

        [SaveableField(2)]
        public int TotalBattles;

        [SaveableField(3)]
        public int BattlesWon;

        [SaveableField(4)]
        public int BattlesLost;

        [SaveableField(5)]
        public int FieldBattles;

        [SaveableField(6)]
        public int SiegeBattles;

        [SaveableField(7)]
        public int HideoutBattles;

        [SaveableField(8)]
        public int VillageRaidBattles;

        [SaveableField(9)]
        public int OtherBattles;

        [SaveableField(10)]
        public int TotalKills;

        [SaveableField(11)]
        public int TotalDeaths;

        [SaveableField(12)]
        public Dictionary<string, int> KillsByTroopId;

        [SaveableField(13)]
        public Dictionary<string, int> DeathsByTroopId;

        [SaveableField(14)]
        public Dictionary<string, int> FactionsFought;
    }
}
