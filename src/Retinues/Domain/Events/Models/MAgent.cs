using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Framework.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Domain.Events.Models
{
    /// <summary>
    /// Wrapper for Agent.
    /// </summary>
    public sealed class MAgent(
        Agent @base,
        MMission mission,
        MMapEvent mapEvent,
        WParty party = null
    ) : MBase<Agent>(@base)
    {
        readonly MMission _mission = mission;
        public MMission Mission => _mission;

        readonly MMapEvent _mapEvent = mapEvent;
        public MMapEvent MapEvent => _mapEvent;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Snapshot                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates a snapshot of this agent.
        /// </summary>
        public Snapshot TakeSnapshot() => new(this);

        /// <summary>
        /// Immutable snapshot of an agent at a point in time.
        /// </summary>
        public sealed class Snapshot(MAgent @base)
        {
            /* ━━━━━━━ Character ━━━━━━ */

            public string CharacterId { get; } = @base.Character.StringId;
            public WCharacter Character => WCharacter.Get(CharacterId);

            /* ━━━━━━━━━ Party ━━━━━━━━ */

            public string PartyId { get; } = @base.Party?.StringId;
            public WParty Party => WParty.Get(PartyId);

            /* ━━━━━━━ Equipment ━━━━━━ */

            public string EquipmentCode { get; } = @base.EquipmentCode;
            public MEquipment Equipment => MEquipment.FromCode(Character, EquipmentCode);

            /* ━━━━━━━━━ Flags ━━━━━━━━ */

            public bool IsPlayer { get; } = @base.IsPlayer;
            public bool IsPlayerTroop { get; } = @base.IsPlayerTroop;
            public bool IsAllyTroop { get; } = @base.IsAllyTroop;
            public bool IsEnemyTroop { get; } = @base.IsEnemyTroop;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Side                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public MapEventSide Side => ResolveMapEventSide(MapEvent, Base);

        public BattleSideEnum SideEnum
        {
            get
            {
                var t = Base.Team;
                if (t != null && t.Side != BattleSideEnum.None)
                    return t.Side;

                var side = Side;
                if (side == null)
                    return BattleSideEnum.None;

                if (_mapEvent == null)
                    return BattleSideEnum.None;

                if (ReferenceEquals(side, _mapEvent.AttackerSide))
                    return BattleSideEnum.Attacker;

                if (ReferenceEquals(side, _mapEvent.DefenderSide))
                    return BattleSideEnum.Defender;

                return BattleSideEnum.None;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Character                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Character
        {
            get
            {
                var c = Base?.Character;
                if (c is not CharacterObject co)
                    return null;

                return WCharacter.Get(co);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Party                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        readonly WParty _party = party;
        public WParty Party => _party ?? ResolveParty(Base);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MEquipment _equipment;

        public MEquipment Equipment
        {
            get
            {
                if (_equipment != null)
                    return _equipment;

                var eq = Base?.SpawnEquipment;
                if (eq == null)
                    return null;

                _equipment = new MEquipment(eq, Character);
                return _equipment;
            }
        }

        public string EquipmentCode => Equipment?.Code;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsPlayer => Character.IsPlayer;

        BattleSideEnum PlayerSideEnum => MapEvent?.PlayerSideEnum ?? BattleSideEnum.None;

        public bool IsPlayerSide =>
            PlayerSideEnum != BattleSideEnum.None
            && SideEnum != BattleSideEnum.None
            && SideEnum == PlayerSideEnum;

        public bool IsEnemySide =>
            PlayerSideEnum != BattleSideEnum.None
            && SideEnum != BattleSideEnum.None
            && SideEnum != PlayerSideEnum;

        public bool IsPlayerTroop => IsPlayerSide && Party?.IsMainParty == true;
        public bool IsAllyTroop => IsPlayerSide && Party?.IsMainParty != true;
        public bool IsEnemyTroop => IsEnemySide;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Resolve                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Resolve the wrapped party for a given agent.
        /// </summary>
        static WParty ResolveParty(Agent agent)
        {
            var partyBase = ResolvePartyBase(agent);
            var mobileParty = partyBase?.MobileParty;
            if (mobileParty == null)
                return null;

            return WParty.Get(mobileParty);
        }

        /// <summary>
        /// Resolve the PartyBase from an agent origin.
        /// </summary>
        static PartyBase ResolvePartyBase(Agent agent)
        {
            var origin = agent?.Origin;
            if (origin == null)
                return null;

            if (origin is PartyAgentOrigin pao)
                return pao.Party;

            return null;
        }

        /// <summary>
        /// Find the map event side that contains the agent's party.
        /// </summary>
        static MapEventSide ResolveMapEventSide(MMapEvent mapEvent, Agent agent)
        {
            if (mapEvent == null || agent == null)
                return null;

            var partyBase = ResolvePartyBase(agent);
            if (partyBase == null)
                return null;

            var attacker = mapEvent.Base.AttackerSide;
            if (attacker != null)
            {
                foreach (var p in attacker.Parties)
                {
                    if (ReferenceEquals(p?.Party, partyBase))
                        return attacker;
                }
            }

            var defender = mapEvent.Base.DefenderSide;
            if (defender != null)
            {
                foreach (var p in defender.Parties)
                {
                    if (ReferenceEquals(p?.Party, partyBase))
                        return defender;
                }
            }

            return null;
        }
    }
}
