using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Shared.Views
{
    /// <summary>
    /// Partial class for base list ViewModel handling sorting.
    /// </summary>
    public abstract partial class BaseListVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const int SortButtonsTotalWidth = 588;

        private MBBindingList<ListSortButtonVM> _sortButtons = [];

        [DataSourceProperty]
        public MBBindingList<ListSortButtonVM> SortButtons
        {
            get => _sortButtons;
            protected set
            {
                if (ReferenceEquals(value, _sortButtons))
                    return;

                _sortButtons = value ?? [];
                OnPropertyChanged(nameof(SortButtons));

                RecomputeSortButtonProperties();
            }
        }

        /// <summary>
        /// Returns true when rows of the given header should be tree-sorted.
        /// </summary>
        protected virtual bool IsTreeSortHeader(string headerId) => false;

        /// <summary>
        /// Handles a sort button click.
        /// </summary>
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

        /// <summary>
        /// Re-sorts all headers based on the active sort button (if any).
        /// </summary>
        private void OnSortChanged()
        {
            if (_headers.Count == 0)
                return;

            // Never auto-scroll to selected row because of sorting.
            AutoScrollRowsEnabled = false;

            var active = _sortButtons.FirstOrDefault(b =>
                b.IsSortedAscending || b.IsSortedDescending
            );

            if (active == null)
            {
                var expansion = CaptureExpansion();
                Build();
                RestoreExpansion(expansion);
                ApplyFilter();
                return;
            }

            var sortKey = active.SortKey;
            var ascending = active.IsSortedAscending;

            for (int i = 0; i < _headers.Count; i++)
            {
                var header = _headers[i];
                if (header == null || header.Rows.Count <= 1)
                    continue;

                if (IsTreeSortHeader(header.Id))
                    SortTreeHeader(header, sortKey, ascending);
                else
                    SortFlatHeader(header, sortKey, ascending);
            }
        }

        /// <summary>
        /// Sorts a header's rows in flat mode.
        /// </summary>
        private void SortFlatHeader(ListHeaderVM header, ListSortKey sortKey, bool ascending)
        {
            if (header == null || header.Rows.Count <= 1)
                return;

            // Partition rows into normal vs pinned rows.
            var normal = new List<BaseListRowVM>(header.Rows.Count);
            var pinned = new List<BaseListRowVM>();
            var pinnedProgress = new Dictionary<BaseListRowVM, int>();

            for (int i = 0; i < header.Rows.Count; i++)
            {
                var row = header.Rows[i];
                if (row == null)
                    continue;

                if (row.TryGetPinnedSortProgress(out var p))
                {
                    pinned.Add(row);
                    pinnedProgress[row] = p;
                }
                else
                {
                    normal.Add(row);
                }
            }

            // Sort normal rows by active sort key.
            var sortedNormal = ascending
                ? normal.OrderBy(r => r.GetSortValue(sortKey)).ToList()
                : [.. normal.OrderByDescending(r => r.GetSortValue(sortKey))];

            // Pinned rows always:
            // - pinned to end
            // - ordered by progress DESC
            // - tie-break by Name ASC
            var sortedPinned = pinned
                .OrderByDescending(r => pinnedProgress.TryGetValue(r, out var p) ? p : 0)
                .ThenBy(r => r.GetSortValue(ListSortKey.Name))
                .ToList();

            var sorted = new List<BaseListRowVM>(sortedNormal.Count + sortedPinned.Count);
            sorted.AddRange(sortedNormal);
            sorted.AddRange(sortedPinned);

            header.Rows.Clear();
            for (int i = 0; i < sorted.Count; i++)
                header.Rows.Add(sorted[i]);

            if (header.IsExpanded)
            {
                header.ExpandedRows.Clear();
                for (int i = 0; i < sorted.Count; i++)
                    header.ExpandedRows.Add(sorted[i]);
            }
        }

        /// <summary>
        /// Sorts a header's rows in tree mode.
        /// </summary>
        private void SortTreeHeader(ListHeaderVM header, ListSortKey sortKey, bool ascending)
        {
            if (header == null || header.Rows.Count <= 1)
                return;

            var treeRows = header.Rows.Where(r => r != null && r.IsTreeNode).ToList();
            if (treeRows.Count != header.Rows.Count)
            {
                // Mixed content; fall back to flat sorting.
                SortFlatHeader(header, sortKey, ascending);
                return;
            }

            var rowById = new Dictionary<string, BaseListRowVM>(StringComparer.Ordinal);
            for (int i = 0; i < treeRows.Count; i++)
            {
                var row = treeRows[i];
                var id = row.Id;
                if (string.IsNullOrEmpty(id))
                    continue;

                // Avoid exceptions on duplicate keys; last wins, order-independent for our use.
                rowById[id] = row;
            }

            if (rowById.Count == 0)
                return;

            // Roots for this header: nodes whose parents are not present in this header.
            var roots = new List<string>();
            foreach (var kvp in rowById)
            {
                var id = kvp.Key;
                var row = kvp.Value;

                var parents = row.GetTreeParentIds();
                var hasParentInHeader = false;

                if (parents != null)
                {
                    foreach (var parentId in parents)
                    {
                        if (!string.IsNullOrEmpty(parentId) && rowById.ContainsKey(parentId))
                        {
                            hasParentInHeader = true;
                            break;
                        }
                    }
                }

                if (!hasParentInHeader)
                    roots.Add(id);
            }

            if (roots.Count == 0)
            {
                // Defensive fallback; shouldn't happen for well-formed trees.
                SortFlatHeader(header, sortKey, ascending);
                return;
            }

            var orderedRoots = ascending
                ? roots.OrderBy(id => rowById[id].GetSortValue(sortKey))
                : roots.OrderByDescending(id => rowById[id].GetSortValue(sortKey));

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var newOrder = new List<BaseListRowVM>(header.Rows.Count);

            foreach (var rootId in orderedRoots)
                VisitTreeNode(rootId, sortKey, ascending, rowById, visited, newOrder);

            // Safety: if something was not reachable from any root (cycles / stray nodes),
            // append it at the end, preserving tree semantics as much as possible.
            if (visited.Count < rowById.Count)
            {
                foreach (var id in rowById.Keys)
                {
                    if (!visited.Contains(id))
                        VisitTreeNode(id, sortKey, ascending, rowById, visited, newOrder);
                }
            }

            header.Rows.Clear();
            for (int i = 0; i < newOrder.Count; i++)
                header.Rows.Add(newOrder[i]);

            if (header.IsExpanded)
            {
                header.ExpandedRows.Clear();
                for (int i = 0; i < newOrder.Count; i++)
                    header.ExpandedRows.Add(newOrder[i]);
            }
        }

        /// <summary>
        /// Visits a tree node and its children in sort order, adding them to output.
        /// </summary>
        private void VisitTreeNode(
            string id,
            ListSortKey sortKey,
            bool ascending,
            Dictionary<string, BaseListRowVM> rowById,
            HashSet<string> visited,
            List<BaseListRowVM> output
        )
        {
            if (string.IsNullOrEmpty(id) || !rowById.ContainsKey(id))
                return;

            if (!visited.Add(id))
                return;

            var row = rowById[id];
            output.Add(row);

            var childrenRaw = row.GetTreeChildIds();
            if (childrenRaw == null)
                return;

            var children = new List<string>();
            foreach (var childId in childrenRaw)
            {
                if (!string.IsNullOrEmpty(childId) && rowById.ContainsKey(childId))
                    children.Add(childId);
            }

            if (children.Count == 0)
                return;

            var orderedChildren = ascending
                ? children.OrderBy(cid => rowById[cid].GetSortValue(sortKey))
                : children.OrderByDescending(cid => rowById[cid].GetSortValue(sortKey));

            foreach (var childId in orderedChildren)
                VisitTreeNode(childId, sortKey, ascending, rowById, visited, output);
        }

        /// <summary>
        /// Recomputes the sort button widths and last-column states.
        /// </summary>
        public void RecomputeSortButtonProperties()
        {
            if (_sortButtons.Count == 0)
                return;

            var totalRequested = _sortButtons.Sum(b => Math.Max(1, b.RequestedWidth));
            if (totalRequested <= 0)
                totalRequested = _sortButtons.Count;

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
                        width = 1;

                    remaining -= width;
                }

                button.Width = width;
                button.SetIsLastColumn(i == _sortButtons.Count - 1);
            }
        }
    }
}
