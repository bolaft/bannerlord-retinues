using System.Collections.Generic;
using Retinues.Core.Game.Features.Doctrines;
using Retinues.Core.Game.Features.Doctrines.Catalog;
using Retinues.Core.Game.Wrappers.Cache;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Features.Combat
{
    public sealed class RetinueSurvivalBonusBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private const float SurvivalChance = 0.2f;

        private bool Enabled =>
            DoctrineAPI.IsDoctrineUnlocked<Immortals>() &&
            MobileParty.MainParty?.MapEvent != null &&
            MobileParty.MainParty.MapEvent.IsPlayerMapEvent &&
            Mission?.MainAgent != null;

        // Track fatal casualties by troop type
        private readonly Dictionary<CharacterObject, int> _retinueDeaths = [];

        public override void OnAgentRemoved(Agent victim, Agent killer, AgentState state, KillingBlow blow)
        {
            if (!Enabled) return;
            if (victim == null || !victim.IsHuman) return;
            if (state != AgentState.Killed) return; // only actual deaths
            if (victim.Team != Mission.MainAgent.Team) return; // player-side only

            var co = victim.Character as CharacterObject;
            if (co == null || co.IsHero) return; // ignore heroes

            var wc = WCharacterCache.Wrap(co);
            if (!wc.IsRetinue) return; // strictly retinue

            _retinueDeaths.TryGetValue(co, out var c);
            _retinueDeaths[co] = c + 1;
        }

        protected override void OnEndMission()
        {
            if (!Enabled) return;

            var party = MobileParty.MainParty;
            var roster = party?.MemberRoster;
            if (roster == null || _retinueDeaths.Count == 0) return;

            foreach (var kvp in _retinueDeaths)
            {
                var troop = kvp.Key;
                var deaths = kvp.Value;
                int restored = 0;
                for (int i = 0; i < deaths; i++)
                    if (MBRandom.RandomFloat < SurvivalChance) restored++;

                if (restored > 0)
                {
                    // Add back as wounded survivors
                    try
                    {
                        // Signature: AddToCounts(troop, number, insertAtFront, woundedNumber, xpChange)
                        roster.AddToCounts(troop, restored, insertAtFront: false, woundedCount: restored, xpChange: 0);
                    }
                    catch
                    {
                        // Older signature (no xpChange)
                        roster.AddToCounts(troop, restored, false, restored);
                    }
                }
            }

            _retinueDeaths.Clear();
        }
    }
}
