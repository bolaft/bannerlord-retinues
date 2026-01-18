using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Equipment.Views.List
{
    /// <summary>
    /// Equipment list ViewModel.
    /// </summary>
    public sealed class EquipmentListVM : BaseListVM
    {
        protected override EditorPage Page => EditorPage.Equipment;

        /// <summary>
        /// Gets the tooltip for the filter input.
        /// </summary>
        protected override Tooltip GetFilterTooltip()
        {
            return new(
                L.S(
                    "filter_tooltip_description_equipment",
                    "Type to filter the list by name, category or tier."
                )
            );
        }

        private EquipmentIndex _previousSlot = State.Slot;

        private readonly EquipmentIndex[] WeaponSlots =
        [
            EquipmentIndex.Weapon0,
            EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2,
            EquipmentIndex.Weapon3,
        ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Lifecycle                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void AfterBuildOnActivate()
        {
            UpdateEquipmentHeaderExpansion();
        }

        [EventListener(UIEvent.Slot)]
        private void OnSlotChange()
        {
            if (State.Page != Page)
                return;

            var previousSlot = _previousSlot;
            var currentSlot = State.Slot;

            AutoScrollRowsEnabled = true;
            AutoScrollVersion++;

            // When switching weapon slots, we do not rebuild, but we still want auto-scroll.
            if (WeaponSlots.Contains(previousSlot) && WeaponSlots.Contains(currentSlot))
            {
                UpdateEquipmentHeaderExpansion();
                _previousSlot = currentSlot;
                return;
            }

            Build();
            UpdateEquipmentHeaderExpansion();
            _previousSlot = currentSlot;
        }

        [EventListener(UIEvent.Crafted, UIEvent.Preview)]
        private void OnModeChange()
        {
            if (State.Page != Page)
                return;

            AutoScrollRowsEnabled = true;
            AutoScrollVersion++;

            Build();
            UpdateEquipmentHeaderExpansion();
        }

        public override void Build()
        {
            BuildSortButtons();
            BuildSections();
            RecomputeHeaderStates();
        }

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
            var headers = new List<ListHeaderVM>();
            var slot = State.Slot;

            if (
                slot
                is EquipmentIndex.Weapon0
                    or EquipmentIndex.Weapon1
                    or EquipmentIndex.Weapon2
                    or EquipmentIndex.Weapon3
            )
            {
                // Weapons are grouped by type.
                var types = new Dictionary<ItemObject.ItemTypeEnum, List<WItem>>();

                foreach (var item in WItem.GetEquipmentsForSlot(slot))
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
                // Horses are grouped by category.
                var categories = new Dictionary<ItemCategory, List<WItem>>();

                foreach (var item in WItem.GetEquipmentsForSlot(slot))
                {
                    var category = item.Category;
                    if (!categories.ContainsKey(category))
                        categories[category] = [];

                    categories[category].Add(item);
                }

                foreach (var kvp in categories)
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
            else
            {
                // Armor pieces are not grouped.
                headers.Add(
                    CreateHeader(
                        slot.ToString().ToLowerInvariant(),
                        Format.CamelCaseToTitle(slot.ToString()),
                        WItem.GetEquipmentsForSlot(slot)
                    )
                );
            }

            // Sort headers alphabetically.
            headers.Sort((a, b) => a.Name.CompareTo(b.Name));

            // Single binding update instead of N inserts.
            SetHeaders(headers);
        }

        private EquipmentListHeaderVM CreateHeader(
            string headerId,
            string headerText,
            List<WItem> items
        )
        {
            var header = new EquipmentListHeaderVM(this, headerId, headerText);

            bool isPlayerMode = State.Mode == EditorMode.Player;
            bool includeCrafted = State.ShowCrafted;

            if (items != null && items.Count > 0)
            {
                var normal = new List<WItem>();
                var unlocking = new List<WItem>();

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null)
                        continue;

                    // Skip crafted items if the filter is off.
                    if (!includeCrafted && item.IsCrafted)
                        continue;

                    // Player mode:
                    // - Fully locked (0%) items stay hidden.
                    // - Partially unlocked (1-99%) items are included, but should appear at the end.
                    if (isPlayerMode && !item.IsCrafted && !item.IsUnlocked)
                    {
                        if (item.UnlockProgress <= 0)
                            continue;

                        unlocking.Add(item);
                        continue;
                    }

                    normal.Add(item);
                }

                // Default view (no sort active): keep current order for normal items,
                // and append "unlocking" items at end by progress desc (higher first).
                unlocking.Sort(
                    (a, b) =>
                    {
                        int pa = a?.UnlockProgress ?? 0;
                        int pb = b?.UnlockProgress ?? 0;
                        int cmp = pb.CompareTo(pa); // desc
                        if (cmp != 0)
                            return cmp;

                        return string.Compare(
                            a?.Name,
                            b?.Name,
                            System.StringComparison.OrdinalIgnoreCase
                        );
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

        private void UpdateEquipmentHeaderExpansion()
        {
            if (Headers.Count == 0)
                return;

            ListHeaderVM selectedHeader = null;

            for (int i = 0; i < Headers.Count; i++)
            {
                var h = Headers[i];
                if (h != null && h.ContainsSelectedRow())
                {
                    selectedHeader = h;
                    break;
                }
            }

            for (int i = 0; i < Headers.Count; i++)
            {
                var h = Headers[i];
                if (h == null)
                    continue;

                // Keep exactly one open (the selected one).
                if (ReferenceEquals(h, selectedHeader))
                    h.IsExpanded = true;
                else if (h.IsVisible)
                    h.IsExpanded = false;
            }
        }
    }
}
