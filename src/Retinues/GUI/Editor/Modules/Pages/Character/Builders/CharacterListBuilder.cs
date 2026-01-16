using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Game.Retinues;
using Retinues.UI.Services;

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

            var faction = EditorState.Instance.Faction;
            if (faction == null)
            {
                list.SetHeaders(headers);
                return;
            }

            bool ShouldInclude(WCharacter c)
            {
                if (c == null)
                    return false;

                if (EditorState.Instance.Mode == EditorMode.Player)
                {
                    if (c.IsHero)
                        return false;

                    return c.IsFactionTroop;
                }

                // Universal: no custom.
                return !c.IsFactionTroop;
            }

            void AddSection(
                List<ListHeaderVM> headers,
                string headerId,
                string headerLocKey,
                string headerFallback,
                IEnumerable<WCharacter> characters,
                Func<bool> condition = null
            )
            {
                if (condition != null && !condition())
                    return;

                if (characters == null)
                    return;

                var header = new CharacterListHeaderVM(
                    list,
                    headerId,
                    L.S(headerLocKey, headerFallback)
                )
                {
                    // Character headers should always start expanded
                    IsExpanded = true,
                };

                var any = false;

                foreach (var character in characters)
                {
                    if (!ShouldInclude(character))
                        continue;

                    any = true;
                    AddCharacterRow(header, character);
                }

                if (!any)
                    return;

                headers.Add(header);

                header.UpdateRowCount();
                header.UpdateState();
            }

            // Heroes are only shown in universal mode.
            AddSection(
                headers,
                "heroes",
                "list_header_heroes",
                L.S("list_header_heroes", "Heroes"),
                faction.RosterHeroes,
                condition: () =>
                    faction is WClan && EditorState.Instance.Mode == EditorMode.Universal
            );

            // Retinues: only meaningful in player mode (custom only).
            if (
                EditorState.Instance.Mode == EditorMode.Player
                && faction is WClan
                && Settings.EnableRetinues
            )
            {
                var header = new CharacterListHeaderVM(
                    list,
                    "retinues",
                    L.S("list_header_retinues", "Retinues")
                )
                {
                    IsExpanded = true,
                };

                var any = false;

                foreach (var character in faction.RosterRetinues)
                {
                    if (!ShouldInclude(character))
                        continue;

                    any = true;
                    AddCharacterRow(header, character);
                }

                // Append "unlock in progress" rows (always at the bottom).
                var progress = RetinuesBehavior.GetSnapshot();
                // Sort by progress DESC.
                foreach (var (culture, points) in progress.OrderByDescending(x => x.Progress))
                {
                    if (culture == null)
                        continue;

                    if (points <= 0)
                        continue;

                    any = true;
                    header.AddRow(new RetinueUnlockProgressRowVM(header, culture, points));
                }

                if (any)
                {
                    headers.Add(header);
                    header.UpdateRowCount();
                    header.UpdateState();
                }
            }

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
                condition: () => faction is WCulture
            );

            // Apply headers in one operation.
            list.SetHeaders(headers);
        }

        /// <summary>
        /// Adds a character row to the given header.
        /// </summary>
        private void AddCharacterRow(ListHeaderVM header, WCharacter character)
        {
            if (character == null)
                return;

            if (character.IsHero)
                header.AddRow(new HeroListRowVM(header, character));
            else
                header.AddRow(new CharacterListRowVM(header, character));
        }
    }
}
