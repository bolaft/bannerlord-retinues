using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Model.Attributes;

namespace Retinues.Domain.Characters.Wrappers
{
    /// <summary>
    /// Partial wrapper for character battle history.
    /// </summary>
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Battle History                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Won / Lost ━━━━━━ */

        MAttribute<int> HistoryBattlesWon => Attribute(initialValue: 0);
        MAttribute<int> HistoryBattlesLost => Attribute(initialValue: 0);

        /* ━━━━━ Battle Types ━━━━━ */

        MAttribute<int> HistoryFieldBattles => Attribute(initialValue: 0);
        MAttribute<int> HistorySiegeBattles => Attribute(initialValue: 0);
        MAttribute<int> HistoryNavalBattles => Attribute(initialValue: 0);
        MAttribute<int> HistoryRaids => Attribute(initialValue: 0);

        /* ━━━━ Kills / Deaths ━━━━ */

        MAttribute<Dictionary<string, int>> HistoryKills =>
            Attribute(initialValue: new Dictionary<string, int>());
        MAttribute<Dictionary<string, int>> HistoryCasualties =>
            Attribute(initialValue: new Dictionary<string, int>());

        /// <summary>
        /// Gets the battle history for this character.
        /// </summary>
        public void GetHistory(
            out int battlesWon,
            out int battlesLost,
            out int fieldBattles,
            out int siegeBattles,
            out int navalBattles,
            out int raids,
            out Dictionary<WCharacter, int> kills,
            out Dictionary<WCharacter, int> casualties
        )
        {
            battlesWon = HistoryBattlesWon.Get();
            battlesLost = HistoryBattlesLost.Get();
            fieldBattles = HistoryFieldBattles.Get();
            siegeBattles = HistorySiegeBattles.Get();
            navalBattles = HistoryNavalBattles.Get();
            raids = HistoryRaids.Get();
            kills = HistoryKills.Get().Select(kv =>
                new KeyValuePair<WCharacter, int>(
                    Get(kv.Key),
                    kv.Value
                )
            ).Where(kv => kv.Key != null).ToDictionary(kv => kv.Key, kv => kv.Value);
            casualties = HistoryCasualties.Get().Select(kv =>
                new KeyValuePair<WCharacter, int>(
                    Get(kv.Key),
                    kv.Value
                )
            ).Where(kv => kv.Key != null).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <summary>
        /// Adds a battle result to this character's history.
        /// </summary>
        public void RecordBattleResult(MMapEvent battle)
        {
            // Type.
            if (battle.IsSiegeBattle)
                HistorySiegeBattles.Set(HistorySiegeBattles.Get() + 1);
            else if (battle.IsNavalBattle)
                HistoryNavalBattles.Set(HistoryNavalBattles.Get() + 1);
            else if (battle.IsRaid)
                HistoryRaids.Set(HistoryRaids.Get() + 1);
            else
                HistoryFieldBattles.Set(HistoryFieldBattles.Get() + 1);

            // Outcome.
            if (battle.IsWon)
                HistoryBattlesWon.Set(HistoryBattlesWon.Get() + 1);
            else
                HistoryBattlesLost.Set(HistoryBattlesLost.Get() + 1);
        }

        /// <summary>
        /// Adds a kill to this character's history.
        /// </summary>
        public void RecordKill(WCharacter target, int count = 1)
        {
            var kills = HistoryKills.Get();
            if (kills.ContainsKey(target.StringId))
                kills[target.StringId] += count;
            else
                kills[target.StringId] = count;

            HistoryKills.Set(kills);
        }

        /// <summary>
        /// Adds a casualty to this character's history.
        /// </summary>
        public void RecordCasualty(WCharacter target, int count = 1)
        {
            var casualties = HistoryCasualties.Get();
            if (casualties.ContainsKey(target.StringId))
                casualties[target.StringId] += count;
            else
                casualties[target.StringId] = count;

            HistoryCasualties.Set(casualties);
        }
    }
}
