using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Game.Wrappers
{
    public class WParty(MobileParty party) : StringIdentifier
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

        public WFaction Clan
        {
            get
            {
                if (_party.ActualClan != null)
                    return new WFaction(_party.ActualClan);

                if (_party.LeaderHero?.Clan != null)
                    return new WFaction(_party.LeaderHero.Clan);

                if (_party.HomeSettlement?.OwnerClan != null)
                    return new WFaction(_party.HomeSettlement.OwnerClan);

                return null;
            }
        }

        public WFaction Kingdom => Clan?.Kingdom != null ? new WFaction(_party.ActualClan.Kingdom) : null;

        public bool IsPlayerClanParty => Clan?.StringId == Player.Clan.StringId;
        public bool IsPlayerKingdomParty => Kingdom != null && Player.Kingdom != null && Kingdom.StringId == Player.Kingdom.StringId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string StringId => _party.StringId;

        public string Name => _party.Name.ToString();

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
