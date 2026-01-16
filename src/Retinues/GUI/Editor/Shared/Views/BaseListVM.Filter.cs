using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.GUI.Components;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Services;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Shared.Views
{
    public abstract partial class BaseListVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Filter                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
        public Tooltip ClearFilterTooltip =>
            new(L.S("clear_filter_tooltip", "Clear the current filter text."));

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
        private void ApplyFilter()
        {
            if (_headers.Count == 0)
                return;

            var filter = _filterText?.Trim() ?? string.Empty;

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

                return;
            }

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

            // Second pass: for tree headers, ensure ancestors of matching nodes are visible,
            // to avoid orphan children.
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
    }
}
