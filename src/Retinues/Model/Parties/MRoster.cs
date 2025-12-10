using System.Collections.Generic;
using Retinues.Model.Characters;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Model.Parties
{
    /// <summary>
    /// An element in a troop roster.
    /// </summary>
    public class RosterElement(TroopRosterElement element)
    {
        public WCharacter Troop => WCharacter.Get(element.Character);
        public int Number => element.Number;
        public int WoundedNumber => element.WoundedNumber;
        public int Xp => element.Xp;
    }

    public class MRoster(TroopRoster @base) : MBase<TroopRoster>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Counts                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Total number of men (regulars + heroes) in the roster.
        /// </summary>
        public int Count => Base.TotalManCount;

        /// <summary>
        /// Number of distinct entries in the underlying troop list.
        /// </summary>
        public int ElementsCount => Base.GetTroopRoster()?.Count ?? 0;

        /// <summary>
        /// Total number of a given troop type.
        /// </summary>
        public int CountOf(WCharacter troop)
        {
            if (troop == null)
                return 0;

            return Base.GetTroopCount(troop.Base);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Elements                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IReadOnlyList<RosterElement> Elements
        {
            get
            {
                var raw = Base.GetTroopRoster();
                if (raw == null || raw.Count == 0)
                    return [];

                var list = new List<RosterElement>(raw.Count);
                foreach (var element in raw)
                {
                    list.Add(new RosterElement(element));
                }

                return list;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Mutations                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds troops to the roster.
        /// </summary>
        public void AddTroop(
            WCharacter troop,
            int number,
            int woundedNumber = 0,
            int xp = 0,
            bool insertAtFront = false
        )
        {
            if (troop == null || number == 0)
                return;

            Base.AddToCounts(troop.Base, number, insertAtFront, woundedNumber, xp);
        }

        /// <summary>
        /// Removes troops from the roster.
        /// </summary>
        public void RemoveTroop(WCharacter troop, int number, int xp = 0)
        {
            if (troop == null || number <= 0)
                return;

            // Optional params (troopSeed, xp) take their defaults.
            Base.RemoveTroop(troop.Base, number, xp: xp);
        }
    }
}
