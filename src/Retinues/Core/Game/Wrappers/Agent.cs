using Retinues.Core.Game.Wrappers.Cache;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Wrappers
{
    public class WAgent(Agent agent)
    {
        private readonly Agent _agent = agent;

        public Agent Agent => _agent;

        public BattleSideEnum Side => _agent.Team?.Side ?? BattleSideEnum.None;

        public WCharacter Character = agent.Character is CharacterObject @object
            ? WCharacterCache.Wrap(@object)
            : null;

        public bool IsPlayer => _agent.IsPlayerControlled;

        public bool IsPlayerTroop => _agent.Team?.IsPlayerTeam == true;
    }
}
