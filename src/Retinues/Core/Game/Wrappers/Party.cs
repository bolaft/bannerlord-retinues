using System;
using System.Collections.Generic;
using Retinues.Core.Game.Helpers;
using Retinues.Core.Game.Wrappers.Base;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Core.Game.Wrappers
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Swap all troops in member and/or prisoner rosters to match the given faction.
        /// </summary>
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

        /// <summary>
        /// Swap all troops in a roster to the best match from the given faction.
        /// Preserves heroes and logs replacements.
        /// </summary>
        public void SwapRosterTroops(WRoster roster, WFaction faction)
        {
            if (roster == null || Base == null)
                return;

            try
            {
                // Build temp roster (dummy so it won't fire OwnerParty events during staging)
                var tmp = TroopRoster.CreateDummyTroopRoster();

                // Enumerate a snapshot so we don't fight internal mutations while staging
                var elements = new List<WRosterElement>(roster.Elements);

                foreach (var e in elements)
                {
                    if (e?.Troop?.Base == null)
                        continue;

                    // Keep heroes as-is
                    if (e.Troop.IsHero)
                    {
                        tmp.AddToCounts(
                            e.Troop.Base,
                            e.Number,
                            insertAtFront: false,
                            woundedCount: e.WoundedNumber,
                            xpChange: e.Xp
                        );
                        continue;
                    }

                    // Default replacement = same troop
                    WCharacter replacement =
                        (
                            IsMilitia
                                ? TroopMatcher.PickMilitiaFromFaction(faction, e.Troop)
                                : TroopMatcher.PickBestFromFaction(faction, e.Troop)
                        ) ?? e.Troop;

                    if (replacement != e.Troop)
                        Log.Debug(
                            $"{Name}: swapping {e.Number}x {e.Troop.Name} to {replacement.Name}."
                        );

                    // Stage into temp roster, preserving totals
                    tmp.AddToCounts(
                        replacement.Base,
                        e.Number,
                        insertAtFront: false,
                        woundedCount: e.WoundedNumber,
                        xpChange: e.Xp
                    );
                }

                // Apply to the original instance to keep engine refs intact
                var original = roster.Base;
                original.Clear();
                original.Add(tmp);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"SwapRosterTroops failed for {Name}");
            }
        }
    }
}
