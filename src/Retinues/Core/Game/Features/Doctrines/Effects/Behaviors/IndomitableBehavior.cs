using Retinues.Core.Game.Features.Doctrines;
using Retinues.Core.Game.Features.Doctrines.Catalog;
using Retinues.Core.Game.Wrappers.Cache;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Features.Doctrines.Effects.Behaviors
{
    public sealed class IndomitableBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        private const float Multiplier = 1.2f;

        private bool Enabled =>
            DoctrineAPI.IsDoctrineUnlocked<Indomitable>() &&
            MobileParty.MainParty?.MapEvent?.IsPlayerMapEvent == true;

        public override void OnAgentCreated(Agent agent)
        {
            if (!Enabled || agent == null || !agent.IsHuman) return;
            if (agent.Team != Mission.MainAgent?.Team) return;

            var co = agent.Character as CharacterObject;
            if (co == null || co.IsHero) return;
            if (!WCharacterCache.Wrap(co).IsRetinue) return;

            agent.Health *= Multiplier;
        }
    }
}
