using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Characters;
using Retinues.Utilities;

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

            var faction = State.Instance.Faction;
            if (faction == null)
                return;

            void AddSection(
                ListVM list,
                string headerId,
                string headerLocKey,
                string headerFallback,
                IEnumerable<WCharacter> troops,
                bool civilian = false
            )
            {
                var header = new CharacterListHeaderVM(
                    list,
                    headerId,
                    L.S(headerLocKey, headerFallback)
                );
                list.AddHeader(header);

                if (troops == null)
                    return;

                foreach (var troop in troops)
                {
                    if (troop == null)
                        continue;

                    AddCharacterRow(header, troop, civilian);
                }
            }

            // Retinues.
            AddSection(
                list,
                "retinues",
                "list_header_retinues",
                L.S("list_header_retinues", "Retinues"),
                faction.RosterRetinues
            );

            // Elite tree.
            AddSection(
                list,
                "elite",
                "list_header_elite",
                L.S("list_header_elite", "Elite"),
                faction.RootElite?.Tree
            );

            // Regular tree.
            AddSection(
                list,
                "regular",
                "list_header_regular",
                L.S("list_header_regular", "Regular"),
                faction.RootBasic?.Tree
            );

            // Militia.
            AddSection(
                list,
                "militia",
                "list_header_militia",
                L.S("list_header_militia", "Militia"),
                faction.RosterMilitia
            );

            // Caravan.
            AddSection(
                list,
                "caravan",
                "list_header_caravan",
                L.S("list_header_caravan", "Caravan"),
                faction.RosterCaravan
            );

            // Villagers.
            AddSection(
                list,
                "villagers",
                "list_header_villagers",
                L.S("list_header_villagers", "Villagers"),
                faction.RosterVillager
            );

            // Bandits.
            AddSection(
                list,
                "bandits",
                "list_header_bandits",
                L.S("list_header_bandits", "Bandits"),
                faction.RosterBandit
            );

            // Civilians.
            AddSection(
                list,
                "civilians",
                "list_header_civilians",
                L.S("list_header_civilians", "Civilians"),
                faction.RosterCivilian,
                civilian: true
            );
        }

        private void AddCharacterRow(
            ListHeaderVM header,
            WCharacter character,
            bool civilian = false
        )
        {
            if (character == null)
                return;

            var wasEmpty = header.Rows.Count == 0;

            var row = new CharacterListRowVM(header, character, civilian);
            header.Rows.Add(row);

            header.UpdateRowCount();
            header.UpdateState();

            if (wasEmpty && header.IsEnabled)
                header.IsExpanded = true;
        }
    }
}
