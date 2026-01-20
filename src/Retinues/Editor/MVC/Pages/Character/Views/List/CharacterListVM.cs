using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Retinues;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Components;
using Retinues.Interface.Services;

namespace Retinues.Editor.MVC.Pages.Character.Views.List
{
    /// <summary>
    /// Character list ViewModel.
    /// Builds headers/rows for the active faction roster and upgrade trees.
    /// </summary>
    public sealed class CharacterListVM : BaseListVM
    {
        protected override EditorPage Page => EditorPage.Character;

        /* ━━━━━━━━━ Test ━━━━━━━━━ */

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true when the specified header should use tree sorting.
        /// </summary>
        protected override bool IsTreeSortHeader(string headerId)
        {
            return headerId == "elite"
                || headerId == "regular"
                || headerId == "mercenaries"
                || headerId == "bandits";
        }

        /// <summary>
        /// Returns true when the specified header should use tree filtering.
        /// </summary>
        protected override bool IsTreeFilterHeader(string headerId)
        {
            return headerId == "elite" || headerId == "regular";
        }

        /// <summary>
        /// Returns the tooltip content for the list filter box.
        /// </summary>
        protected override Tooltip GetFilterTooltip()
        {
            return new(
                L.S(
                    "filter_tooltip_description_character",
                    "Type to filter the list by name, culture or tier."
                )
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Selection Sync                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string _lastSelectedRowId;

        /// <summary>
        /// Finds a row by id and returns its containing header.
        /// </summary>
        private bool TryFindRow(string id, out ListHeaderVM header, out BaseListRowVM row)
        {
            header = null;
            row = null;

            if (string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < Headers.Count; i++)
            {
                var h = Headers[i];
                if (h == null)
                    continue;

                var rows = h.Rows;
                if (rows == null || rows.Count == 0)
                    continue;

                for (int r = 0; r < rows.Count; r++)
                {
                    var rr = rows[r];
                    if (rr == null)
                        continue;

                    if (rr.Id == id)
                    {
                        header = h;
                        row = rr;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Ensures the newly selected character row is visible, then triggers list auto-scroll.
        /// </summary>
        [EventListener(UIEvent.Character)]
        private void OnCharacterChange()
        {
            if (State.Page != Page)
                return;

            var selected = State.Character;
            if (selected == null)
                return;

            var newId = selected.StringId;
            if (string.IsNullOrEmpty(newId))
                return;

            // 1) Find the actual row for the newly selected character and open its header first.
            if (TryFindRow(newId, out var newHeader, out var newRow))
            {
                if (newHeader != null && !newHeader.IsExpanded)
                    newHeader.IsExpanded = true;

                // If you have tree filtering that can hide parents, refresh visibility (cheap now).
                // Only do it if you know you're in a tree-filter header.
                if (newHeader != null && IsTreeFilterHeader(newHeader.Id))
                    ApplyFilter();
            }

            // 2) Force selection bindings to update BEFORE we bump AutoScrollVersion.
            // This avoids "scroll to previous selection" when this listener runs before row listeners.
            var oldId = _lastSelectedRowId;
            _lastSelectedRowId = newId;

            if (
                !string.IsNullOrEmpty(oldId)
                && oldId != newId
                && TryFindRow(oldId, out _, out var oldRow)
            )
                oldRow?.NotifySelectionChanged();

            if (newRow != null)
                newRow.NotifySelectionChanged();

            // 3) Now trigger autoscroll (list-driven, correct selection state).
            AutoScrollRowsEnabled = true;
            AutoScrollVersion++;
        }

        /// <summary>
        /// Expands the tree path to the selected row when tree filtering is active.
        /// </summary>
        private void ExpandTreePathToSelected(ListHeaderVM header, WCharacter selected)
        {
            if (header == null || selected == null)
                return;

            // We need the row for selected, then open all ancestors in that header.
            // Note: "tree" in this list is implemented through filtering, not nested UI nodes,
            // but we still need to ensure the rows that make the path are visible.
            // Your existing BaseListVM tree filter logic uses parent ids; reuse that by forcing
            // a tree filter recompute for this header only.

            // The simplest robust option: re-apply the current filter (no-op when empty)
            // which recomputes visibility of tree-related rows for this header.
            ApplyFilter();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Rebuilds the list when the upgrade tree or doctrine state changes.
        /// </summary>
        [EventListener(UIEvent.Tree, UIEvent.Doctrine)]
        private void OnTreeChange()
        {
            if (State.Page != Page)
                return;

            Build();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Building                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Builds headers and rows for the current faction and editor mode.
        /// </summary>
        public override void Build()
        {
            BuildSortButtons();
            BuildSections();
            RecomputeHeaderStates();
        }

        /// <summary>
        /// Builds the sort button set used by the character list.
        /// </summary>
        private void BuildSortButtons()
        {
            SortButtons.Clear();

            SortButtons.Add(
                new ListSortButtonVM(this, ListSortKey.Name, L.S("sort_by_name", "Name"), 3)
            );
            SortButtons.Add(
                new ListSortButtonVM(this, ListSortKey.Tier, L.S("sort_by_tier", "Tier"), 1)
            );

            RecomputeSortButtonProperties();
        }

        /// <summary>
        /// Builds all list headers and populates them with character rows.
        /// </summary>
        private void BuildSections()
        {
            // Single binding update instead of N inserts.
            var headers = new List<ListHeaderVM>();

            var faction = EditorState.Instance.Faction;
            if (faction == null)
            {
                SetHeaders(headers);
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

            // If there are a lot of troops, start "secondary" categories collapsed to reduce clutter.
            int CountTroops(IEnumerable<WCharacter> chars)
            {
                if (chars == null)
                    return 0;

                int n = 0;
                foreach (var c in chars)
                {
                    if (!ShouldInclude(c))
                        continue;

                    if (c.IsHero)
                        continue;

                    n++;
                }

                return n;
            }

            int totalTroops =
                CountTroops(faction.RosterRetinues)
                + CountTroops(faction.RootElite?.Tree)
                + CountTroops(faction.RootBasic?.Tree)
                + CountTroops(faction.RosterMilitia)
                + CountTroops(faction.RosterCaravan)
                + CountTroops(faction.RosterVillager)
                + CountTroops(faction.RosterMercenary)
                + CountTroops(faction.RosterBandit)
                + CountTroops(faction.RosterCivilian);

            bool collapseSecondaryHeaders = totalTroops > 40;

            bool ShouldStartExpanded(string headerId)
            {
                // Always keep main groups open.
                if (headerId == "retinues" || headerId == "elite" || headerId == "regular")
                    return true;

                // Otherwise, collapse when there are many troops.
                return !collapseSecondaryHeaders;
            }

            void AddSection(
                List<ListHeaderVM> headersList,
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

                var header = new ListHeaderVM(this, headerId, L.S(headerLocKey, headerFallback))
                {
                    IsExpanded = ShouldStartExpanded(headerId),
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

                headersList.Add(header);

                header.UpdateRowCount();
                header.UpdateState();
            }

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                         Heroes                         //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                        Retinues                        //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            // Retinues: only meaningful in player mode (custom only).
            if (
                EditorState.Instance.Mode == EditorMode.Player
                && faction is WClan
                && Settings.EnableRetinues
            )
            {
                var header = new ListHeaderVM(
                    this,
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

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                          Trees                         //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                    Secondary Rosters                   //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            // Militia.
            AddSection(
                headers,
                "militia",
                "list_header_militia",
                L.S("list_header_militia", "Militia"),
                faction.RosterMilitia,
                condition: () => faction is WCulture || faction.RosterMilitia.Count > 0
            );

            // Caravan.
            AddSection(
                headers,
                "caravan",
                "list_header_caravan",
                L.S("list_header_caravan", "Caravan"),
                faction.RosterCaravan,
                condition: () => faction is WCulture || faction.RosterCaravan.Count > 0
            );

            // Villagers.
            AddSection(
                headers,
                "villagers",
                "list_header_villagers",
                L.S("list_header_villagers", "Villagers"),
                faction.RosterVillager,
                condition: () => faction is WCulture || faction.RosterVillager.Count > 0
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
            SetHeaders(headers);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Row Creation                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds the appropriate row type for the given character into the header.
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
