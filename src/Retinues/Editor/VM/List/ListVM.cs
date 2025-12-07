using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Editor.VM.List.Rows;
using Retinues.Utilities;
using Retinues.Wrappers.Characters;
using Retinues.Wrappers.Factions;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    public enum ListSortKey
    {
        Name,
        Tier,
    }

    /// <summary>
    /// Main list ViewModel; driven by shared faction and character state.
    /// </summary>
    public class ListVM : BaseStatefulVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const int SortButtonsTotalWidth = 586;

        private MBBindingList<ListHeaderVM> _headers = [];
        private readonly Dictionary<ListHeaderVM, string> _headerIds = new();

        private ListRowVM _selectedElement;

        private MBBindingList<ListSortButtonVM> _sortButtons = [];
        private string _filterText = string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ListVM()
        {
            FactionChanged += OnStateFactionChanged;
            CharacterChanged += OnStateCharacterChanged;

            RebuildSortButtons();
        }

        public override void OnFinalize()
        {
            FactionChanged -= OnStateFactionChanged;
            CharacterChanged -= OnStateCharacterChanged;
            base.OnFinalize();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Headers / Selection                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<ListHeaderVM> Headers
        {
            get => _headers;
            private set
            {
                if (ReferenceEquals(value, _headers))
                {
                    return;
                }

                _headers = value;
                OnPropertyChanged(nameof(Headers));
            }
        }

        [DataSourceProperty]
        public ListRowVM SelectedElement
        {
            get => _selectedElement;
            private set
            {
                if (ReferenceEquals(value, _selectedElement))
                {
                    return;
                }

                _selectedElement = value;
                OnPropertyChanged(nameof(SelectedElement));
            }
        }

        public ListHeaderVM AddHeader(string id, string name)
        {
            var header = new ListHeaderVM(this, id, name);

            // Insert at index 0 because the list is displayed in reverse.
            _headers.Insert(0, header);
            _headerIds[header] = id;

            return header;
        }

        public void Clear()
        {
            foreach (var header in _headers)
            {
                header.Elements.Clear();
            }

            _headers.Clear();
            _headerIds.Clear();
            SelectedElement = null;
        }

        internal void OnElementSelected(ListRowVM element)
        {
            foreach (var header in _headers)
            {
                header.ClearSelectionExcept(element);
            }

            SelectedElement = element;

            if (element is CharacterRowVM characterRow)
            {
                StateCharacter = characterRow.Character;
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            foreach (var header in _headers)
            {
                header.RefreshValues();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     State / Rebuild                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void RebuildSortButtons()
        {
            _sortButtons.Clear();

            switch (EditorVM.Mode)
            {
                case EditorMode.Character:
                default:
                    // Character mode: Name (weight 3), Tier (weight 1).
                    AddSortButton(ListSortKey.Name, L.S("sort_by_name", "Name"), 3);
                    AddSortButton(ListSortKey.Tier, L.S("sort_by_tier", "Tier"), 1);
                    break;
            }

            SetDynamicButtonProperties();
        }

        private void OnStateFactionChanged(IBaseFaction faction)
        {
            if (EditorVM.Mode == EditorMode.Character)
            {
                RebuildFromFaction(faction);
            }
        }

        private void RebuildFromFaction(IBaseFaction faction)
        {
            Clear();

            if (faction == null)
            {
                return;
            }

            WCharacter firstCharacter = null;

            static void AddSection(
                ListVM list,
                ref WCharacter first,
                string headerId,
                string headerLocKey,
                string headerFallback,
                System.Collections.Generic.IEnumerable<WCharacter> troops,
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
                this,
                ref firstCharacter,
                "retinues",
                "list_header_retinues",
                L.S("list_header_retinues", "Retinues"),
                faction.RosterRetinues
            );

            // Elite tree.
            AddSection(
                this,
                ref firstCharacter,
                "elite",
                "list_header_elite",
                L.S("list_header_elite", "Elite"),
                faction.RootElite?.Tree
            );

            // Regular tree.
            AddSection(
                this,
                ref firstCharacter,
                "regular",
                "list_header_regular",
                L.S("list_header_regular", "Regular"),
                faction.RootBasic?.Tree
            );

            // Militia.
            AddSection(
                this,
                ref firstCharacter,
                "militia",
                "list_header_militia",
                L.S("list_header_militia", "Militia"),
                faction.RosterMilitia
            );

            // Caravan.
            AddSection(
                this,
                ref firstCharacter,
                "caravan",
                "list_header_caravan",
                L.S("list_header_caravan", "Caravan"),
                faction.RosterCaravan
            );

            // Villagers.
            AddSection(
                this,
                ref firstCharacter,
                "villagers",
                "list_header_villagers",
                L.S("list_header_villagers", "Villagers"),
                faction.RosterVillager
            );

            // Bandits.
            AddSection(
                this,
                ref firstCharacter,
                "bandits",
                "list_header_bandits",
                L.S("list_header_bandits", "Bandits"),
                faction.RosterBandit
            );

            // Civilians.
            AddSection(
                this,
                ref firstCharacter,
                "civilians",
                "list_header_civilians",
                L.S("list_header_civilians", "Civilians"),
                faction.RosterCivilian,
                civilian: true
            );

            if (StateCharacter == null && firstCharacter != null)
            {
                StateCharacter = firstCharacter;
            }
            else if (StateCharacter != null)
            {
                UpdateSelectionFromCharacter(StateCharacter);
            }

            RefreshValues();
        }

        private void OnStateCharacterChanged(WCharacter character)
        {
            UpdateSelectionFromCharacter(character);
        }

        private void UpdateSelectionFromCharacter(WCharacter character)
        {
            if (_headers.Count == 0)
            {
                return;
            }

            if (character == null)
            {
                foreach (var header in _headers)
                {
                    header.ClearSelectionExcept(null);
                }

                SelectedElement = null;
                return;
            }

            ListRowVM match = null;

            foreach (var header in _headers)
            {
                foreach (var element in header.Elements)
                {
                    if (element is CharacterRowVM row && ReferenceEquals(row.Character, character))
                    {
                        match = element;
                        break;
                    }
                }

                if (match != null)
                {
                    break;
                }
            }

            if (match != null)
            {
                OnElementSelected(match);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<ListSortButtonVM> SortButtons
        {
            get => _sortButtons;
            private set
            {
                if (ReferenceEquals(value, _sortButtons))
                {
                    return;
                }

                _sortButtons = value;
                OnPropertyChanged(nameof(SortButtons));

                SetDynamicButtonProperties();
            }
        }

        public ListSortButtonVM AddSortButton(ListSortKey sortKey, string text, int relativeWidth)
        {
            var button = new ListSortButtonVM(this, sortKey, text, relativeWidth);
            _sortButtons.Add(button);

            SetDynamicButtonProperties();

            return button;
        }

        internal void OnSortButtonClicked(ListSortButtonVM clicked)
        {
            if (clicked == null)
            {
                return;
            }

            foreach (var button in _sortButtons)
            {
                if (ReferenceEquals(button, clicked))
                {
                    button.CycleSortState();
                }
                else
                {
                    button.ResetSortState();
                }
            }

            OnSortChanged();
        }

        private static readonly string[] TreeAwareSortHeaders = ["elite", "regular"];

        private void OnSortChanged()
        {
            if (_headers.Count == 0)
            {
                return;
            }

            // Find the active sort button (if any).
            var active = _sortButtons.FirstOrDefault(b =>
                b.IsSortedAscending || b.IsSortedDescending
            );

            if (active == null)
            {
                // No active sort: revert to default ordering for the current mode.
                if (EditorVM.Mode == EditorMode.Character)
                {
                    OnStateFactionChanged(StateFaction);
                }

                return;
            }

            var sortKey = active.SortKey;
            var ascending = active.IsSortedAscending;

            foreach (var header in _headers)
            {
                if (header.Elements.Count <= 1)
                {
                    continue;
                }

                // Only Elite / Regular use tree-aware sorting; everything else stays flat.
                var isTreeHeader =
                    _headerIds.TryGetValue(header, out var headerId)
                    && TreeAwareSortHeaders.Contains(headerId);

                if (isTreeHeader)
                {
                    SortTreeHeader(header, sortKey, ascending);
                }
                else
                {
                    SortFlatHeader(header, sortKey, ascending);
                }
            }
        }

        private void SortFlatHeader(ListHeaderVM header, ListSortKey sortKey, bool ascending)
        {
            if (header.Elements.Count <= 1)
            {
                return;
            }

            var sorted = ascending
                ? header.Elements.OrderBy(row => row.GetSortValue(sortKey)).ToList()
                : [.. header.Elements.OrderByDescending(row => row.GetSortValue(sortKey))];

            header.Elements.Clear();
            for (int i = 0; i < sorted.Count; i++)
            {
                header.Elements.Add(sorted[i]);
            }
        }

        private void SortTreeHeader(ListHeaderVM header, ListSortKey sortKey, bool ascending)
        {
            if (header.Elements.Count <= 1)
            {
                return;
            }

            // We only support tree sorting when all elements are character rows.
            var characterRows = header.Elements.OfType<CharacterRowVM>().ToList();
            if (characterRows.Count != header.Elements.Count)
            {
                // Mixed content; fall back to flat sorting.
                SortFlatHeader(header, sortKey, ascending);
                return;
            }

            // Map characters to their row.
            var rowByCharacter = new Dictionary<WCharacter, CharacterRowVM>();
            for (int i = 0; i < characterRows.Count; i++)
            {
                var row = characterRows[i];
                var character = row.Character;
                if (character == null)
                {
                    continue;
                }

                // Avoid exceptions on duplicate keys; last wins, order-independent for our use.
                rowByCharacter[character] = row;
            }

            if (rowByCharacter.Count == 0)
            {
                return;
            }

            // Roots for this header: characters whose parents (UpgradeSources)
            // are not present in this header.
            var roots = new List<WCharacter>();
            foreach (var kvp in rowByCharacter)
            {
                var character = kvp.Key;
                var sources = character.UpgradeSources;
                var hasParentInHeader = false;

                if (sources != null)
                {
                    for (int i = 0; i < sources.Count; i++)
                    {
                        var parent = sources[i];
                        if (parent != null && rowByCharacter.ContainsKey(parent))
                        {
                            hasParentInHeader = true;
                            break;
                        }
                    }
                }

                if (!hasParentInHeader)
                {
                    roots.Add(character);
                }
            }

            if (roots.Count == 0)
            {
                // Defensive fallback; shouldn't happen for well-formed trees.
                SortFlatHeader(header, sortKey, ascending);
                return;
            }

            // Order roots themselves.
            var orderedRoots = ascending
                ? roots.OrderBy(c => rowByCharacter[c].GetSortValue(sortKey))
                : roots.OrderByDescending(c => rowByCharacter[c].GetSortValue(sortKey));

            var visited = new HashSet<WCharacter>();
            var newOrder = new List<ListRowVM>(header.Elements.Count);

            foreach (var root in orderedRoots)
            {
                VisitTreeNode(root, sortKey, ascending, rowByCharacter, visited, newOrder);
            }

            // Safety: if something was not reachable from any root (cycles / stray nodes),
            // append it at the end, preserving tree semantics as much as possible.
            if (visited.Count < rowByCharacter.Count)
            {
                foreach (var character in rowByCharacter.Keys)
                {
                    if (!visited.Contains(character))
                    {
                        VisitTreeNode(
                            character,
                            sortKey,
                            ascending,
                            rowByCharacter,
                            visited,
                            newOrder
                        );
                    }
                }
            }

            header.Elements.Clear();
            for (int i = 0; i < newOrder.Count; i++)
            {
                header.Elements.Add(newOrder[i]);
            }
        }

        private void VisitTreeNode(
            WCharacter character,
            ListSortKey sortKey,
            bool ascending,
            Dictionary<WCharacter, CharacterRowVM> rowByCharacter,
            HashSet<WCharacter> visited,
            List<ListRowVM> output
        )
        {
            if (character == null || !rowByCharacter.ContainsKey(character))
            {
                return;
            }

            // Prevent infinite loops / duplicates on cycles.
            if (!visited.Add(character))
            {
                return;
            }

            output.Add(rowByCharacter[character]);

            var childrenRaw = character.UpgradeTargets;
            if (childrenRaw == null || childrenRaw.Count == 0)
            {
                return;
            }

            // Filter children to those present in this header.
            var children = new List<WCharacter>();
            for (int i = 0; i < childrenRaw.Count; i++)
            {
                var child = childrenRaw[i];
                if (child != null && rowByCharacter.ContainsKey(child))
                {
                    children.Add(child);
                }
            }

            if (children.Count == 0)
            {
                return;
            }

            var orderedChildren = ascending
                ? children.OrderBy(c => rowByCharacter[c].GetSortValue(sortKey))
                : children.OrderByDescending(c => rowByCharacter[c].GetSortValue(sortKey));

            foreach (var child in orderedChildren)
            {
                VisitTreeNode(child, sortKey, ascending, rowByCharacter, visited, output);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Filter                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string FilterLabel => L.S("filter_label", "Filter:");

        [DataSourceProperty]
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (string.Equals(value, _filterText, StringComparison.Ordinal))
                {
                    return;
                }

                _filterText = value ?? string.Empty;
                OnPropertyChanged(nameof(FilterText));

                ApplyFilter();
            }
        }

        private static readonly string[] TreeAwareFilterHeaders = ["elite", "regular"];

        private void ApplyFilter()
        {
            if (_headers.Count == 0)
            {
                return;
            }

            var filter = _filterText?.Trim() ?? string.Empty;

            // Empty filter: show everything.
            if (string.IsNullOrWhiteSpace(filter))
            {
                foreach (var header in _headers)
                {
                    foreach (var row in header.Elements)
                    {
                        row.IsVisible = true;
                    }
                }

                return;
            }

            // First pass: each row decides on its own.
            foreach (var header in _headers)
            {
                foreach (var row in header.Elements)
                {
                    row.IsVisible = row.MatchesFilter(filter);
                }
            }

            // Second pass: for tree headers (elite/regular), ensure ancestors
            // of matching nodes are visible as well, so you don't get orphan children.
            foreach (var header in _headers)
            {
                if (
                    !_headerIds.TryGetValue(header, out var headerId)
                    || !TreeAwareFilterHeaders.Contains(headerId)
                )
                {
                    continue;
                }

                var characterRows = header.Elements.OfType<CharacterRowVM>().ToList();

                if (characterRows.Count == 0)
                {
                    continue;
                }

                var rowByCharacter = new Dictionary<WCharacter, CharacterRowVM>();
                foreach (var row in characterRows)
                {
                    var character = row.Character;
                    if (character == null)
                    {
                        continue;
                    }

                    rowByCharacter[character] = row;
                }

                if (rowByCharacter.Count == 0)
                {
                    continue;
                }

                var visited = new HashSet<WCharacter>();

                foreach (var row in characterRows)
                {
                    if (!row.IsVisible)
                    {
                        continue;
                    }

                    var character = row.Character;
                    if (character == null)
                    {
                        continue;
                    }

                    // Walk up all parents, making them visible too.
                    var queue = new Queue<WCharacter>();
                    var sources = character.UpgradeSources;

                    if (sources != null)
                    {
                        for (int i = 0; i < sources.Count; i++)
                        {
                            var parent = sources[i];
                            if (parent != null)
                            {
                                queue.Enqueue(parent);
                            }
                        }
                    }

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        if (current == null || !visited.Add(current))
                        {
                            continue;
                        }

                        if (rowByCharacter.TryGetValue(current, out var parentRow))
                        {
                            parentRow.IsVisible = true;
                        }

                        var parentSources = current.UpgradeSources;
                        if (parentSources == null)
                        {
                            continue;
                        }

                        for (int i = 0; i < parentSources.Count; i++)
                        {
                            var grandParent = parentSources[i];
                            if (grandParent != null)
                            {
                                queue.Enqueue(grandParent);
                            }
                        }
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void SetDynamicButtonProperties()
        {
            if (_sortButtons.Count == 0)
            {
                return;
            }

            var totalRequested = _sortButtons.Sum(b => Math.Max(1, b.RequestedWidth));
            if (totalRequested <= 0)
            {
                totalRequested = _sortButtons.Count;
            }

            var remaining = SortButtonsTotalWidth;

            for (var i = 0; i < _sortButtons.Count; i++)
            {
                var button = _sortButtons[i];
                var request = Math.Max(1, button.RequestedWidth);

                int width;
                if (i == _sortButtons.Count - 1)
                {
                    width = Math.Max(1, remaining);
                }
                else
                {
                    var fraction = (double)request / totalRequested;
                    width = (int)Math.Round(SortButtonsTotalWidth * fraction);
                    if (width < 1)
                    {
                        width = 1;
                    }

                    remaining -= width;
                }

                button.SetNormalizedWidth(width);
                button.SetIsLastColumn(i == _sortButtons.Count - 1);
            }
        }
    }
}
