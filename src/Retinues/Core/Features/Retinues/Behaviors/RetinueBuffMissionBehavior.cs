using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Features.Retinues.Behaviors
{
    [SafeClass]
    public sealed class RetinueBuffMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        private const float Bonus = 5;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Mission Events                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void OnAgentCreated(Agent agent)
        {
            if (agent == null || !agent.IsHuman)
                return;

            if (agent.Team != Mission.MainAgent?.Team)
                return;

            if (agent.Character is not CharacterObject co || co.IsHero)
                return;

            if (!new WCharacter(co).IsRetinue)
                return;

            agent.Health += Bonus;
        }
    }
}
