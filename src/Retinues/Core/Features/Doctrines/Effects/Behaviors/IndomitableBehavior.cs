using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Game.Wrappers.Cache;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Features.Doctrines.Effects.Behaviors
{
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

        public override void OnAgentCreated(Agent agent)
        {
            if (!Enabled || agent == null || !agent.IsHuman)
                return;
            if (agent.Team != Mission.MainAgent?.Team)
                return;

            if (agent.Character is not CharacterObject co || co.IsHero)
                return;
            if (!WCharacterCache.Wrap(co).IsRetinue)
                return;

            agent.Health += Bonus;
        }
    }
}
