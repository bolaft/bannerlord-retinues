using System.Collections.Generic;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Utils;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Features.Doctrines.Effects.Behaviors
{
    public sealed class ImmortalsBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private const float SurvivalChance = 0.2f;

        private bool Enabled =>
            DoctrineAPI.IsDoctrineUnlocked<Immortals>()
            && MobileParty.MainParty?.MapEvent != null
            && MobileParty.MainParty.MapEvent.IsPlayerMapEvent
            && Mission?.MainAgent != null;

        // Track fatal casualties by troop type
        private readonly Dictionary<CharacterObject, int> _retinueDeaths = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Mission Events                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void OnAgentRemoved(
            Agent victim,
            Agent killer,
            AgentState state,
            KillingBlow blow
        )
        {
            if (!Enabled)
                return;
            if (victim == null || !victim.IsHuman)
                return;
            if (killer == null || !killer.IsHuman)
                return;
            if (state != AgentState.Killed)
                return; // only actual deaths
            if (victim.Team != Mission.MainAgent.Team)
                return; // player-side only

            var co = victim.Character;
            if (co == null || co.IsHero)
                return; // ignore heroes
            if (co is not CharacterObject charObj)
                return; // ensure type

            var wc = new WCharacter(charObj);
            if (!wc.IsRetinue)
                return; // strictly retinue

            _retinueDeaths.TryGetValue(charObj, out var c);
            _retinueDeaths[charObj] = c + 1;
        }

        protected override void OnEndMission()
        {
            if (!Enabled)
                return;

            var party = MobileParty.MainParty;
            var roster = party?.MemberRoster;
            if (roster == null || _retinueDeaths.Count == 0)
                return;

            foreach (var kvp in _retinueDeaths)
            {
                var troop = kvp.Key;
                var deaths = kvp.Value;
                int restored = 0;
                for (int i = 0; i < deaths; i++)
                    if (MBRandom.RandomFloat < SurvivalChance)
                        restored++;

                if (restored > 0)
                {
                    // Add back as wounded survivors
                    try
                    {
                        Log.Info(
                            $"Immortals: Restoring {restored} of {deaths} fallen retinue '{troop.Name}'"
                        );
                        // Signature: AddToCounts(troop, number, insertAtFront, woundedNumber, xpChange)
                        roster.AddToCounts(
                            troop,
                            restored,
                            insertAtFront: false,
                            woundedCount: restored,
                            xpChange: 0
                        );
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
