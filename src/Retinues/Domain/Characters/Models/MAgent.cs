using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Framework.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Domain.Characters.Models
{
    /// <summary>
    /// Wrapper for Agent.
    /// Centralizes computed links (Character, Party, MapEvent side, spawn equipment).
    /// Intended to be used transiently (e.g. during mission events).
    /// </summary>
    public sealed class MAgent(
        Agent @base,
        MMission mission = null,
        MMapEvent mapEvent = null,
        WParty party = null
    ) : MBase<Agent>(@base)
    {
        readonly MMission _mission = mission;
        readonly MMapEvent _mapEvent = mapEvent;
        readonly WParty _party = party;

        MEquipment _equipment;

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

        public WParty Party => _party ?? ResolveParty(Base);

        public MMission Mission => _mission ?? MMission.Current;

        public MMapEvent MapEvent => _mapEvent ?? MMapEvent.Current;

        /// <summary>
        /// The agent's spawn equipment.
        /// Owner is the agent Character.
        /// </summary>
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

        /// <summary>
        /// MapEventSide if resolvable (campaign battle), otherwise null.
        /// </summary>
        public MapEventSide Side => ResolveMapEventSide(MapEvent?.Base, Base);

        /// <summary>
        /// BattleSideEnum resolved without keeping references.
        /// Prefers Agent.Team.Side, falls back to MapEventSide matching.
        /// </summary>
        public BattleSideEnum SideEnum
        {
            get
            {
                if (Base?.Team != null && Base.Team.Side != BattleSideEnum.None)
                    return Base.Team.Side;

                var side = Side;
                if (side == null)
                    return BattleSideEnum.None;

                var mapEvent = MapEvent?.Base;
                if (mapEvent == null)
                    return BattleSideEnum.None;

                if (ReferenceEquals(side, mapEvent.AttackerSide))
                    return BattleSideEnum.Attacker;

                if (ReferenceEquals(side, mapEvent.DefenderSide))
                    return BattleSideEnum.Defender;

                return BattleSideEnum.None;
            }
        }

        static WParty ResolveParty(Agent agent)
        {
            var partyBase = ResolvePartyBase(agent);
            var mobileParty = partyBase?.MobileParty;
            if (mobileParty == null)
                return null;

            return WParty.Get(mobileParty);
        }

        static PartyBase ResolvePartyBase(Agent agent)
        {
            var origin = agent?.Origin;
            if (origin == null)
                return null;

            if (origin is PartyAgentOrigin pao)
                return pao.Party;

            return null;
        }

        static MapEventSide ResolveMapEventSide(MapEvent mapEvent, Agent agent)
        {
            if (mapEvent == null || agent == null)
                return null;

            var partyBase = ResolvePartyBase(agent);
            if (partyBase == null)
                return null;

            var attacker = mapEvent.AttackerSide;
            if (attacker != null)
            {
                foreach (var p in attacker.Parties)
                {
                    if (ReferenceEquals(p?.Party, partyBase))
                        return attacker;
                }
            }

            var defender = mapEvent.DefenderSide;
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
