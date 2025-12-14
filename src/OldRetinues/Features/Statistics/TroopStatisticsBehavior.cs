using System;
using System.Collections.Generic;
using System.Text;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace OldRetinues.Features.Statistics
{
    [Serializable]
    public sealed class TroopCombatStats
    {
        [SaveableField(1)]
        public string TroopId;

        /* ━━━━━━━ Battles / Outcomes ━━━━━━━ */

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

        /* ━━━━━━━ Casualties / Kills ━━━━━━━ */

        [SaveableField(10)]
        public int TotalKills;

        [SaveableField(11)]
        public int TotalDeaths;

        // Kills by victim troop id
        [SaveableField(12)]
        public Dictionary<string, int> KillsByTroopId = [];

        // Deaths by killer troop id
        [SaveableField(13)]
        public Dictionary<string, int> DeathsByTroopId = [];

        // Factions they have fought against (display name keyed)
        [SaveableField(14)]
        public Dictionary<string, int> FactionsFought = [];
    }

    /// <summary>
    /// Tracks per-troop battle records (kills, deaths, contexts) and can display them in a popup.
    /// </summary>
    [SafeClass]
    public sealed class TroopStatisticsBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Fields                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static TroopStatisticsBehavior _instance;

        // Keyed by troop StringId
        private Dictionary<string, TroopCombatStats> _stats = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Lifecycle                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public TroopStatisticsBehavior()
        {
            _instance = this;
        }

        public override void RegisterEvents()
        {
            // No campaign events needed right now; we are driven directly from Combat/Battle wrappers.
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_retinuesTroopStatistics", ref _stats);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Static API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clears all recorded statistics for the given troop.
        /// </summary>
        public static void Clear(WCharacter troop)
        {
            try
            {
                var behavior = GetInstance();
                if (behavior == null || troop == null)
                    return;

                behavior._stats.Remove(troop.StringId);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        /// <summary>
        /// Called from mission layer when a battle ends, before Combat.Kills is cleared.
        /// </summary>
        public static void RecordFromMission(Battle battle, IReadOnlyList<Combat.Kill> kills)
        {
            try
            {
                if (battle == null || kills == null || kills.Count == 0)
                    return;

                var behavior = GetInstance();
                behavior?.RecordInternal(battle, kills);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        /// <summary>
        /// Shows an inquiry popup with the given troop's battle statistics.
        /// </summary>
        public static void ShowForTroop(WCharacter troop)
        {
            try
            {
                var behavior = GetInstance();
                behavior?.ShowForTroopInternal(troop);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private static TroopStatisticsBehavior GetInstance()
        {
            if (_instance != null)
                return _instance;

            var campaign = Campaign.Current;
            if (campaign == null)
                return null;

            _instance = campaign.GetCampaignBehavior<TroopStatisticsBehavior>();
            return _instance;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Recording                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void RecordInternal(Battle battle, IReadOnlyList<Combat.Kill> kills)
        {
            // Per-battle set so we only bump "battles" counters once per troop per battle.
            var seenThisBattle = new HashSet<string>();

            foreach (var kill in kills)
            {
                // Only care about our side (player / allies)
                bool killerIsOurSide =
                    kill.KillerIsPlayer || kill.KillerIsPlayerTroop || kill.KillerIsAllyTroop;
                bool victimIsOurSide =
                    kill.VictimIsPlayer || kill.VictimIsPlayerTroop || kill.VictimIsAllyTroop;

                if (killerIsOurSide && !string.IsNullOrEmpty(kill.KillerCharacterId))
                {
                    UpdateForParticipant(
                        battle,
                        kill,
                        troopId: kill.KillerCharacterId,
                        isKiller: true,
                        seenThisBattle: seenThisBattle
                    );
                }

                if (victimIsOurSide && !string.IsNullOrEmpty(kill.VictimCharacterId))
                {
                    UpdateForParticipant(
                        battle,
                        kill,
                        troopId: kill.VictimCharacterId,
                        isKiller: false,
                        seenThisBattle: seenThisBattle
                    );
                }
            }
        }

        private void UpdateForParticipant(
            Battle battle,
            Combat.Kill kill,
            string troopId,
            bool isKiller,
            HashSet<string> seenThisBattle
        )
        {
            var stats = GetOrCreateStats(troopId);

            // 1) Battle-level stats (only once per battle per troop)
            if (seenThisBattle.Add(troopId))
                BumpBattleCounters(stats, battle);

            // 2) Per-kill stats
            var otherId = isKiller ? kill.VictimCharacterId : kill.KillerCharacterId;

            if (!string.IsNullOrEmpty(otherId))
            {
                var other = new WCharacter(otherId);

                var otherFactionName = ResolveFactionName(other);

                if (!string.IsNullOrEmpty(otherFactionName))
                    Increment(stats.FactionsFought, otherFactionName);

                if (isKiller)
                {
                    stats.TotalKills++;

                    Increment(stats.KillsByTroopId, otherId);
                }
                else
                {
                    // victim: treat both Killed and Unconscious as "went down"
                    stats.TotalDeaths++;

                    Increment(stats.DeathsByTroopId, otherId);
                }
            }
        }

        private TroopCombatStats GetOrCreateStats(string troopId)
        {
            if (string.IsNullOrEmpty(troopId))
                troopId = "?";

            if (!_stats.TryGetValue(troopId, out var stats))
            {
                stats = new TroopCombatStats { TroopId = troopId };
                _stats[troopId] = stats;
            }

            return stats;
        }

        private static void BumpBattleCounters(TroopCombatStats stats, Battle battle)
        {
            stats.TotalBattles++;

            if (battle.IsWon)
                stats.BattlesWon++;
            else if (battle.IsLost)
                stats.BattlesLost++;

            if (battle.IsSiege)
                stats.SiegeBattles++;
            else if (battle.IsVillageRaid)
                stats.VillageRaidBattles++;
            else if (battle.IsHideout)
                stats.HideoutBattles++;
            else if (battle.IsFieldBattle)
                stats.FieldBattles++;
            else
                stats.OtherBattles++;
        }

        private static string ResolveFactionName(WCharacter other)
        {
            try
            {
                var faction = other?.Faction;
                if (faction == null)
                    return null;

                var name = faction.Name;
                if (name != null && !string.IsNullOrEmpty(name.ToString()))
                    return name.ToString();

                return faction.StringId;
            }
            catch
            {
                return null;
            }
        }

        private static void Increment(Dictionary<string, int> dict, string key)
        {
            if (string.IsNullOrEmpty(key))
                key = "?";

            if (!dict.TryGetValue(key, out var value))
                value = 0;
            dict[key] = value + 1;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Popup / UI                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void ShowForTroopInternal(WCharacter troop)
        {
            if (troop == null)
                return;

            var title = L.T("battle_record_title", "Battle Record");

            if (!_stats.TryGetValue(troop.StringId, out var s))
            {
                Notifications.Popup(
                    title,
                    L.T("battle_record_no_data", "No battle data available yet for this troop.")
                );
                return;
            }

            var sb = new StringBuilder();

            // Battles
            sb.AppendLine(
                L.T(
                        "battle_record_battles_summary",
                        "Fought in {BATTLES} battles ({WON} victories, {LOST} defeats)."
                    )
                    .SetTextVariable("BATTLES", s.TotalBattles)
                    .SetTextVariable("WON", s.BattlesWon)
                    .SetTextVariable("LOST", s.BattlesLost)
                    .ToString()
            );

            sb.AppendLine();

            if (s.TotalBattles > 0)
            {
                sb.AppendLine(L.S("battle_record_battle_types", "Battle types:"));
                if (s.FieldBattles > 0)
                    sb.AppendLine(
                        L.T("battle_record_field_line", "  • Field battles: {BATTLES}")
                            .SetTextVariable("BATTLES", s.FieldBattles)
                            .ToString()
                    );
                if (s.SiegeBattles > 0)
                    sb.AppendLine(
                        L.T("battle_record_siege_line", "  • Sieges: {BATTLES}")
                            .SetTextVariable("BATTLES", s.SiegeBattles)
                            .ToString()
                    );
                if (s.HideoutBattles > 0)
                    sb.AppendLine(
                        L.T("battle_record_hideout_line", "  • Hideouts: {BATTLES}")
                            .SetTextVariable("BATTLES", s.HideoutBattles)
                            .ToString()
                    );
                if (s.VillageRaidBattles > 0)
                    sb.AppendLine(
                        L.T("battle_record_village_raid_line", "  • Village raids: {BATTLES}")
                            .SetTextVariable("BATTLES", s.VillageRaidBattles)
                            .ToString()
                    );
                if (s.OtherBattles > 0)
                    sb.AppendLine(
                        L.T("battle_record_other_line", "  • Other: {BATTLES}")
                            .SetTextVariable("BATTLES", s.OtherBattles)
                            .ToString()
                    );
            }

            sb.AppendLine();

            // Kills / deaths
            sb.AppendLine(
                L.T(
                        "stats_kills_deaths_summary",
                        "Killed {KILLS} enemies and suffered {DEATHS} casualties."
                    )
                    .SetTextVariable("KILLS", s.TotalKills)
                    .SetTextVariable("DEATHS", s.TotalDeaths)
                    .ToString()
            );
            sb.AppendLine();

            // High-level story bits
            var mostBattledFaction = GetMaxEntry(s.FactionsFought);
            if (mostBattledFaction.Key != null)
            {
                sb.AppendLine(
                    L.T(
                            "stats_most_battled_faction",
                            "Most battled: {FACTION} ({COUNT} encounters)"
                        )
                        .SetTextVariable("FACTION", mostBattledFaction.Key)
                        .SetTextVariable("COUNT", mostBattledFaction.Value)
                        .ToString()
                );
            }

            var mostSlain = GetMaxEntry(s.KillsByTroopId);
            if (mostSlain.Key != null)
            {
                var prey = new WCharacter(mostSlain.Key);
                sb.AppendLine(
                    L.T("stats_most_slain_enemy", "Most slain: {ENEMY} ({COUNT} slain)")
                        .SetTextVariable("ENEMY", prey.Name)
                        .SetTextVariable("COUNT", mostSlain.Value)
                        .ToString()
                );
            }

            var nemesis = GetMaxEntry(s.DeathsByTroopId);
            if (nemesis.Key != null)
            {
                var enemy = new WCharacter(nemesis.Key);
                sb.AppendLine(
                    L.T("stats_most_feared_enemy", "Most feared: {ENEMY} ({COUNT} casualties)")
                        .SetTextVariable("ENEMY", enemy.Name)
                        .SetTextVariable("COUNT", nemesis.Value)
                        .ToString()
                );
            }

            var body = new TextObject(sb.ToString());
            Notifications.Popup(title, body);
        }

        private static KeyValuePair<string, int> GetMaxEntry(Dictionary<string, int> dict)
        {
            if (dict == null || dict.Count == 0)
                return default;

            string bestKey = null;
            int bestValue = 0;

            foreach (var kv in dict)
            {
                if (kv.Value > bestValue)
                {
                    bestKey = kv.Key;
                    bestValue = kv.Value;
                }
            }

            return bestKey == null ? default : new KeyValuePair<string, int>(bestKey, bestValue);
        }
    }
}
