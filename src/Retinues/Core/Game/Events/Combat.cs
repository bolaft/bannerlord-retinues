using System.Collections.Generic;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Events
{
    public class Combat : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Info                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly List<Kill> Kills = [];

        public class Kill(Agent victim, Agent killer, AgentState state, KillingBlow blow)
        {
            public WAgent Victim = new(victim);
            public WAgent Killer = new(killer);
            public AgentState State = state;
            public KillingBlow Blow = blow;
        }

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
            if (victim == null || killer == null)
                return; // e.g. if agent despawned

            if (state != AgentState.Killed && state != AgentState.Unconscious)
                return; // only care about kills and knockouts

            if (victim.Character is not CharacterObject)
                return; // ignore non-character agents (horses, etc)

            if (killer.Character is not CharacterObject)
                return; // ignore non-character agents (horses, etc)

            Kills.Add(new Kill(victim, killer, state, blow));
        }
    }
}
