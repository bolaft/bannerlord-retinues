using System.Collections.Generic;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        /// <summary>
        /// Sets battle history from raw values loaded during save migration.
        /// Not intended for runtime use – call <see cref="RecordBattleResult"/>
        /// during gameplay instead.
        /// </summary>
        internal void ImportLegacyHistory(
            int battlesWon,
            int battlesLost,
            int fieldBattles,
            int siegeBattles,
            Dictionary<string, int> killsByTroopId,
            Dictionary<string, int> deathsByTroopId
        )
        {
            HistoryBattlesWon.Set(battlesWon);
            HistoryBattlesLost.Set(battlesLost);
            HistoryFieldBattles.Set(fieldBattles);
            HistorySiegeBattles.Set(siegeBattles);

            if (killsByTroopId != null)
                HistoryKills.Set(killsByTroopId);
            if (deathsByTroopId != null)
                HistoryCasualties.Set(deathsByTroopId);
        }
    }
}
