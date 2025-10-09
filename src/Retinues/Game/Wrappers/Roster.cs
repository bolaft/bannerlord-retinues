using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for TroopRoster, provides helpers for accessing elements, troop counts, ratios, and adding/removing troops.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
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
        /// Gets the number of healthy troops in the roster.
        /// </summary>
        public int Count => _roster.TotalHealthyCount;

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
        public float EliteRatio => Count == 0 ? 0 : (float)EliteCount / (Count - HeroCount);

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
        public float CustomRatio => Count == 0 ? 0 : (float)CustomCount / (Count - HeroCount);

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
        public float RetinueRatio => Count == 0 ? 0 : (float)RetinueCount / (Count - HeroCount);

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
        public float InfantryRatio => Count == 0 ? 0 : (float)InfantryCount / (Count - HeroCount);

        /// <summary>
        /// Ratio of archer troops (excluding heroes).
        /// </summary>
        public float ArchersRatio => Count == 0 ? 0 : (float)ArchersCount / (Count - HeroCount);

        /// <summary>
        /// Ratio of cavalry troops (excluding heroes).
        /// </summary>
        public float CavalryRatio => Count == 0 ? 0 : (float)CavalryCount / (Count - HeroCount);

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
