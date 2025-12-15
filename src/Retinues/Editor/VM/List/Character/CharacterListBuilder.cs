using System;
using System.Collections.Generic;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
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
            // Single binding update instead of N inserts.
            var headers = new List<ListHeaderVM>();

            var faction = State.Instance.Faction;
            if (faction == null)
            {
                list.SetHeaders(headers);
                return;
            }

            void AddSection(
                List<ListHeaderVM> headers,
                string headerId,
                string headerLocKey,
                string headerFallback,
                IEnumerable<WCharacter> characters,
                Func<bool> condition = null,
                bool civilian = false
            )
            {
                if (condition != null && !condition())
                    return;

                var header = new CharacterListHeaderVM(
                    list,
                    headerId,
                    L.S(headerLocKey, headerFallback)
                );
                headers.Add(header);

                // Character headers should always start expanded
                header.IsExpanded = true;

                if (characters != null)
                {
                    foreach (var character in characters)
                    {
                        if (character == null)
                            continue;

                        AddCharacterRow(header, character, civilian);
                    }
                }

                header.UpdateRowCount();
                header.UpdateState();
            }

            // Heroes.
            AddSection(
                headers,
                "heroes",
                "list_header_heroes",
                L.S("list_header_heroes", "Heroes"),
                faction.RosterHeroes,
                condition: () => faction is WClan
            );

            // Retinues.
            AddSection(
                headers,
                "retinues",
                "list_header_retinues",
                L.S("list_header_retinues", "Retinues"),
                faction.RosterRetinues,
                condition: () => faction is WClan
            );

            // Elite tree.
            AddSection(
                headers,
                "elite",
                "list_header_elite",
                L.S("list_header_elite", "Elite"),
                faction.RootElite?.Tree
            );

            // Regular tree.
            AddSection(
                headers,
                "regular",
                "list_header_regular",
                L.S("list_header_regular", "Regular"),
                faction.RootBasic?.Tree
            );

            // Militia.
            AddSection(
                headers,
                "militia",
                "list_header_militia",
                L.S("list_header_militia", "Militia"),
                faction.RosterMilitia,
                condition: () => faction is WCulture
            );

            // Caravan.
            AddSection(
                headers,
                "caravan",
                "list_header_caravan",
                L.S("list_header_caravan", "Caravan"),
                faction.RosterCaravan,
                condition: () => faction is WCulture
            );

            // Villagers.
            AddSection(
                headers,
                "villagers",
                "list_header_villagers",
                L.S("list_header_villagers", "Villagers"),
                faction.RosterVillager,
                condition: () => faction is WCulture
            );

            // Mercenaries.
            AddSection(
                headers,
                "mercenaries",
                "list_header_mercenaries",
                L.S("list_header_mercenaries", "Mercenaries"),
                faction.RosterMercenary,
                condition: () => faction is WCulture
            );

            // Bandits.
            AddSection(
                headers,
                "bandits",
                "list_header_bandits",
                L.S("list_header_bandits", "Bandits"),
                faction.RosterBandit,
                condition: () => faction is WCulture
            );

            // Civilians.
            AddSection(
                headers,
                "civilians",
                "list_header_civilians",
                L.S("list_header_civilians", "Civilians"),
                faction.RosterCivilian,
                civilian: true,
                condition: () => faction is WCulture
            );

            // Apply headers in one operation.
            list.SetHeaders(headers);
        }

        /// <summary>
        /// Adds a character row to the given header.
        /// </summary>
        private void AddCharacterRow(
            ListHeaderVM header,
            WCharacter character,
            bool civilian = false
        )
        {
            if (character == null)
                return;

            if (character.IsHero)
                header.AddRow(new HeroListRowVM(header, character));
            else
                header.AddRow(new CharacterListRowVM(header, character, civilian));
        }
    }
}
