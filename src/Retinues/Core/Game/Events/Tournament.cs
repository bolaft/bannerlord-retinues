using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Game.Events
{
    public class Tournament(Town town, WCharacter winner = null, List<WCharacter> participants = null) : MissionBehavior
    {
        // =========================================================================
        // Fields
        // =========================================================================

        public Town Town = town;
        public WCharacter Winner = winner;
        public List<WCharacter> Participants = participants ?? [];

        // =========================================================================
        // Overrides
        // =========================================================================

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        // =========================================================================
        // Updates
        // =========================================================================

        public void UpdateOnFinish(WCharacter winner, List<WCharacter> participants)
        {
            Winner = winner;
            Participants = participants;
        }

        // =========================================================================
        // Info
        // =========================================================================

        public readonly List<KnockOut> KnockOuts = [];

        // =========================================================================
        // Mission Events
        // =========================================================================

        public override void OnAgentRemoved(Agent victim, Agent killer, AgentState state, KillingBlow blow)
        {
            if (victim == null || killer == null)
                return; // e.g. if agent despawned

            if (state != AgentState.Unconscious)
                return; // only care about knockouts

            KnockOuts.Add(new KnockOut(victim, killer));
        }

        // =========================================================================
        // Internals
        // =========================================================================

        public class KnockOut(Agent victim, Agent killer)
        {
            public WAgent Victim = new(victim);
            public WAgent Killer = new(killer);
        }
    }
}