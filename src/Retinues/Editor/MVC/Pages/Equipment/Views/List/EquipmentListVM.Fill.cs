using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.MVC.Pages.Equipment.Controllers;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Equipment.Views.List
{
    public sealed partial class EquipmentListVM
    {
        private void BuildSortButtons()
        {
            SortButtons.Clear();

            SortButtons.Add(
                new ListSortButtonVM(this, ListSortKey.Name, L.S("sort_by_name", "Name"), 2)
            );
            SortButtons.Add(
                new ListSortButtonVM(
                    this,
                    ListSortKey.Culture,
                    L.S("sort_by_culture", "Culture"),
                    2
                )
            );
            SortButtons.Add(
                new ListSortButtonVM(this, ListSortKey.Tier, L.S("sort_by_tier", "Tier"), 1)
            );
            SortButtons.Add(
                new ListSortButtonVM(this, ListSortKey.Value, L.S("sort_by_value", "Value"), 1)
            );

            RecomputeSortButtonProperties();
        }

        private void BuildSections()
        {
            EnsureItemCache();

            var headers = new List<ListHeaderVM>();
            var slot = State.Slot;

            _groupNonWeaponsByCategory = false;

            if (WeaponSlots.Contains(slot))
            {
                var types = new Dictionary<ItemObject.ItemTypeEnum, List<WItem>>();

                foreach (var item in _cachedVisibleItems)
                {
                    var type = item.Type;
                    if (!types.ContainsKey(type))
                        types[type] = [];

                    types[type].Add(item);
                }

                foreach (var kvp in types)
                {
                    headers.Add(
                        CreateHeader(
                            kvp.Key.ToString().ToLowerInvariant(),
                            Format.CamelCaseToTitle(kvp.Key.ToString()),
                            kvp.Value
                        )
                    );
                }
            }
            else if (slot == EquipmentIndex.Horse)
            {
                var categories = new Dictionary<string, List<WItem>>(StringComparer.Ordinal);

                foreach (var item in _cachedVisibleItems)
                {
                    var groupId = GetCategoryGroupId(slot, item);
                    if (!categories.TryGetValue(groupId, out var list))
                        categories[groupId] = list = [];

                    list.Add(item);
                }

                foreach (var kvp in categories)
                    headers.Add(CreateCategoryHeader(kvp.Key, kvp.Value));
            }
            else
            {
                var totalItems = _cachedVisibleItems?.Count ?? 0;

                if (totalItems > 100)
                {
                    _groupNonWeaponsByCategory = true;

                    var categories = new Dictionary<string, List<WItem>>(StringComparer.Ordinal);

                    foreach (var item in _cachedVisibleItems)
                    {
                        var groupId = GetCategoryGroupId(slot, item);
                        if (!categories.TryGetValue(groupId, out var list))
                            categories[groupId] = list = [];

                        list.Add(item);
                    }

                    foreach (var kvp in categories)
                        headers.Add(CreateCategoryHeader(kvp.Key, kvp.Value));
                }
                else
                {
                    headers.Add(
                        CreateHeader(
                            slot.ToString().ToLowerInvariant(),
                            Format.CamelCaseToTitle(slot.ToString()),
                            _cachedVisibleItems
                        )
                    );
                }
            }

            headers.Sort((a, b) => a.Name.CompareTo(b.Name));
            SetHeaders(headers);

            // Rebuild header index for fast expansion.
            _headersById.Clear();
            for (int i = 0; i < headers.Count; i++)
            {
                var h = headers[i];
                if (h?.Id == null)
                    continue;

                _headersById[h.Id] = h;
            }

            // If the current expanded header no longer exists, forget it.
            if (
                !string.IsNullOrEmpty(_expandedHeaderId)
                && !_headersById.ContainsKey(_expandedHeaderId)
            )
                _expandedHeaderId = null;
        }

        private EquipmentListHeaderVM CreateCategoryHeader(string categoryId, List<WItem> items)
        {
            var title = GetCategoryName(categoryId);
            return CreateHeader(categoryId.ToLowerInvariant(), title, items);
        }

        private static string GetCategoryName(string categoryId)
        {
            return GameTexts.FindText("str_item_category", categoryId)?.ToString() ?? categoryId;
        }

        private static string GetCategoryGroupId(EquipmentIndex slot, WItem item)
        {
            var rawId = item?.Category?.StringId ?? string.Empty;

            if (!ArmorSlots.Contains(slot))
                return rawId;

            // Rule: only merge ultra_armor into heavy_armor.
            return rawId switch
            {
                "ultra_armor" => "heavy_armor",
                _ => rawId,
            };
        }

        private EquipmentListHeaderVM CreateHeader(
            string headerId,
            string headerText,
            List<WItem> items
        )
        {
            var header = new EquipmentListHeaderVM(this, headerId, headerText);

            bool isPlayerMode = State.Mode == EditorMode.Player;

            if (items != null && items.Count > 0)
            {
                var normal = new List<WItem>();
                var unlocking = new List<WItem>();

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null)
                        continue;

                    if (isPlayerMode && !item.IsCrafted && !item.IsUnlocked)
                    {
                        unlocking.Add(item);
                        continue;
                    }

                    normal.Add(item);
                }

                unlocking.Sort(
                    (a, b) =>
                    {
                        int pa = a?.UnlockProgress ?? 0;
                        int pb = b?.UnlockProgress ?? 0;
                        int cmp = pb.CompareTo(pa);
                        if (cmp != 0)
                            return cmp;

                        return string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase);
                    }
                );

                for (int i = 0; i < normal.Count; i++)
                    header.AddRow(new EquipmentListRowVM(header, normal[i]));

                for (int i = 0; i < unlocking.Count; i++)
                    header.AddRow(new EquipmentListRowVM(header, unlocking[i]));
            }

            header.UpdateRowCount();
            return header;
        }

        private string GetSelectedHeaderId()
        {
            if (Headers.Count == 0)
                return null;

            var slot = State.Slot;
            var equipped = PreviewController.GetItem(slot);
            if (equipped == null)
                return null;

            if (WeaponSlots.Contains(slot))
                return equipped.Type.ToString().ToLowerInvariant();

            if (slot == EquipmentIndex.Horse)
                return GetCategoryGroupId(slot, equipped).ToLowerInvariant();

            if (_groupNonWeaponsByCategory)
                return GetCategoryGroupId(slot, equipped).ToLowerInvariant();

            return slot.ToString().ToLowerInvariant();
        }

        private void UpdateEquipmentHeaderExpansion()
        {
            if (Headers.Count == 0)
                return;

            var targetId = GetSelectedHeaderId();
            if (string.IsNullOrEmpty(targetId))
            {
                // Nothing selected: collapse the previously expanded header only.
                if (
                    !string.IsNullOrEmpty(_expandedHeaderId)
                    && _headersById.TryGetValue(_expandedHeaderId, out var prev)
                )
                {
                    if (prev.IsExpanded)
                        prev.IsExpanded = false;
                }

                _expandedHeaderId = null;
                return;
            }

            // Already expanded -> do nothing.
            if (
                string.Equals(_expandedHeaderId, targetId, StringComparison.Ordinal)
                && _headersById.TryGetValue(targetId, out var already)
                && already.IsExpanded
            )
                return;

            // Collapse previous expanded header (at most one).
            if (
                !string.IsNullOrEmpty(_expandedHeaderId)
                && !string.Equals(_expandedHeaderId, targetId, StringComparison.Ordinal)
                && _headersById.TryGetValue(_expandedHeaderId, out var previousHeader)
            )
            {
                if (previousHeader.IsExpanded)
                    previousHeader.IsExpanded = false;
            }

            // Expand target header (one).
            if (_headersById.TryGetValue(targetId, out var targetHeader))
            {
                if (!targetHeader.IsExpanded)
                    targetHeader.IsExpanded = true;

                _expandedHeaderId = targetId;
            }
            else
            {
                _expandedHeaderId = null;
            }
        }
    }
}
