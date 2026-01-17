using System;
using System.Collections.Generic;
using System.Text;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Missions
{
    /// <summary>
    /// Battle lifecycle hook.
    /// Maintains MMapEvent.Current/StartSnapshot/EndSnapshot and logs on end.
    /// </summary>
    public sealed class BattleReportBehavior : BaseCampaignBehavior
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Start                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Logs mission start.
        /// </summary>
        protected override void OnMissionStarted(MMission mission)
        {
            Log.Debug($"Mission started. Scene='{mission.SceneName}', Mode='{mission.Mode}'.");
        }

        /// <summary>
        /// Logs map event start.
        /// </summary>
        protected override void OnMapEventStarted(MMapEvent mapEvent)
        {
            if (mapEvent?.IsPlayerInvolved != true)
                return;

            Log.Debug($"Map event started. Type='{mapEvent.EventType}'.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Mission  Summary                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Logs mission end summary.
        /// </summary>
        protected override void OnMissionEnded(MMission mission)
        {
            // Gather kill stats.
            var kills = CombatBehavior.GetKills();

            var sb = new StringBuilder(4096);
            sb.AppendLine("Summary:\n");
            sb.Append("Kills captured: ").Append(kills?.Count ?? 0).AppendLine();

            if (kills == null || kills.Count == 0)
            {
                Log.Debug(sb.ToString());
                return;
            }

            int killed = 0;
            int unconscious = 0;
            int headshots = 0;
            int missiles = 0;

            var killsByKiller = new Dictionary<string, int>(64);
            var deathsByVictim = new Dictionary<string, int>(64);

            foreach (var k in kills)
            {
                if (k.State == AgentState.Killed)
                    killed++;
                else if (k.State == AgentState.Unconscious)
                    unconscious++;

                if (k.IsHeadShot)
                    headshots++;

                if (k.IsMissile)
                    missiles++;

                if (!string.IsNullOrEmpty(k.KillerCharacterId))
                {
                    killsByKiller.TryGetValue(k.KillerCharacterId, out var count);
                    killsByKiller[k.KillerCharacterId] = count + 1;
                }

                if (!string.IsNullOrEmpty(k.VictimCharacterId))
                {
                    deathsByVictim.TryGetValue(k.VictimCharacterId, out var count);
                    deathsByVictim[k.VictimCharacterId] = count + 1;
                }
            }

            sb.Append("Outcomes: killed=")
                .Append(killed)
                .Append(", unconscious=")
                .Append(unconscious)
                .Append(", headshots=")
                .Append(headshots)
                .Append(", missiles=")
                .Append(missiles)
                .AppendLine();

            AppendTopNCharacters(sb, "Top killers", killsByKiller, 8);
            AppendTopNCharacters(sb, "Most deaths", deathsByVictim, 8);

            Log.Debug(sb.ToString());
        }

        /// <summary>
        /// Appends a summary of a battle side to the given StringBuilder.
        /// </summary>
        private void AppendTopNCharacters(
            StringBuilder sb,
            string title,
            Dictionary<string, int> counts,
            int n
        )
        {
            sb.AppendLine(title + ":");

            foreach (var kv in TakeTop(counts, n))
            {
                var name = WCharacter.Get(kv.Key).Name ?? "<unknown>";

                sb.Append("  - ").Append(name);

                if (!string.IsNullOrEmpty(kv.Key) && name != kv.Key)
                    sb.Append(" (").Append(kv.Key).Append(")");

                sb.Append(": ").Append(kv.Value).AppendLine();
            }
        }

        /// <summary>
        /// Yields the top N entries from the given counts dictionary.
        /// </summary>
        private IEnumerable<KeyValuePair<string, int>> TakeTop(
            Dictionary<string, int> counts,
            int n
        )
        {
            var list = new List<KeyValuePair<string, int>>(counts.Count);
            foreach (var kv in counts)
                list.Add(kv);

            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            for (int i = 0; i < list.Count && i < n; i++)
                yield return list[i];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Map Event Summary                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Logs map event end summary.
        /// </summary>
        protected override void OnMapEventEnded(MMapEvent end)
        {
            if (end?.IsPlayerInvolved != true)
                return;

            var start = CombatBehavior.Snapshot;
            if (start == null)
            {
                Log.Warning("Skipping MapEvent summary: start snapshot is null.");
                return;
            }

            if (end == null)
            {
                Log.Warning("Skipping MapEvent summary: MMapEvent.Current is null.");
                return;
            }

            var sb = new StringBuilder(4096);

            sb.AppendLine("Summary:\n");
            sb.Append("Type: ").Append(end.EventType).AppendLine();
            sb.Append("Outcome: ").Append(GetOutcome(end)).AppendLine();

            AppendSideSummary(sb, "Attacker", start.AttackerSide, end.AttackerSide);
            AppendSideSummary(sb, "Defender", start.DefenderSide, end.DefenderSide);

            Log.Debug(sb.ToString());
        }

        private static string GetOutcome(MMapEvent end)
        {
            if (end == null)
                return "<none>";

            if (!end.HasWinner)
                return "Draw";

            return end.WinningSide == end.PlayerSideEnum ? "Victory" : "Defeat";
        }

        /// <summary>
        /// Appends a summary of a battle side to the given StringBuilder.
        /// </summary>
        private void AppendSideSummary(
            StringBuilder sb,
            string title,
            MMapEvent.SideData start,
            MMapEvent.SideData end
        )
        {
            sb.AppendLine();
            if (end == null)
            {
                sb.Append(title).Append(": <null>").AppendLine();
                return;
            }

            var leaderParty = end.LeaderParty;
            var leaderPartyName = leaderParty?.Name ?? "<none>";
            var factionName = leaderParty?.Base?.MapFaction?.Name?.ToString() ?? "<none>";

            sb.Append(title)
                .Append(": ")
                .Append(leaderPartyName)
                .Append(" (")
                .Append(factionName)
                .AppendLine(")");

            var isInArmy = end.IsInArmy;
            sb.Append("  IsInArmy: ").Append(isInArmy).AppendLine();

            var startTroops = start?.HealthyTroops ?? 0;
            var startHeroes = start?.HealthyHeroes ?? 0;

            var endTroops = end.HealthyTroops;
            var endHeroes = end.HealthyHeroes;

            var troopCasualties = Math.Max(0, startTroops - endTroops);
            var heroCasualties = Math.Max(0, startHeroes - endHeroes);

            sb.Append("  Casualties: ")
                .Append(troopCasualties)
                .Append("/")
                .Append(startTroops)
                .Append(" troops, ")
                .Append(heroCasualties)
                .Append("/")
                .Append(startHeroes)
                .Append(" heroes")
                .AppendLine();

            var endParties = end.PartyData;
            if (endParties == null || endParties.Count == 0)
                return;

            sb.Append("  Parties (").Append(endParties.Count).AppendLine("):");

            // Compute party-name width for nice alignment.
            var nameWidth = 0;
            for (int i = 0; i < endParties.Count; i++)
            {
                var n = endParties[i]?.Party?.Name;
                if (!string.IsNullOrEmpty(n))
                    nameWidth = Math.Max(nameWidth, n.Length);
            }

            // Build rows first, then align columns.
            var rows = new List<(
                string name,
                string fac,
                string start,
                string end,
                string gold,
                string renown,
                string influence,
                string morale
            )>(endParties.Count);

            for (int i = 0; i < endParties.Count; i++)
            {
                var ep = endParties[i];
                if (ep == null)
                    continue;

                var startHealthy = ep.HealthyEnd;
                if (start?.PartyData != null)
                {
                    for (int j = 0; j < start.PartyData.Count; j++)
                    {
                        var sp = start.PartyData[j];
                        if (sp != null && sp.PartyId == ep.PartyId)
                        {
                            startHealthy = sp.HealthyStart;
                            break;
                        }
                    }
                }

                var partyName = ep.Party?.Name ?? "<null>";
                var partyFaction = ep.Party?.Base?.MapFaction?.Name?.ToString() ?? "<none>";

                // Match your sample: defenders show "+gold" even when it is GoldLost.
                var goldValue = ep.PlunderedGold > 0 ? ep.PlunderedGold : ep.GoldLost;

                var goldText = Format.PlusLabel(goldValue, "gold");
                var renownText = Format.PlusLabel((int)MathF.Round(ep.GainedRenown), "renown");
                var influenceText = Format.PlusLabel(
                    (int)MathF.Round(ep.GainedInfluence),
                    "influence"
                );
                var moraleText = Format.PlusLabel((int)MathF.Round(ep.MoraleChange), "morale");

                rows.Add(
                    (
                        name: partyName,
                        fac: partyFaction,
                        start: startHealthy.ToString(),
                        end: ep.HealthyEnd.ToString(),
                        gold: goldText,
                        renown: renownText,
                        influence: influenceText,
                        morale: moraleText
                    )
                );
            }

            // Compute max widths per column.
            int wName = 0,
                wFac = 0,
                wStart = 0,
                wEnd = 0,
                wGold = 0,
                wRenown = 0,
                wInfluence = 0,
                wMorale = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                wName = Math.Max(wName, r.name.Length);
                wFac = Math.Max(wFac, r.fac.Length);
                wStart = Math.Max(wStart, r.start.Length);
                wEnd = Math.Max(wEnd, r.end.Length);
                wGold = Math.Max(wGold, r.gold.Length);
                wRenown = Math.Max(wRenown, r.renown.Length);
                wInfluence = Math.Max(wInfluence, r.influence.Length);
                wMorale = Math.Max(wMorale, r.morale.Length);
            }

            // Print aligned.
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];

                sb.Append("    ")
                    .Append(Format.PadRight(r.name, wName))
                    .Append(" | ")
                    .Append(Format.PadRight(r.fac, wFac))
                    .Append(" | ")
                    .Append(Format.PadLeft(r.start, wStart))
                    .Append(" | ")
                    .Append(Format.PadLeft(r.end, wEnd))
                    .Append(" | ")
                    .Append(Format.PadLeft(r.gold, wGold))
                    .Append(" | ")
                    .Append(Format.PadLeft(r.renown, wRenown))
                    .Append(" | ")
                    .Append(Format.PadLeft(r.influence, wInfluence))
                    .Append(" | ")
                    .Append(Format.PadLeft(r.morale, wMorale))
                    .AppendLine();
            }
        }
    }
}
