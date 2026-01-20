using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.MVC.Shared.Views;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Equipment.Views.List
{
    public sealed partial class EquipmentListVM
    {
        protected override void ApplyFilter()
        {
            // With the base batching fix, the default path is now cheap.
            // Keep the "rebuild for huge lists" option for really large slots.
            var sortActive = SortButtons.Any(b => b.IsSortedAscending || b.IsSortedDescending);
            if (sortActive)
            {
                ApplyFilter_Default();
                return;
            }

            var totalRows = 0;
            for (int i = 0; i < Headers.Count; i++)
                totalRows += Headers[i]?.Rows?.Count ?? 0;

            var filter = (FilterText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(filter) || totalRows < FastFilter_RebuildThresholdRows)
            {
                ApplyFilter_Default();
                return;
            }

            EnsureItemCache();

            var matched = FilterItems(_cachedVisibleItems, State.Slot, filter);

            var expansion = CaptureExpansion();
            var headers = BuildHeadersFromItems(matched);

            SetHeaders(headers);
            RestoreExpansion(expansion);

            RecomputeHeaderStates();
            UpdateEquipmentHeaderExpansion();
        }

        private List<ListHeaderVM> BuildHeadersFromItems(List<WItem> items)
        {
            var headers = new List<ListHeaderVM>();
            var slot = State.Slot;

            if (WeaponSlots.Contains(slot))
            {
                var types = new Dictionary<ItemObject.ItemTypeEnum, List<WItem>>();
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    var type = it.Type;
                    if (!types.TryGetValue(type, out var list))
                        types[type] = list = [];

                    list.Add(it);
                }

                foreach (var kvp in types)
                {
                    headers.Add(
                        CreateHeader(
                            kvp.Key.ToString().ToLowerInvariant(),
                            kvp.Key.ToString(),
                            kvp.Value
                        )
                    );
                }
            }
            else if (slot == EquipmentIndex.Horse || _groupNonWeaponsByCategory)
            {
                var categories = new Dictionary<string, List<WItem>>(StringComparer.Ordinal);
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    var groupId = GetCategoryGroupId(slot, it);
                    if (!categories.TryGetValue(groupId, out var list))
                        categories[groupId] = list = [];

                    list.Add(it);
                }

                foreach (var kvp in categories)
                    headers.Add(CreateCategoryHeader(kvp.Key, kvp.Value));
            }
            else
            {
                headers.Add(
                    CreateHeader(slot.ToString().ToLowerInvariant(), slot.ToString(), items)
                );
            }

            headers.Sort((a, b) => a.Name.CompareTo(b.Name));
            return headers;
        }

        private static List<WItem> FilterItems(
            List<WItem> items,
            EquipmentIndex slot,
            string filter
        )
        {
            if (items == null || items.Count == 0)
                return [];

            var f = filter.Trim();
            var cmp = StringComparison.OrdinalIgnoreCase;

            var result = new List<WItem>();

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null)
                    continue;

                var name = it.Name;
                if (!string.IsNullOrEmpty(name) && name.IndexOf(f, cmp) >= 0)
                {
                    result.Add(it);
                    continue;
                }

                var catId = GetCategoryGroupId(slot, it);
                if (!string.IsNullOrEmpty(catId))
                {
                    if (catId.IndexOf(f, cmp) >= 0)
                    {
                        result.Add(it);
                        continue;
                    }

                    var catName = GameTexts.FindText("str_item_category", catId)?.ToString();
                    if (!string.IsNullOrEmpty(catName) && catName.IndexOf(f, cmp) >= 0)
                    {
                        result.Add(it);
                        continue;
                    }
                }

                var typeText = it.Type.ToString();
                if (!string.IsNullOrEmpty(typeText) && typeText.IndexOf(f, cmp) >= 0)
                {
                    result.Add(it);
                    continue;
                }

                var tierText = it.Tier.ToString();
                if (!string.IsNullOrEmpty(tierText) && tierText.IndexOf(f, cmp) >= 0)
                {
                    result.Add(it);
                    continue;
                }
            }

            return result;
        }
    }
}
