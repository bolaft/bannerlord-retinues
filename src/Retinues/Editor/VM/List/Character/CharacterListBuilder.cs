using System.Collections.Generic;
using Retinues.Utilities;
using Retinues.Wrappers.Characters;

namespace Retinues.Editor.VM.List.Character
{
    /// <summary>
    /// Builds the character list for the editor.
    /// </summary>
    public class CharacterListBuilder : BaseListBuilder
    {
        /// <summary>
        /// Builds the sort buttons for the character list.
        /// </summary>
        protected override void BuildSortButtons(ListVM list)
        {
            list.SortButtons.Clear();
            list.SortButtons.Add(
                new ListSortButtonVM(list, ListSortKey.Name, L.S("sort_by_name", "Name"), 3)
            );
            list.SortButtons.Add(
                new ListSortButtonVM(list, ListSortKey.Tier, L.S("sort_by_tier", "Tier"), 1)
            );

            list.RecomputeSortButtonProperties();
        }

        /// <summary>
        /// Builds the sections for the character list.
        /// </summary>
        protected override void BuildSections(ListVM list)
        {
            list.Clear();

            var faction = BaseStatefulVM.StateFaction;

            if (faction == null)
                return;

            WCharacter firstCharacter = null;

            static void AddSection(
                ListVM list,
                ref WCharacter first,
                string headerId,
                string headerLocKey,
                string headerFallback,
                IEnumerable<WCharacter> troops,
                bool civilian = false
            )
            {
                var header = list.AddHeader(headerId, L.S(headerLocKey, headerFallback));

                if (troops == null)
                {
                    return;
                }

                foreach (var troop in troops)
                {
                    if (troop == null)
                    {
                        continue;
                    }

                    header.AddCharacterRow(troop, civilian);
                    first ??= troop;
                }
            }

            // Retinues.
            AddSection(
                list,
                ref firstCharacter,
                "retinues",
                "list_header_retinues",
                L.S("list_header_retinues", "Retinues"),
                faction.RosterRetinues
            );

            // Elite tree.
            AddSection(
                list,
                ref firstCharacter,
                "elite",
                "list_header_elite",
                L.S("list_header_elite", "Elite"),
                faction.RootElite?.Tree
            );

            // Regular tree.
            AddSection(
                list,
                ref firstCharacter,
                "regular",
                "list_header_regular",
                L.S("list_header_regular", "Regular"),
                faction.RootBasic?.Tree
            );

            // Militia.
            AddSection(
                list,
                ref firstCharacter,
                "militia",
                "list_header_militia",
                L.S("list_header_militia", "Militia"),
                faction.RosterMilitia
            );

            // Caravan.
            AddSection(
                list,
                ref firstCharacter,
                "caravan",
                "list_header_caravan",
                L.S("list_header_caravan", "Caravan"),
                faction.RosterCaravan
            );

            // Villagers.
            AddSection(
                list,
                ref firstCharacter,
                "villagers",
                "list_header_villagers",
                L.S("list_header_villagers", "Villagers"),
                faction.RosterVillager
            );

            // Bandits.
            AddSection(
                list,
                ref firstCharacter,
                "bandits",
                "list_header_bandits",
                L.S("list_header_bandits", "Bandits"),
                faction.RosterBandit
            );

            // Civilians.
            AddSection(
                list,
                ref firstCharacter,
                "civilians",
                "list_header_civilians",
                L.S("list_header_civilians", "Civilians"),
                faction.RosterCivilian,
                civilian: true
            );

            // Update state character to first in list.
            BaseStatefulVM.StateCharacter = firstCharacter;
        }
    }
}
