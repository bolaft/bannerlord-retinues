using TaleWorlds.CampaignSystem;
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

        public BattleSideEnum Side = agent?.Team?.Side ?? BattleSideEnum.None;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsPlayer = agent?.IsPlayerControlled == true;

        public bool IsPlayerTroop = agent.Origin?.Banner?.GetHashCode() == Player.Clan?.Base?.Banner?.GetHashCode(); // Hacky but works

        public bool IsAllyTroop = agent?.Team?.IsPlayerAlly == true && agent.Origin?.Banner?.GetHashCode() != Player.Clan?.Base?.Banner?.GetHashCode();

        public bool IsEnemyTroop = agent?.Team?.IsEnemyOf(agent?.Mission?.PlayerTeam) == true;
    }
}
