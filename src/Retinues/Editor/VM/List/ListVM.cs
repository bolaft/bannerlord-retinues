using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.VM.List.Character;
using Retinues.Editor.VM.List.Equipment;
using Retinues.Engine;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    /// <summary>
    /// Keys for sorting list rows.
    /// </summary>
    public enum ListSortKey
    {
        Name,
        Tier,
        Value,
        Category,
    }

    /// <summary>
    /// Main list ViewModel; driven by shared faction and character state.
    /// </summary>
    public class ListVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Life Cycle                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the appropriate list builder for the current editor mode.
        /// </summary>
        private BaseListBuilder Builder
        {
            get
            {
                return EditorVM.Mode switch
                {
                    EditorMode.Character => BaseListBuilder.GetInstance<CharacterListBuilder>(),
                    EditorMode.Equipment => BaseListBuilder.GetInstance<EquipmentListBuilder>(),
                    _ => throw new NotSupportedException(
                        $"No list builder for mode {EditorVM.Mode}."
                    ),
                };
            }
        }

        /// <summary>
        /// Clears all headers and their rows from the list.
        /// </summary>
        public void Clear()
        {
            foreach (var header in _headers)
                header.Rows.Clear();

            _headers.Clear();
            _headerIds.Clear();
        }

        /// <summary>
        /// On mode change, rebuild the list using the current builder.
        /// </summary>
        [EventListener(UIEvent.Mode)]
        private void OnModeChange()
        {
            Builder.Build(this);
        }

        /// <summary>
        /// On faction change, rebuild the list if in character mode.
        /// </summary>
        [EventListener(UIEvent.Faction)]
        private void OnFactionChange()
        {
            if (EditorVM.Mode == EditorMode.Character)
                Builder.Build(this);
        }

        EquipmentIndex _previousSlot = State.Slot;

        readonly EquipmentIndex[] WeaponSlots =
        [
            EquipmentIndex.Weapon0,
            EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2,
            EquipmentIndex.Weapon3,
        ];

        /// <summary>
        /// On slot change, rebuild the list if in equipment mode.
        /// </summary>
        [EventListener(UIEvent.Slot)]
        private void OnSlotChange()
        {
            if (EditorVM.Mode == EditorMode.Equipment)
            {
                if (State.Slot == _previousSlot)
                    return; // No change.

                if (WeaponSlots.Contains(State.Slot) && WeaponSlots.Contains(_previousSlot))
                    return; // Both old and new slots are weapon slots; no need to rebuild the entire list.

                Builder.Build(this);
                _previousSlot = State.Slot;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Headers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private MBBindingList<ListHeaderVM> _headers = [];
        private readonly Dictionary<ListHeaderVM, string> _headerIds = [];

        [DataSourceProperty]
        public MBBindingList<ListHeaderVM> Headers
        {
            get => _headers;
            private set
            {
                if (ReferenceEquals(value, _headers))
                    return;

                _headers = value;
                OnPropertyChanged(nameof(Headers));
            }
        }

        public void AddHeader(ListHeaderVM header)
        {
            // Insert at index 0 because the list is displayed in reverse.
            _headers.Insert(0, header);
            _headerIds[header] = header.Id;
        }

        internal int VisibleHeaderCount
        {
            get
            {
                var count = 0;

                for (int i = 0; i < _headers.Count; i++)
                {
                    if (_headers[i].HasVisibleRows)
                        count++;
                }

                return count;
            }
        }

        internal void RecomputeHeaderStates()
        {
            for (int i = 0; i < _headers.Count; i++)
                _headers[i]?.UpdateState();
        }

        internal void OnHeaderRowVisibilityChanged()
        {
            // Re-evaluate all headers because the "only one full header" rule depends on siblings.
            RecomputeHeaderStates();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly string[] TreeAwareSortHeaders = ["elite", "regular"];
        private const int SortButtonsTotalWidth = 586;

        private MBBindingList<ListSortButtonVM> _sortButtons = [];

        [DataSourceProperty]
        public MBBindingList<ListSortButtonVM> SortButtons
        {
            get => _sortButtons;
            private set
            {
                if (ReferenceEquals(value, _sortButtons))
                    return;

                _sortButtons = value;
                OnPropertyChanged(nameof(SortButtons));

                RecomputeSortButtonProperties();
            }
        }

        internal void OnSortButtonClicked(ListSortButtonVM clicked)
        {
            if (clicked == null)
                return;

            foreach (var button in _sortButtons)
            {
                if (ReferenceEquals(button, clicked))
                    button.CycleSortState();
                else
                    button.ResetSortState();
            }

            OnSortChanged();
        }

        private void OnSortChanged()
        {
            if (_headers.Count == 0)
                return;

            // Find the active sort button (if any).
            var active = _sortButtons.FirstOrDefault(b =>
                b.IsSortedAscending || b.IsSortedDescending
            );

            if (active == null)
            {
                // No active sort: revert to default ordering for the current mode.
                Builder.Build(this);
                return;
            }

            var sortKey = active.SortKey;
            var ascending = active.IsSortedAscending;

            foreach (var header in _headers)
            {
                if (header.Rows.Count <= 1)
                    continue;

                // Only Elite / Regular use tree-aware sorting; everything else stays flat.
                var isTreeHeader =
                    _headerIds.TryGetValue(header, out var headerId)
                    && TreeAwareSortHeaders.Contains(headerId);

                if (isTreeHeader)
                    SortTreeHeader(header, sortKey, ascending);
                else
                    SortFlatHeader(header, sortKey, ascending);
            }
        }

        private void SortFlatHeader(ListHeaderVM header, ListSortKey sortKey, bool ascending)
        {
            if (header.Rows.Count <= 1)
                return;

            var sorted = ascending
                ? header.Rows.OrderBy(row => row.GetSortValue(sortKey)).ToList()
                : [.. header.Rows.OrderByDescending(row => row.GetSortValue(sortKey))];

            header.Rows.Clear();
            for (int i = 0; i < sorted.Count; i++)
                header.Rows.Add(sorted[i]);
        }

        private void SortTreeHeader(ListHeaderVM header, ListSortKey sortKey, bool ascending)
        {
            if (header.Rows.Count <= 1)
                return;

            // We only support tree sorting when all rows are character rows.
            var characterRows = header.Rows.OfType<CharacterListRowVM>().ToList();
            if (characterRows.Count != header.Rows.Count)
            {
                // Mixed content; fall back to flat sorting.
                SortFlatHeader(header, sortKey, ascending);
                return;
            }

            // Map characters to their row.
            var rowByCharacter = new Dictionary<WCharacter, CharacterListRowVM>();
            for (int i = 0; i < characterRows.Count; i++)
            {
                var row = characterRows[i];
                var character = row.Character;
                if (character == null)
                    continue;

                // Avoid exceptions on duplicate keys; last wins, order-independent for our use.
                rowByCharacter[character] = row;
            }

            if (rowByCharacter.Count == 0)
                return;

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
                    for (int i = 0; i < sources.Count(); i++)
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
                    roots.Add(character);
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
            var newOrder = new List<ListRowVM>(header.Rows.Count);

            foreach (var root in orderedRoots)
                VisitTreeNode(root, sortKey, ascending, rowByCharacter, visited, newOrder);

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

            header.Rows.Clear();
            for (int i = 0; i < newOrder.Count; i++)
                header.Rows.Add(newOrder[i]);
        }

        private void VisitTreeNode(
            WCharacter character,
            ListSortKey sortKey,
            bool ascending,
            Dictionary<WCharacter, CharacterListRowVM> rowByCharacter,
            HashSet<WCharacter> visited,
            List<ListRowVM> output
        )
        {
            if (character == null || !rowByCharacter.ContainsKey(character))
                return;

            // Prevent infinite loops / duplicates on cycles.
            if (!visited.Add(character))
                return;

            output.Add(rowByCharacter[character]);

            var childrenRaw = character.UpgradeTargets;
            if (childrenRaw == null || childrenRaw.Count() == 0)
                return;

            // Filter children to those present in this header.
            var children = new List<WCharacter>();
            for (int i = 0; i < childrenRaw.Count(); i++)
            {
                var child = childrenRaw[i];
                if (child != null && rowByCharacter.ContainsKey(child))
                    children.Add(child);
            }

            if (children.Count == 0)
                return;

            var orderedChildren = ascending
                ? children.OrderBy(c => rowByCharacter[c].GetSortValue(sortKey))
                : children.OrderByDescending(c => rowByCharacter[c].GetSortValue(sortKey));

            foreach (var child in orderedChildren)
                VisitTreeNode(child, sortKey, ascending, rowByCharacter, visited, output);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Filter                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string FilterLabel => L.S("filter_label", "Filter:");

        private string _filterText = string.Empty;

        [DataSourceProperty]
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (string.Equals(value, _filterText, StringComparison.Ordinal))
                    return;

                _filterText = value ?? string.Empty;
                OnPropertyChanged(nameof(FilterText));

                ApplyFilter();
            }
        }

        [EventListener(UIEvent.Mode)]
        [DataSourceProperty]
        public Tooltip FilterTooltip
        {
            get
            {
                if (EditorVM.Mode == EditorMode.Character)
                {
                    return new(
                        L.S(
                            "filter_tooltip_description_character",
                            "Type to filter the list by name, culture or tier."
                        )
                    );
                }
                if (EditorVM.Mode == EditorMode.Equipment)
                {
                    return new(
                        L.S(
                            "filter_tooltip_description_equipment",
                            "Type to filter the list by name, category, culture or tier."
                        )
                    );
                }
                return new(L.S("filter_tooltip_description", "Type to filter the list."));
            }
        }

        [DataSourceProperty]
        public Tooltip ClearFilterTooltip =>
            new(L.S("clear_filter_tooltip", "Clear the current filter text."));

        [EventListener(UIEvent.Mode)]
        [DataSourceMethod]
        public void ExecuteClearFilter()
        {
            if (string.IsNullOrEmpty(_filterText))
                return;

            FilterText = string.Empty;
        }

        private static readonly string[] TreeAwareFilterHeaders = ["elite", "regular"];

        private void ApplyFilter()
        {
            if (_headers.Count == 0)
                return;

            var filter = _filterText?.Trim() ?? string.Empty;

            // Empty filter: show everything.
            if (string.IsNullOrWhiteSpace(filter))
            {
                foreach (var header in _headers)
                foreach (var row in header.Rows)
                    row.IsVisible = true;

                return;
            }

            // First pass: each row decides on its own.
            foreach (var header in _headers)
            foreach (var row in header.Rows)
                row.IsVisible = row.MatchesFilter(filter);

            // Second pass: for tree headers (elite/regular), ensure ancestors
            // of matching nodes are visible as well, so you don't get orphan children.
            foreach (var header in _headers)
            {
                if (
                    !_headerIds.TryGetValue(header, out var headerId)
                    || !TreeAwareFilterHeaders.Contains(headerId)
                )
                    continue;

                var characterRows = header.Rows.OfType<CharacterListRowVM>().ToList();

                if (characterRows.Count == 0)
                    continue;

                var rowByCharacter = new Dictionary<WCharacter, CharacterListRowVM>();
                foreach (var row in characterRows)
                {
                    var character = row.Character;
                    if (character == null)
                        continue;

                    rowByCharacter[character] = row;
                }

                if (rowByCharacter.Count == 0)
                    continue;

                var visited = new HashSet<WCharacter>();

                foreach (var row in characterRows)
                {
                    if (!row.IsVisible)
                        continue;

                    var character = row.Character;
                    if (character == null)
                        continue;

                    // Walk up all parents, making them visible too.
                    var queue = new Queue<WCharacter>();
                    var sources = character.UpgradeSources;

                    if (sources != null)
                    {
                        for (int i = 0; i < sources.Count(); i++)
                        {
                            var parent = sources[i];
                            if (parent != null)
                                queue.Enqueue(parent);
                        }
                    }

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        if (current == null || !visited.Add(current))
                            continue;

                        if (rowByCharacter.TryGetValue(current, out var parentRow))
                            parentRow.IsVisible = true;

                        var parentSources = current.UpgradeSources;
                        if (parentSources == null)
                            continue;

                        for (int i = 0; i < parentSources.Count(); i++)
                        {
                            var grandParent = parentSources[i];
                            if (grandParent != null)
                                queue.Enqueue(grandParent);
                        }
                    }
                }
            }
        }

        public void RecomputeSortButtonProperties()
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

                button.Width = width;
                button.SetIsLastColumn(i == _sortButtons.Count - 1);
            }
        }
    }
}
