using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Retinues.Behaviors
{
    /// <summary>
    /// Mission behavior that gives a health bonus to retinue troops on the player's team when spawned.
    /// </summary>
    [SafeClass]
    public sealed class RetinueBuffMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        private const float Bonus = 5;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds health bonus to retinue agents on the player's team when created.
        /// </summary>
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
