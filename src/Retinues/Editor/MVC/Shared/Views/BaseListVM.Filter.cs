using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Events;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Shared.Views
{
    /// <summary>
    /// Partial class for base list ViewModel handling filtering.
    /// </summary>
    public abstract partial class BaseListVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Filter                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal bool IsRowVisibilityBatchActive { get; private set; }

        private void BeginRowVisibilityBatch() => IsRowVisibilityBatchActive = true;

        private void EndRowVisibilityBatch() => IsRowVisibilityBatchActive = false;

        /// <summary>
        /// Returns true when the given header uses tree filtering (ancestor visibility).
        /// </summary>
        protected virtual bool IsTreeFilterHeader(string headerId) => false;

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

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public Tooltip FilterTooltip => GetFilterTooltip();

        [DataSourceProperty]
        public Tooltip ClearFilterTooltip => new(L.S("clear_filter_tooltip", "Clear filter"));

        [EventListener(UIEvent.Page)]
        [DataSourceMethod]
        public void ExecuteClearFilter()
        {
            if (string.IsNullOrEmpty(_filterText))
                return;

            FilterText = string.Empty;
        }

        /// <summary>
        /// Applies the current filter text to all rows.
        /// </summary>
        protected virtual void ApplyFilter()
        {
            ApplyFilter_Default();
        }

        /// <summary>
        /// Default filtering strategy:
        /// - batch row visibility changes
        /// - recompute headers once at the end
        /// - then run tree ancestor expansion (also batched)
        /// </summary>
        protected void ApplyFilter_Default()
        {
            if (_headers.Count == 0)
                return;

            var filter = _filterText?.Trim() ?? string.Empty;

            BeginRowVisibilityBatch();
            try
            {
                // Empty filter: show everything.
                if (string.IsNullOrWhiteSpace(filter))
                {
                    for (int i = 0; i < _headers.Count; i++)
                    {
                        var header = _headers[i];
                        if (header == null)
                            continue;

                        for (int r = 0; r < header.Rows.Count; r++)
                        {
                            var row = header.Rows[r];
                            if (row != null)
                                row.IsVisible = true;
                        }
                    }
                }
                else
                {
                    // First pass: each row decides on its own.
                    for (int i = 0; i < _headers.Count; i++)
                    {
                        var header = _headers[i];
                        if (header == null)
                            continue;

                        for (int r = 0; r < header.Rows.Count; r++)
                        {
                            var row = header.Rows[r];
                            if (row != null)
                                row.IsVisible = row.MatchesFilter(filter);
                        }
                    }
                }

                // Second pass: for tree headers, ensure ancestors of matching nodes are visible.
                for (int i = 0; i < _headers.Count; i++)
                {
                    var header = _headers[i];
                    if (header == null)
                        continue;

                    if (!IsTreeFilterHeader(header.Id))
                        continue;

                    var treeRows = header.Rows.Where(r => r != null && r.IsTreeNode).ToList();
                    if (treeRows.Count == 0)
                        continue;

                    var rowById = new Dictionary<string, BaseListRowVM>(StringComparer.Ordinal);
                    for (int r = 0; r < treeRows.Count; r++)
                    {
                        var row = treeRows[r];
                        var id = row.Id;
                        if (!string.IsNullOrEmpty(id))
                            rowById[id] = row;
                    }

                    if (rowById.Count == 0)
                        continue;

                    var visited = new HashSet<string>(StringComparer.Ordinal);

                    for (int r = 0; r < treeRows.Count; r++)
                    {
                        var row = treeRows[r];
                        if (row == null || !row.IsVisible)
                            continue;

                        var queue = new Queue<string>();
                        var parents = row.GetTreeParentIds();

                        if (parents != null)
                        {
                            foreach (var pid in parents)
                            {
                                if (!string.IsNullOrEmpty(pid))
                                    queue.Enqueue(pid);
                            }
                        }

                        while (queue.Count > 0)
                        {
                            var currentId = queue.Dequeue();
                            if (string.IsNullOrEmpty(currentId) || !visited.Add(currentId))
                                continue;

                            if (rowById.TryGetValue(currentId, out var parentRow))
                                parentRow.IsVisible = true;

                            var nextParents = parentRow?.GetTreeParentIds();
                            if (nextParents == null)
                                continue;

                            foreach (var gp in nextParents)
                            {
                                if (!string.IsNullOrEmpty(gp))
                                    queue.Enqueue(gp);
                            }
                        }
                    }
                }
            }
            finally
            {
                EndRowVisibilityBatch();
            }

            // ONE recompute pass at the end (instead of per-row storms).
            RecomputeHeaderStates();
        }
    }
}
