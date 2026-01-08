using System;
using System.Text;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Retinues.Game.Missions
{
    /// <summary>
    /// MapEvent lifecycle hook.
    /// Sets MMapEvent.Current on map event start and clears it on map event end.
    /// Also clears on mission end to avoid stale current references.
    /// </summary>
    public sealed class MapEventBehavior : BaseCampaignBehavior
    {
        protected override void OnMapEventStarted(
            MapEvent mapEvent,
            PartyBase attackerParty,
            PartyBase defenderParty
        )
        {
            // Respect IsEnabled like the old behavior.
            if (!IsEnabled)
                return;

            MMapEvent.SetCurrent(mapEvent);

            var attacker = attackerParty?.Name?.ToString();
            var defender = defenderParty?.Name?.ToString();

            Log.Info(
                $"Map event started. Type='{mapEvent?.EventType}', Id='{mapEvent?.StringId}', Attacker='{attacker}', Defender='{defender}'."
            );
        }

        protected override void OnMapEventEnded(MapEvent mapEvent)
        {
            if (!IsEnabled)
                return;

#if DEBUG
            try
            {
                // Only summarize if player is involved.
                if (mapEvent != null && mapEvent.IsPlayerMapEvent)
                    DebugLogMapEventSummary(mapEvent);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
#endif

            Log.Info($"Map event ended. Type='{mapEvent?.EventType}', Id='{mapEvent?.StringId}'.");

            // Only clear if we're still the current map event.
            MMapEvent.ClearCurrentIf(mapEvent);
        }

        protected override void OnMissionEnded(IMission mission)
        {
            // Spec: clear statics on mission end. This avoids stale state if MapEventEnded is skipped.
            // Intentionally not gated behind IsEnabled.
            MMapEvent.ClearCurrent();
        }

#if DEBUG
        private static void DebugLogMapEventSummary(MapEvent mapEvent)
        {
            var sb = new StringBuilder(4096);

            sb.AppendLine("=== MapEvent Summary (Player Involved) ===");
            sb.Append("Id: ").Append(mapEvent.StringId).AppendLine();
            sb.Append("Type: ").Append(mapEvent.EventType).AppendLine();
            sb.Append("BattleState: ").Append(mapEvent.BattleState).AppendLine();
            sb.Append("PlayerSide: ").Append(mapEvent.PlayerSide).AppendLine();

            if (mapEvent.HasWinner)
            {
                sb.Append("Winner: ").Append(mapEvent.WinningSide).AppendLine();
                sb.Append("Defeated: ").Append(mapEvent.DefeatedSide).AppendLine();
            }
            else
            {
                sb.AppendLine("Winner: <none>");
            }

            AppendSideSummary(sb, mapEvent, BattleSideEnum.Attacker);
            AppendSideSummary(sb, mapEvent, BattleSideEnum.Defender);

            Log.Info(sb.ToString());
        }

        private static void AppendSideSummary(
            StringBuilder sb,
            MapEvent mapEvent,
            BattleSideEnum sideEnum
        )
        {
            var side = mapEvent.GetMapEventSide(sideEnum);
            if (side == null)
            {
                sb.Append("Side ").Append(sideEnum).AppendLine(": <null>");
                return;
            }

            var leaderParty = side.LeaderParty;
            var leaderHero = leaderParty?.LeaderHero;

            sb.AppendLine();
            sb.Append("Side: ").Append(sideEnum).AppendLine();
            sb.Append("LeaderParty: ")
                .Append(leaderParty?.Name?.ToString() ?? "<null>")
                .AppendLine();
            sb.Append("LeaderHero: ").Append(leaderHero?.Name?.ToString() ?? "<none>").AppendLine();
            sb.Append("Faction: ")
                .Append(side.MapFaction?.Name?.ToString() ?? "<null>")
                .AppendLine();

            int startMen = 0;
            try
            {
                startMen = mapEvent.GetNumberOfInvolvedMen(sideEnum);
            }
            catch { }

            sb.Append("Men involved (start): ").Append(startMen).AppendLine();
            sb.Append("Healthy troops (end): ")
                .Append(side.GetTotalHealthyTroopCountOfSide())
                .AppendLine();
            sb.Append("Healthy heroes (end): ")
                .Append(side.GetTotalHealthyHeroCountOfSide())
                .AppendLine();
            sb.Append("Parties: ").Append(side.Parties?.Count ?? 0).AppendLine();

            if (side.Parties == null || side.Parties.Count == 0)
                return;

            sb.AppendLine("Party details:");
            for (int i = 0; i < side.Parties.Count; i++)
            {
                var p = side.Parties[i];
                var party = p?.Party;

                var pname = party?.Name?.ToString() ?? "<null>";
                var hero = party?.LeaderHero?.Name?.ToString() ?? "<none>";
                var clan = party?.LeaderHero?.Clan?.Name?.ToString() ?? "<none>";
                var faction = party?.MapFaction?.Name?.ToString() ?? "<none>";

                int endHealthy = 0;
                try
                {
                    endHealthy = party?.MemberRoster?.TotalHealthyCount ?? 0;
                }
                catch { }

                sb.Append("  - ")
                    .Append(pname)
                    .Append(" | Leader=")
                    .Append(hero)
                    .Append(" | Clan=")
                    .Append(clan)
                    .Append(" | Faction=")
                    .Append(faction)
                    .Append(" | StartHealthy=")
                    .Append(p?.HealthyManCountAtStart ?? 0)
                    .Append(" | EndHealthy=")
                    .Append(endHealthy)
                    .Append(" | Contribution=")
                    .Append(p?.ContributionToBattle ?? 0)
                    .Append(" | Renown=")
                    .Append(p?.GainedRenown ?? 0f)
                    .Append(" | Influence=")
                    .Append(p?.GainedInfluence ?? 0f)
                    .Append(" | Morale=")
                    .Append(p?.MoraleChange ?? 0f)
                    .Append(" | Plunder=")
                    .Append(p?.PlunderedGold ?? 0)
                    .Append(" | GoldLost=")
                    .Append(p?.GoldLost ?? 0)
                    .AppendLine();
            }
        }
#endif
    }
}
