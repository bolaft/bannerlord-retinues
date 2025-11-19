using System;
using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for MobileParty, provides helpers for accessing rosters, leader, faction, and swapping troops to match a faction.
    /// </summary>
    [SafeClass]
    public class WParty(MobileParty party) : BaseFactionMember(party.ActualClan ?? party.LeaderHero?.Clan ?? party.HomeSettlement?.OwnerClan)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IEnumerable<WParty> All
        {
            get
            {
                foreach (var mp in MobileParty.All)
                    yield return new WParty(mp);
            }
        }

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
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string StringId => _party.StringId;

        public string Name
        {
            get
            {
                try
                {
                    return _party.Name.ToString();
                }
                catch
                {
                    // Crashes when uninitialized
                    return "Unknown Party";
                }
            }
        }

        public bool IsMainParty => _party.IsMainParty;
        public bool IsLordParty => _party.IsLordParty;
        public bool IsCustomParty => _party.IsCustomParty;
        public bool IsVillager => _party.IsVillager;
        public bool IsCaravan => _party.IsCaravan;
        public bool IsBandit => _party.IsBandit;
        public bool IsGarrison => _party.IsGarrison;
        public bool IsMilitia => _party.IsMilitia;

        public int PartySizeLimit => _party.Party.PartySizeLimit;

        public float Morale => _party.Morale;

        public Army Army => _party.Army;

        public bool IsInArmy => Army != null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Swaps all troops in member and prisoner rosters to their corresponding player versions.
        /// </summary>
        public static void SwapAll(
            bool members,
            bool prisoners,
            bool skipMainParty = false,
            bool skipLordParties = false,
            bool skipCustomParties = false,
            bool skipGarrisons = false,
            bool skipCaravans = false,
            bool skipVillagers = false,
            bool skipBandits = false,
            bool skipMilitia = false
        )
        {
            foreach (var party in All)
            {
                if (skipMainParty && party.IsMainParty)
                    continue;
                if (skipLordParties && party.IsLordParty)
                    continue;
                if (skipCustomParties && party.IsCustomParty)
                    continue;
                if (skipGarrisons && party.IsGarrison)
                    continue;
                if (skipCaravans && party.IsCaravan)
                    continue;
                if (skipVillagers && party.IsVillager)
                    continue;
                if (skipBandits && party.IsBandit)
                    continue;
                if (skipMilitia && party.IsMilitia)
                    continue;

                if (members)
                    party.MemberRoster?.SwapTroops();
                if (prisoners)
                    party.PrisonRoster?.SwapTroops();
            }
        }
    }
}
