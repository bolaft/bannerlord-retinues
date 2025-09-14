
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Retinues.Core.Game.Wrappers.Cache;
using TaleWorlds.Core;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Wrappers
{
    public class WAgent(Agent agent)
    {
        private readonly Agent _agent = agent;

        public Agent Agent => _agent;

        public BattleSideEnum Side => _agent.Team?.Side ?? BattleSideEnum.None;

        public WCharacter Character = WCharacterCache.Wrap((CharacterObject)agent.Character);

        public bool IsPlayer => _agent.IsPlayerControlled;
        public bool IsPlayerTroop => _agent.IsPlayerTroop;
    }
}
