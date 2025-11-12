using System;
using System.Collections.Generic;
using Retinues.Game.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for TroopRoster, provides helpers for accessing elements, troop counts, ratios, and adding/removing troops.
    /// </summary>
    [SafeClass]
    public class WRoster(TroopRoster roster, WParty party)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly TroopRoster _roster = roster;

        public TroopRoster Base => _roster;

        private readonly WParty _party = party;

        public WParty Party => _party;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WRoster(TroopRoster roster)
            : this(
                roster,
                new WParty(Reflector.GetPropertyValue<PartyBase>(roster, "OwnerParty").MobileParty)
            ) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Elements                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Enumerates all elements in the roster as WRosterElement.
        /// </summary>
        public IEnumerable<WRosterElement> Elements
        {
            get
            {
                int i = 0;
                foreach (var element in _roster.GetTroopRoster())
                {
                    yield return new WRosterElement(element, this, i);
                    i++;
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the number of troops in the roster.
        /// </summary>
        public int Count => _roster.TotalManCount;

        /// <summary>
        /// Gets the number of healthy troops in the roster.
        /// </summary>
        public int HealthyCount => _roster.TotalHealthyCount;

        /// <summary>
        /// Gets the count of a specific troop type.
        /// </summary>
        public int CountOf(WCharacter troop)
        {
            if (troop.Base == null)
            {
                Log.Warn($"CountOf: troop has no base!");
                return 0;
            }
            return _roster.GetTroopCount(troop.Base);
        }

        /// <summary>
        /// Adds a troop to the roster with healthy, wounded, xp, and index.
        /// </summary>
        public void AddTroop(
            WCharacter troop,
            int healthy,
            int wounded = 0,
            int xp = 0,
            int index = -1
        )
        {
            if (troop.Base == null)
                return;

            _roster.AddToCounts(
                troop.Base,
                healthy,
                woundedCount: wounded,
                xpChange: xp,
                index: index
            );
        }

        /// <summary>
        /// Removes a troop from the roster.
        /// </summary>
        public void RemoveTroop(WCharacter troop, int healthy, int wounded = 0)
        {
            if (troop.Base == null)
                return;

            _roster.AddToCounts(troop.Base, -healthy, woundedCount: -wounded);
        }

        /// <summary>
        /// Swap a specific troop in the roster to another troop.
        /// </summary>
        public void SwapTroop(WCharacter troop, WCharacter target)
        {
            if (troop.Base == null || target.Base == null)
                return;

            foreach (var e in Elements)
            {
                if (e.Troop == troop)
                {
                    int healthy = e.Number;
                    int wounded = e.WoundedNumber;
                    int xp = e.Xp;
                    int index = e.Index;

                    // Remove old troop
                    _roster.AddToCounts(troop.Base, -healthy, woundedCount: -wounded);

                    // Add new troop at same index
                    _roster.AddToCounts(
                        target.Base,
                        healthy,
                        woundedCount: wounded,
                        xpChange: xp,
                        index: index
                    );

                    Log.Debug($"{Party.Name}: swapped {healthy}x {troop.Name} to {target.Name}.");
                    return;
                }
            }
        }

        /// <summary>
        /// Swap all troops in a roster to the best match from the given faction.
        /// Preserves heroes and logs replacements.
        /// </summary>
        public void SwapTroops(WFaction faction = null, bool skipHeroParties = true)
        {
            if (Base == null)
                return;

            if (faction == null)
                faction = Party.PlayerFaction;

            if (faction == null)
                return; // no player faction

            try
            {
                bool swapped = false;

                // Build temp roster (dummy so it won't fire OwnerParty events during staging)
                var tmp = TroopRoster.CreateDummyTroopRoster();

                // Enumerate a snapshot so we don't fight internal mutations while staging
                var elements = new List<WRosterElement>(Elements);

                foreach (var e in elements)
                {
                    if (e?.Troop?.Base == null)
                        continue;

                    // Keep heroes as-is
                    if (e.Troop.IsHero)
                    {
                        if (skipHeroParties)
                            return; // skip entire swap if hero party

                        tmp.AddToCounts(
                            e.Troop.Base,
                            e.Number,
                            insertAtFront: false,
                            woundedCount: e.WoundedNumber,
                            xpChange: e.Xp
                        );
                        continue;
                    }

                    // Try to pick best replacement from faction
                    WCharacter replacement =
                        TroopMatcher.PickSpecialFromFaction(faction, e.Troop)
                        ?? (
                            Party.IsMilitia
                                ? TroopMatcher.PickMilitiaFromFaction(faction, e.Troop)
                                : TroopMatcher.PickBestFromFaction(faction, e.Troop)
                        )
                        ?? e.Troop;

                    if (replacement != e.Troop)
                    {
                        Log.Debug(
                            $"{Party.Name}: swapping {e.Number}x {e.Troop.Name} to {replacement.Name}."
                        );
                        swapped = true;
                    }

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
                var original = Base;
                original.Clear();
                original.Add(tmp);

                if (swapped)
                    Log.Debug(
                        $"{Party.Name} (militia: {Party.IsMilitia}): swapped all troops to faction {faction?.Name ?? "null"}."
                    );
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"SwapTroops failed for {Party.Name}");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the number of hero troops in the roster.
        /// </summary>
        public int HeroCount
        {
            get
            {
                int count = 0;
                foreach (var e in Elements)
                    if (e.Troop.IsHero)
                        count += e.Number;
                return count;
            }
        }

        /// <summary>
        /// Gets the number of elite troops in the roster.
        /// </summary>
        public int EliteCount
        {
            get
            {
                int count = 0;
                foreach (var e in Elements)
                    if (e.Troop.IsElite)
                        count += e.Number;
                return count;
            }
        }

        /// <summary>
        /// Ratio of elite troops (excluding heroes).
        /// </summary>
        public float EliteRatio =>
            HealthyCount == 0 ? 0 : (float)EliteCount / (HealthyCount - HeroCount);

        /// <summary>
        /// Gets the number of custom troops in the roster.
        /// </summary>
        public int CustomCount
        {
            get
            {
                int count = 0;
                foreach (var e in Elements)
                    if (e.Troop.IsCustom)
                        count += e.Number;
                return count;
            }
        }

        /// <summary>
        /// Ratio of custom troops (excluding heroes).
        /// </summary>
        public float CustomRatio =>
            HealthyCount == 0 ? 0 : (float)CustomCount / (HealthyCount - HeroCount);

        /// <summary>
        /// Gets the number of retinue troops in the roster.
        /// </summary>
        public int RetinueCount
        {
            get
            {
                int count = 0;
                foreach (var e in Elements)
                    if (e.Troop.IsRetinue)
                        count += e.Number;
                return count;
            }
        }

        /// <summary>
        /// Ratio of retinue troops (excluding heroes).
        /// </summary>
        public float RetinueRatio =>
            HealthyCount == 0 ? 0 : (float)RetinueCount / (HealthyCount - HeroCount);

        /// <summary>
        /// Gets the number of infantry troops in the roster.
        /// </summary>
        public int InfantryCount => CountByFormation(FormationClass.Infantry);

        /// <summary>
        /// Gets the number of archer troops in the roster.
        /// </summary>
        public int ArchersCount => CountByFormation(FormationClass.Ranged);

        /// <summary>
        /// Gets the number of cavalry troops in the roster.
        /// </summary>
        public int CavalryCount => CountByFormation(FormationClass.Cavalry);

        /// <summary>
        /// Ratio of infantry troops (excluding heroes).
        /// </summary>
        public float InfantryRatio =>
            HealthyCount == 0 ? 0 : (float)InfantryCount / (HealthyCount - HeroCount);

        /// <summary>
        /// Ratio of archer troops (excluding heroes).
        /// </summary>
        public float ArchersRatio =>
            HealthyCount == 0 ? 0 : (float)ArchersCount / (HealthyCount - HeroCount);

        /// <summary>
        /// Ratio of cavalry troops (excluding heroes).
        /// </summary>
        public float CavalryRatio =>
            HealthyCount == 0 ? 0 : (float)CavalryCount / (HealthyCount - HeroCount);

        /// <summary>
        /// Helper to count troops by formation class.
        /// </summary>
        private int CountByFormation(FormationClass cls)
        {
            int count = 0;
            foreach (var e in Elements)
            {
                var co = e.Troop.Base;
                var c = co?.DefaultFormationClass;
                if (c == cls)
                    count += e.Number;
            }
            return count;
        }
    }
}
