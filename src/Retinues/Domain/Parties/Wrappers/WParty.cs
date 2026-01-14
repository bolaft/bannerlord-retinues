using System;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Parties.Models;
using Retinues.Framework.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Domain.Parties.Wrappers
{
    public class WParty(MobileParty @base) : WBase<WParty, MobileParty>(@base)
    {
        static WParty()
        {
            RegisterResolver(ResolveMobileParty);
        }

        static MobileParty ResolveMobileParty(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            var campaign = Campaign.Current;
            if (campaign == null)
                return null;

            var parties = campaign.MobileParties;
            if (parties == null)
                return null;

            for (int i = 0; i < parties.Count; i++)
            {
                var p = parties[i];
                if (p != null && p.StringId == id)
                    return p;
            }

            return null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Identity                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => Base.Name?.ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Party Base                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public PartyBase PartyBase => Base.Party;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsMainParty => Base.IsMainParty;
        public bool IsLordParty => Base.IsLordParty;
        public bool IsGarrison => Base.IsGarrison;
        public bool IsBandit => Base.IsBandit;
        public bool IsCaravan => Base.IsCaravan;
        public bool IsVillager => Base.IsVillager;
        public bool IsMilitia => Base.IsMilitia;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Leader                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WHero Leader => WHero.Get(Base.LeaderHero);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Factions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WClan Clan => WClan.Get(Base.ActualClan);
        public WKingdom Kingdom => WKingdom.Get(Base.ActualClan.Kingdom);
        public WCulture Culture => Clan?.Culture ?? Kingdom?.Culture;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rosters                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public MRoster MemberRoster => new(Base.MemberRoster);
        public MRoster PrisonRoster => new(Base.PrisonRoster);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Party Size                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int PartySize => Base.Party?.NumberOfAllMembers ?? 0;
        public int PartySizeLimit => Base.Party?.PartySizeLimit ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Army                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Army Army => Base.Army;
        public bool IsInArmy => Army != null;
        public bool IsArmyLeader => Army?.LeaderParty == Base;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Ratios                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float RetinueRatio => ComputeMemberRatio(t => t.IsRetinue);
        public float CustomRatio => ComputeMemberRatio(t => t.IsFactionTroop);

        public float ComputeMemberRatio(Func<WCharacter, bool> selector)
        {
            int part = 0;
            int total = 0;

            foreach (var e in MemberRoster.Elements)
            {
                if (e.Troop.IsHero)
                    continue; // Exclude heroes (including the player).

                var selected = selector(e.Troop);
                if (selected)
                    part += e.Number;

                total += e.Number;
            }

            return total > 0 ? (float)part / total : 0f;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Values                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float Morale => Base.Morale;
        public float Strength => Base.Party?.EstimatedStrength ?? 0f;
        public int TotalWage => Base.TotalWage;
    }
}
