using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Wrappers
{
    public class WAgent
    {
        private readonly Agent _agent;

        public Agent Agent => _agent;

        public WCharacter Character { get; }
        public BattleSideEnum Side { get; }

        public bool IsPlayer { get; }
        public bool IsPlayerTroop { get; }
        public bool IsAllyTroop { get; }
        public bool IsEnemyTroop { get; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WAgent(Agent agent)
        {
            _agent = agent ?? throw new System.ArgumentNullException(nameof(agent));

            // Components
            Character = agent.Character is CharacterObject co ? new WCharacter(co) : null;

            // Attributes
            BattleSideEnum side = BattleSideEnum.None;
            try { side = agent.Team?.Side ?? BattleSideEnum.None; } catch { }
            Side = side;

            // Flags — compute with guards so getters can’t NRE
            bool isPlayer = false;
            try
            {
                isPlayer =
                    agent.IsMainAgent ||
                    agent.Controller == Agent.ControllerType.Player ||
                    agent.IsPlayerControlled; // this one can throw during teardown
            }
            catch { /* leave false */ }
            IsPlayer = isPlayer;

            // Compare banners defensively (Player or Clan can be null in some contexts)
            bool isPlayerTroop = false;
            try
            {
                var myBanner = Player.Clan?.Base?.Banner;
                var troopBanner = agent.Origin?.Banner;
                if (myBanner != null && troopBanner != null)
                    isPlayerTroop = ReferenceEquals(myBanner, troopBanner) ||
                                    myBanner.GetHashCode() == troopBanner.GetHashCode(); // your “hacky” path
            }
            catch { }
            IsPlayerTroop = isPlayerTroop;

            bool isAlly = false;
            try
            {
                // ally = same side but not our own troop
                isAlly = !isPlayerTroop && (agent.Team?.IsPlayerAlly == true);
            }
            catch { }
            IsAllyTroop = isAlly;

            bool isEnemy = false;
            try
            {
                var playerTeam = agent.Mission?.PlayerTeam;
                isEnemy = agent.Team != null && playerTeam != null && agent.Team.IsEnemyOf(playerTeam);
            }
            catch { }
            IsEnemyTroop = isEnemy;
        }
    }
}
