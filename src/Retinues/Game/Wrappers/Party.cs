using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers.Base;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for MobileParty, provides helpers for accessing rosters, leader, faction, and swapping troops to match a faction.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
    public class WParty(MobileParty party) : FactionObject
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly MobileParty _party = party;

        public MobileParty Base => _party;

        private WRoster _memberRoster;

        public WRoster MemberRoster
        {
            get
            {
                _memberRoster ??= new WRoster(_party.MemberRoster, this);
                return _memberRoster;
            }
        }

        public WRoster PrisonRoster
        {
            get
            {
                if (_party.PrisonRoster == null)
                    return null;
                return new WRoster(_party.PrisonRoster, this);
            }
        }

        public WCharacter Leader
        {
            get
            {
                if (_party.LeaderHero == null)
                    return null;
                return new WCharacter(_party.LeaderHero.CharacterObject);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Faction                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Clan _ownerClan;

        public override WFaction Clan
        {
            get
            {
                if (_ownerClan == null)
                {
                    if (_party.ActualClan != null)
                        _ownerClan = _party.ActualClan;

                    if (_party.LeaderHero?.Clan != null)
                        _ownerClan = _party.LeaderHero.Clan;

                    if (_party.HomeSettlement?.OwnerClan != null)
                        _ownerClan = _party.HomeSettlement.OwnerClan;
                }

                return _ownerClan != null ? new WFaction(_ownerClan) : null;
            }
        }

        public override WFaction Kingdom => Clan != null ? new WFaction(_ownerClan.Kingdom) : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string StringId => _party.StringId;

        public string Name => _party.Name.ToString();

        public bool IsMainParty => _party.IsMainParty;

        public bool IsVillager => _party.IsVillager;

        public bool IsCaravan => _party.IsCaravan;

        public bool IsBandit => _party.IsBandit;

        public bool IsGarrison => _party.IsGarrison;

        public bool IsMilitia => _party.IsMilitia;

        public int PartySizeLimit => _party.Party.PartySizeLimit;

        public float Morale => _party.Morale;

        public Army Army => _party.Army;

        public bool IsInArmy => Army != null;
    }
}
