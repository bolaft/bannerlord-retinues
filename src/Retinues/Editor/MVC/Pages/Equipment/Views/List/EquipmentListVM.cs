using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Views;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Equipment.Views.List
{
    /// <summary>
    /// Equipment list ViewModel.
    /// </summary>
    public sealed partial class EquipmentListVM : BaseListVM
    {
        // Header lookup for fast expansion (id -> header).
        private readonly Dictionary<string, ListHeaderVM> _headersById = new(
            StringComparer.Ordinal
        );

        // Track current expanded header id to avoid collapsing/expanding all headers each time.
        private string _expandedHeaderId;

        protected override EditorPage Page => EditorPage.Equipment;

        private EquipmentIndex _previousSlot = State.Slot;

        private readonly EquipmentIndex[] WeaponSlots =
        [
            EquipmentIndex.Weapon0,
            EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2,
            EquipmentIndex.Weapon3,
        ];

        private static readonly HashSet<EquipmentIndex> ArmorSlots =
        [
            EquipmentIndex.Head,
            EquipmentIndex.Cape,
            EquipmentIndex.Body,
            EquipmentIndex.Gloves,
            EquipmentIndex.Leg,
        ];

        // Cached visible items for current slot + mode toggles (crafted/player).
        private EquipmentIndex _cachedSlot;
        private bool _cachedIncludeCrafted;
        private bool _cachedIsPlayerMode;
        private List<WItem> _cachedVisibleItems;

        // For non-weapon/non-horse slots: if true, we split into ItemCategory headers.
        private bool _groupNonWeaponsByCategory;

        // If list is "large", we can choose a filter strategy.
        private const int FastFilter_RebuildThresholdRows = 500;

        protected override void AfterBuildOnActivate()
        {
            ApplyFilter();
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

            if (WeaponSlots.Contains(previousSlot) && WeaponSlots.Contains(currentSlot))
            {
                UpdateEquipmentHeaderExpansion();
                _previousSlot = currentSlot;
                return;
            }

            _cachedVisibleItems = null;
            Build();
            ApplyFilter();
            UpdateEquipmentHeaderExpansion();
            _previousSlot = currentSlot;
        }

        [EventListener(UIEvent.Crafted, UIEvent.Preview, UIEvent.Doctrine)]
        private void OnModeChange()
        {
            if (State.Page != Page)
                return;

            AutoScrollRowsEnabled = true;
            AutoScrollVersion++;

            _cachedVisibleItems = null;
            Build();
            ApplyFilter();
            UpdateEquipmentHeaderExpansion();
        }

        /// <summary>
        /// Builds the equipment list.
        /// </summary>
        public override void Build()
        {
            BuildSortButtons();
            BuildSections();
            RecomputeHeaderStates();
        }

        private void EnsureItemCache()
        {
            var slot = State.Slot;
            var includeCrafted = State.ShowCrafted;
            var isPlayerMode = State.Mode == EditorMode.Player;

            if (
                _cachedVisibleItems != null
                && slot == _cachedSlot
                && includeCrafted == _cachedIncludeCrafted
                && isPlayerMode == _cachedIsPlayerMode
            )
                return;

            _cachedSlot = slot;
            _cachedIncludeCrafted = includeCrafted;
            _cachedIsPlayerMode = isPlayerMode;

            var raw = WItem.GetEquipmentsForSlot(slot) ?? [];
            var list = new List<WItem>(raw.Count);

            for (int i = 0; i < raw.Count; i++)
            {
                var item = raw[i];
                if (item == null)
                    continue;

                if (!includeCrafted && item.IsCrafted)
                    continue;

                if (isPlayerMode && !item.IsCrafted && !item.IsUnlocked)
                {
                    if (item.UnlockProgress <= 0)
                        continue;
                }

                list.Add(item);
            }

            _cachedVisibleItems = list;
        }
    }
}
