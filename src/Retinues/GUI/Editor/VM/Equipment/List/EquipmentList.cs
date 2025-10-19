using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment.List
{
    /// <summary>
    /// ViewModel for the equipment item list for the selected slot and faction.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentListVM : BaseListVM
    {
        private bool _needsRebuild = true; // first show must build

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Caches                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Dictionary<
            string,
            Dictionary<string, List<(WItem item, bool unlocked, int progress)>>
        > _cache = [];

        private string _lastFactionId;
        private string _lastSlotId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap => [];

        /// <summary>
        /// Handle faction changes by marking rebuild required.
        /// </summary>
        protected override void OnFactionChange()
        {
            _needsRebuild = true;
            if (IsVisible)
                Build();
        }

        /// <summary>
        /// Handle slot changes by marking rebuild required.
        /// </summary>
        protected override void OnSlotChange()
        {
            _needsRebuild = true;
            if (IsVisible)
                Build();
        }

        /// <summary>
        /// Handle equipment changes by marking rebuild required.
        /// </summary>
        protected override void OnEquipmentChange()
        {
            _needsRebuild = true;
            if (IsVisible)
                Build();
        }

        /// <summary>
        /// Build or refresh the equipment rows for the current faction and slot.
        /// </summary>
        public void Build()
        {
            if (!_needsRebuild)
                return;
            _needsRebuild = false;

            List<string> weaponSlots =
            [
                EquipmentIndex.Weapon0.ToString(),
                EquipmentIndex.Weapon1.ToString(),
                EquipmentIndex.Weapon2.ToString(),
                EquipmentIndex.Weapon3.ToString(),
            ];

            var factionId = State.Faction?.StringId;
            var slotId = State.Slot.ToString();

            if (_lastFactionId == factionId)
            {
                if (_lastSlotId == slotId)
                    return; // No change
                else if (weaponSlots.Contains(_lastSlotId) && weaponSlots.Contains(slotId))
                    return; // Both are weapon slots
            }

            // Update cache if faction or slot changed
            _lastFactionId = factionId;
            _lastSlotId = slotId;

            // Ensure faction exists in cache
            if (!_cache.ContainsKey(factionId))
                _cache[factionId] = [];

            // Ensure slot exists in cache
            if (!_cache[factionId].ContainsKey(slotId))
                _cache[factionId][slotId] = null;

            // Retrieve cache if any
            var cache = _cache[factionId][slotId];

            // Clear existing rows
            EquipmentRows.Clear();

            // Add "Empty" option
            EquipmentRows.Add(new EquipmentRowVM(null, true, true, 0) { IsVisible = IsVisible });

            // Determine if we need to fill the cache
            bool fillCache = cache == null;

            // Initialize cache if needed
            if (fillCache)
                _cache[factionId][slotId] = [];

            // Populate row list
            foreach (
                var (
                    item,
                    isAvailable,
                    isUnlocked,
                    progress
                ) in EquipmentManager.CollectAvailableItems(State.Faction, State.Slot, cache: cache)
            )
            {
                var row = new EquipmentRowVM(item, isAvailable, isUnlocked, progress)
                {
                    IsVisible = IsVisible, // Ensure visibility matches parent
                };
                EquipmentRows.Add(row);
                if (fillCache)
                    _cache[factionId][slotId].Add((item, isUnlocked, progress));
            }

            RefreshFilter();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<EquipmentRowVM> EquipmentRows { get; set; } = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override List<BaseListElementVM> Rows => [.. EquipmentRows];

        public override void Show()
        {
            base.Show();
            foreach (var r in EquipmentRows)
                r.Show();
            if (_needsRebuild)
                Build();
        }

        public override void Hide()
        {
            foreach (var r in EquipmentRows)
                r.Hide();
            base.Hide();
        }
    }
}
