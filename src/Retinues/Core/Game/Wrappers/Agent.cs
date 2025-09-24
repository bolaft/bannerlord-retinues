using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Wrappers
{
    public class WAgent(Agent agent)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Agent _agent = agent;

        public Agent Agent => _agent;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Character = agent?.Character is CharacterObject @object
            ? new WCharacter(@object)
            : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public BattleSideEnum Side => _agent?.Team?.Side ?? BattleSideEnum.None;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsPlayer => _agent?.IsPlayerControlled == true;

        public bool IsPlayerTroop => _agent.Origin.Banner == Player.Clan.Base.Banner; // Hacky but works

        public bool IsAllyTroop => !IsPlayerTroop && _agent?.Team?.IsPlayerAlly == true;

        public bool IsEnemyTroop => _agent?.Team?.IsEnemyOf(_agent?.Mission?.PlayerTeam) == true;
    }
}
