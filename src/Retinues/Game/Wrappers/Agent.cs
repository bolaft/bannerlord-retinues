using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for Agent, providing convenience properties for troop type and allegiance.
    /// Used to distinguish player, ally, and enemy agents in battles.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
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

        public List<WItem> Items
        {
            get
            {
                var items = new List<WItem>();
                if (_agent == null)
                    return items;

                var eq = new WEquipment(
                    _agent?.SpawnEquipment,
                    Character,
                    (int)EquipmentCategory.Battle
                );

                if (eq.Base == null)
                    return items;

                foreach (var item in eq.Items)
                    if (item != null)
                        items.Add(item);

                return items;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Constructs a WAgent wrapper and initializes troop type and allegiance properties.
        /// </summary>
        public WAgent(Agent agent)
        {
            _agent = agent ?? throw new System.ArgumentNullException(nameof(agent));

            Character = InitCharacter(agent);
            Side = InitSide(agent);
            IsPlayer = InitIsPlayer(agent);
            IsPlayerTroop = InitIsPlayerTroop(agent, IsPlayer);
            IsAllyTroop = InitIsAllyTroop(agent, IsPlayer, IsPlayerTroop);
            IsEnemyTroop = InitIsEnemyTroop(agent);
        }

        private static WCharacter InitCharacter(Agent agent)
        {
            return agent.Character is CharacterObject co ? new WCharacter(co) : null;
        }

#if BL13
        private bool InitIsPlayer(Agent agent)
        {
            bool isPlayer = false;
            try
            {
                isPlayer = agent.IsMainAgent || agent.IsPlayerControlled; // this one can throw during teardown
            }
            catch { }
            return isPlayer;
        }

        private BattleSideEnum InitSide(Agent agent)
        {
            return agent.Team?.Side ?? BattleSideEnum.None;
        }
#else
        private BattleSideEnum InitSide(Agent agent)
        {
            return agent.Team?.Side ?? BattleSideEnum.None;
        }

        private bool InitIsPlayer(Agent agent)
        {
            return agent.IsMainAgent
                || agent.Controller == Agent.ControllerType.Player
                || agent.IsPlayerControlled;
        }
#endif

        private bool InitIsPlayerTroop(Agent agent, bool isPlayer)
        {
            if (isPlayer)
                return false;
            var myBanner = Player.Clan?.Base?.Banner;
            var troopBanner = agent.Origin?.Banner;
            if (myBanner != null && troopBanner != null)
                return ReferenceEquals(myBanner, troopBanner)
                    || myBanner.GetHashCode() == troopBanner.GetHashCode();
            return false;
        }

        private bool InitIsAllyTroop(Agent agent, bool isPlayer, bool isPlayerTroop)
        {
            return !isPlayer && !isPlayerTroop && (agent.Team?.IsPlayerAlly == true);
        }

        private bool InitIsEnemyTroop(Agent agent)
        {
            var playerTeam = agent.Mission?.PlayerTeam;
            return agent.Team != null && playerTeam != null && agent.Team.IsEnemyOf(playerTeam);
        }
    }
}
