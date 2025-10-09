using Retinues.Doctrines.Catalog;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace Retinues.Doctrines.Effects.Behaviors
{
    /// <summary>
    /// Mission behavior for Indomitable doctrine. Adds health bonus to retinue agents on player's team if doctrine is unlocked.
    /// </summary>
    [SafeClass]
    public sealed class IndomitableBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        private const float Bonus = 5;

        private bool Enabled =>
            DoctrineAPI.IsDoctrineUnlocked<Indomitable>()
            && MobileParty.MainParty?.MapEvent?.IsPlayerMapEvent == true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Mission Events                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds health bonus to retinue agents on player's team when created, if doctrine is enabled.
        /// </summary>
        public override void OnAgentCreated(Agent agent)
        {
            if (!Enabled || agent == null || !agent.IsHuman)
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
