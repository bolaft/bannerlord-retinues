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
    /// Produces a single merged report combining mission kills and map event outcome when available.
    /// </summary>
    public sealed class BattleReportBehavior : BaseCampaignBehavior
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Pending                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class PendingMission
        {
            public string MissionMode;
            public string SceneName;

            public IReadOnlyList<CombatBehavior.Kill> Kills;

            public int Killed;
            public int Unconscious;
            public int Headshots;
            public int Missiles;
        }

        private PendingMission _pending;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Buckets                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private enum TroopBucket
        {
            Player = 0,
            Hero = 1,
            Retinues = 2,
            Custom = 3,
            Vanilla = 4,
            Count = 5,
        }

        private static readonly string[] TroopBucketLabels =
        [
            "player",
            "hero",
            "retinues",
            "custom",
            "vanilla",
        ];

        private static TroopBucket GetBucket(WCharacter wc)
        {
            if (wc == null)
                return TroopBucket.Vanilla;

            if (wc.IsPlayer)
                return TroopBucket.Player;

            if (wc.IsHero)
                return TroopBucket.Hero;

            if (wc.IsRetinue)
                return TroopBucket.Retinues;

            if (wc.IsFactionTroop)
                return TroopBucket.Custom;

            return TroopBucket.Vanilla;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Mission  Summary                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Logs a mission end summary, or defers it if a map event summary will follow.
        /// </summary>
        protected override void OnMissionEnded(MMission mission)
        {
            var kills = CombatBehavior.GetKills();
            var pending = BuildPending(mission, kills);

            var mapEvent = CombatBehavior.MapEvent;
            var hasMapEvent =
                mapEvent != null && CombatBehavior.Snapshot != null && mapEvent.IsPlayerInvolved;

            if (hasMapEvent)
            {
                _pending = pending;
                return;
            }

            var report = BuildMergedReport(pending, start: null, end: null);
            Log.Debug(report);
        }

        private static PendingMission BuildPending(
            MMission mission,
            IReadOnlyList<CombatBehavior.Kill> kills
        )
        {
            var pm = new PendingMission
            {
                MissionMode = mission?.Mode.ToString() ?? "<none>",
                SceneName = mission?.SceneName ?? "<none>",
                Kills = kills ?? [],
            };

            if (kills == null || kills.Count == 0)
                return pm;

            for (int i = 0; i < kills.Count; i++)
            {
                var k = kills[i];

                if (k.State == AgentState.Killed)
                    pm.Killed++;
                else if (k.State == AgentState.Unconscious)
                    pm.Unconscious++;

                if (k.IsHeadShot)
                    pm.Headshots++;

                if (k.IsMissile)
                    pm.Missiles++;
            }

            return pm;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Map Event Summary                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Logs a map event end summary merged with the pending mission report.
        /// </summary>
        protected override void OnMapEventEnded(MMapEvent end)
        {
            if (end?.IsPlayerInvolved != true)
                return;

            var start = CombatBehavior.Snapshot;
            if (start == null)
            {
                Log.Warning("Skipping merged battle report: start snapshot is null.");
                return;
            }

            var pending = _pending ?? BuildPending(mission: null, CombatBehavior.GetKills());
            _pending = null;

            var report = BuildMergedReport(pending, start, end);
            Log.Debug(report);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Report                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string BuildMergedReport(
            PendingMission mission,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            var sb = new StringBuilder(8192);

            sb.AppendLine("Summary:\n");

            // Mission info (always)
            sb.Append("Mission: Mode='")
                .Append(mission?.MissionMode ?? "<none>")
                .Append("'. Scene='")
                .Append(mission?.SceneName ?? "<none>")
                .AppendLine("'.");

            var kills = mission?.Kills;
            sb.Append("Kills captured: ").Append(kills?.Count ?? 0).AppendLine();

            if (kills != null && kills.Count > 0)
            {
                sb.Append("Outcomes: killed=")
                    .Append(mission.Killed)
                    .Append(", unconscious=")
                    .Append(mission.Unconscious)
                    .Append(", headshots=")
                    .Append(mission.Headshots)
                    .Append(", missiles=")
                    .Append(mission.Missiles)
                    .AppendLine();
            }

            // If no map event, fall back to a simple player vs other split.
            if (end == null)
            {
                sb.AppendLine();
                sb.AppendLine("No map event detected.");

                BuildSimpleSideTables(sb, kills);
                return sb.ToString();
            }

            // Map event info
            sb.AppendLine();
            sb.Append("Type: ").Append(end.EventType).AppendLine();
            sb.Append("Outcome: ").Append(GetOutcome(end)).AppendLine();

            // Build per-side kill/casualty tables from mission kills.
            var attackerKills = new int[(int)TroopBucket.Count];
            var attackerCasualties = new int[(int)TroopBucket.Count];
            var defenderKills = new int[(int)TroopBucket.Count];
            var defenderCasualties = new int[(int)TroopBucket.Count];

            BuildMapEventSideTables(
                end,
                kills,
                attackerKills,
                attackerCasualties,
                defenderKills,
                defenderCasualties
            );

            AppendSideSummary(
                sb,
                "Attacker",
                start?.AttackerSide,
                end.AttackerSide,
                attackerKills,
                attackerCasualties
            );

            AppendSideSummary(
                sb,
                "Defender",
                start?.DefenderSide,
                end.DefenderSide,
                defenderKills,
                defenderCasualties
            );

            return sb.ToString();
        }

        private static void BuildSimpleSideTables(
            StringBuilder sb,
            IReadOnlyList<CombatBehavior.Kill> kills
        )
        {
            var playerKills = new int[(int)TroopBucket.Count];
            var playerCasualties = new int[(int)TroopBucket.Count];
            var otherKills = new int[(int)TroopBucket.Count];
            var otherCasualties = new int[(int)TroopBucket.Count];

            if (kills != null)
            {
                for (int i = 0; i < kills.Count; i++)
                {
                    var k = kills[i];

                    var killerIsPlayerSide = IsPlayerSide(k.Killer);
                    var victimIsPlayerSide = IsPlayerSide(k.Victim);

                    var killerBucket = GetBucket(k.KillerCharacter);
                    var victimBucket = GetBucket(k.VictimCharacter);

                    if (killerIsPlayerSide)
                        playerKills[(int)killerBucket]++;
                    else
                        otherKills[(int)killerBucket]++;

                    if (victimIsPlayerSide)
                        playerCasualties[(int)victimBucket]++;
                    else
                        otherCasualties[(int)victimBucket]++;
                }
            }

            sb.AppendLine();
            sb.AppendLine("Player");
            AppendTroopTypeTable(sb, playerKills, playerCasualties);

            sb.AppendLine();
            sb.AppendLine("Other");
            AppendTroopTypeTable(sb, otherKills, otherCasualties);
        }

        private static bool IsPlayerSide(MAgent.Snapshot agent)
        {
            if (agent == null)
                return false;

            if (agent.IsPlayer)
                return true;

            var party = agent.Party;
            if (party?.IsMainParty == true)
                return true;

            return false;
        }

        private enum MapSide
        {
            Unknown = 0,
            Attacker = 1,
            Defender = 2,
        }

        private static void BuildMapEventSideTables(
            MMapEvent end,
            IReadOnlyList<CombatBehavior.Kill> kills,
            int[] attackerKills,
            int[] attackerCasualties,
            int[] defenderKills,
            int[] defenderCasualties
        )
        {
            var attackerPartyIds = BuildPartyIdSet(end.AttackerSide);
            var defenderPartyIds = BuildPartyIdSet(end.DefenderSide);

            if (kills == null || kills.Count == 0)
                return;

            for (int i = 0; i < kills.Count; i++)
            {
                var k = kills[i];

                var killerSide = ResolveMapSide(
                    k.Killer,
                    end.PlayerSideEnum,
                    attackerPartyIds,
                    defenderPartyIds
                );
                var victimSide = ResolveMapSide(
                    k.Victim,
                    end.PlayerSideEnum,
                    attackerPartyIds,
                    defenderPartyIds
                );

                var killerBucket = GetBucket(k.KillerCharacter);
                var victimBucket = GetBucket(k.VictimCharacter);

                if (killerSide == MapSide.Attacker)
                    attackerKills[(int)killerBucket]++;
                else if (killerSide == MapSide.Defender)
                    defenderKills[(int)killerBucket]++;

                if (victimSide == MapSide.Attacker)
                    attackerCasualties[(int)victimBucket]++;
                else if (victimSide == MapSide.Defender)
                    defenderCasualties[(int)victimBucket]++;
            }
        }

        private static HashSet<string> BuildPartyIdSet(MMapEvent.SideData side)
        {
            var set = new HashSet<string>();

            var parties = side?.PartyData;
            if (parties == null || parties.Count == 0)
                return set;

            for (int i = 0; i < parties.Count; i++)
            {
                var pid = parties[i]?.PartyId;
                if (!string.IsNullOrEmpty(pid))
                    set.Add(pid);
            }

            return set;
        }

        private static MapSide ResolveMapSide(
            MAgent.Snapshot agent,
            BattleSideEnum playerSide,
            HashSet<string> attackerPartyIds,
            HashSet<string> defenderPartyIds
        )
        {
            if (agent == null)
                return MapSide.Unknown;

            var pid = agent.PartyId;
            if (!string.IsNullOrEmpty(pid))
            {
                if (attackerPartyIds.Contains(pid))
                    return MapSide.Attacker;

                if (defenderPartyIds.Contains(pid))
                    return MapSide.Defender;
            }

            // Fallback: use player/enemy flags if present.
            if (agent.IsPlayerTroop || agent.IsAllyTroop)
            {
                if (playerSide == BattleSideEnum.Attacker)
                    return MapSide.Attacker;

                if (playerSide == BattleSideEnum.Defender)
                    return MapSide.Defender;

                return MapSide.Unknown;
            }

            if (agent.IsEnemyTroop)
            {
                if (playerSide == BattleSideEnum.Attacker)
                    return MapSide.Defender;

                if (playerSide == BattleSideEnum.Defender)
                    return MapSide.Attacker;

                return MapSide.Unknown;
            }

            return MapSide.Unknown;
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
        private static void AppendSideSummary(
            StringBuilder sb,
            string title,
            MMapEvent.SideData start,
            MMapEvent.SideData end,
            int[] kills,
            int[] casualties
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

            sb.Append("  IsInArmy: ").Append(end.IsInArmy).AppendLine();

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

            AppendTroopTypeTable(sb, kills, casualties);

            var endParties = end.PartyData;
            if (endParties == null || endParties.Count == 0)
                return;

            sb.Append("  Parties (").Append(endParties.Count).AppendLine("):");

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

                // Match your existing log format: defenders show "+gold" even when it is GoldLost.
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

        private static void AppendTroopTypeTable(StringBuilder sb, int[] kills, int[] casualties)
        {
            sb.AppendLine("  Kills/Casualties by troop type:");

            var wLabel = 0;
            var wKills = 0;
            var wCas = 0;

            for (int i = 0; i < (int)TroopBucket.Count; i++)
            {
                var label = TroopBucketLabels[i];
                wLabel = Math.Max(wLabel, label.Length);

                var k = kills != null ? kills[i].ToString() : "0";
                var c = casualties != null ? casualties[i].ToString() : "0";

                wKills = Math.Max(wKills, k.Length);
                wCas = Math.Max(wCas, c.Length);
            }

            for (int i = 0; i < (int)TroopBucket.Count; i++)
            {
                var label = TroopBucketLabels[i];
                var k = kills != null ? kills[i].ToString() : "0";
                var c = casualties != null ? casualties[i].ToString() : "0";

                sb.Append("    ")
                    .Append(Format.PadRight(label, wLabel))
                    .Append(" : ")
                    .Append(Format.PadLeft(k, wKills))
                    .Append(" / ")
                    .Append(Format.PadLeft(c, wCas))
                    .AppendLine();
            }
        }
    }
}
