using Retinues.Core.Game.Helpers;
using Retinues.Core.Game.Wrappers.Base;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Core.Game.Wrappers
{
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

        public void SwapTroops(WFaction faction = null, bool members = true, bool prisoners = false)
        {
            if (faction == null)
                faction = PlayerFaction;

            if (faction == null)
                return; // non-player faction, skip

            if (members)
                SwapRosterTroops(MemberRoster, faction);
            if (prisoners)
                SwapRosterTroops(PrisonRoster, faction);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void SwapRosterTroops(WRoster roster, WFaction faction)
        {
            if (roster == null || roster == null || Base == null)
                return; // should not happen

            // Build a fresh roster to avoid in-place index/XpChanged issues.
            var dst = new TroopRoster(Base.Party);

            // Enumerate snapshot so we don't fight internal mutations.
            foreach (var e in roster.Elements)
            {
                if (e == null || e.Troop == null || e.Troop.Base == null)
                    continue; // should not happen

                // Keep heroes as-is
                if (e.Troop.IsHero)
                {
                    dst.AddToCounts(
                        e.Troop.Base,
                        e.Number,
                        insertAtFront: false,
                        woundedCount: e.WoundedNumber,
                        xpChange: e.Xp
                    );
                    continue;
                }

                var replacement = TroopMatcher.PickBestFromFaction(faction, e.Troop);

                // If no good replacement, keep as-is
                if (replacement == null)
                {
                    dst.AddToCounts(
                        e.Troop.Base,
                        e.Number,
                        insertAtFront: false,
                        woundedCount: e.WoundedNumber,
                        xpChange: e.Xp
                    );
                    continue;
                }

                // Apply replacement, preserving totals
                dst.AddToCounts(
                    replacement.Base,
                    e.Number,
                    insertAtFront: false,
                    woundedCount: e.WoundedNumber,
                    xpChange: e.Xp
                );
            }

            // Swap onto PartyBase
            Reflector.SetPropertyValue(Base.Party, "MemberRoster", dst);

            Log.Debug($"Roster swapped for {Name}.");
        }
    }
}
