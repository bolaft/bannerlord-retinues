using System;
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.ViewModelCollection;
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

        private enum SortMode
        {
            Category,
            Name,
            Tier,
            Cost,
        }

        private SortMode _sort = SortMode.Category;
        private bool _descending = false;

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
        /// Handle troop changes by marking rebuild required.
        /// </summary>
        protected override void OnTroopChange()
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
            
            FilterText = string.Empty;
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

            ApplySort();
            RefreshFilter();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<EquipmentRowVM> EquipmentRows { get; set; } = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool SortByNameSelected => _sort == SortMode.Name;

        [DataSourceProperty]
        public bool SortByCategorySelected => _sort == SortMode.Category;

        [DataSourceProperty]
        public bool SortByTierSelected => _sort == SortMode.Tier;

        [DataSourceProperty]
        public bool SortByCostSelected => _sort == SortMode.Cost;

        [DataSourceProperty]
        public CampaignUIHelper.SortState SortByNameState =>
            _sort == SortMode.Name
                ? _descending
                    ? CampaignUIHelper.SortState.Descending
                    : CampaignUIHelper.SortState.Ascending
                : CampaignUIHelper.SortState.Default;

        [DataSourceProperty]
        public CampaignUIHelper.SortState SortByCategoryState =>
            _sort == SortMode.Category
                ? _descending
                    ? CampaignUIHelper.SortState.Descending
                    : CampaignUIHelper.SortState.Ascending
                : CampaignUIHelper.SortState.Default;

        [DataSourceProperty]
        public CampaignUIHelper.SortState SortByTierState =>
            _sort == SortMode.Tier
                ? _descending
                    ? CampaignUIHelper.SortState.Descending
                    : CampaignUIHelper.SortState.Ascending
                : CampaignUIHelper.SortState.Default;

        [DataSourceProperty]
        public CampaignUIHelper.SortState SortByCostState =>
            _sort == SortMode.Cost
                ? _descending
                    ? CampaignUIHelper.SortState.Descending
                    : CampaignUIHelper.SortState.Ascending
                : CampaignUIHelper.SortState.Default;

        [DataSourceProperty]
        public string SortByNameText => L.S("sort_name", "Name");

        [DataSourceProperty]
        public string SortByCategoryText => L.S("sort_category", "Category");

        [DataSourceProperty]
        public string SortByTierText => L.S("sort_tier", "Tier");

        [DataSourceProperty]
        public string SortByCostText => L.S("sort_cost", "Cost");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteSortByName()
        {
            if (_sort == SortMode.Name)
            {
                _descending = !_descending;
            }
            else
            {
                _sort = SortMode.Name;
                _descending = false;
            }
            ApplySort();
        }

        [DataSourceMethod]
        public void ExecuteSortByCategory()
        {
            if (_sort == SortMode.Category)
            {
                _descending = !_descending;
            }
            else
            {
                _sort = SortMode.Category;
                _descending = false;
            }
            ApplySort();
        }

        [DataSourceMethod]
        public void ExecuteSortByTier()
        {
            if (_sort == SortMode.Tier)
            {
                _descending = !_descending;
            }
            else
            {
                _sort = SortMode.Tier;
                _descending = false;
            }
            ApplySort();
        }

        [DataSourceMethod]
        public void ExecuteSortByCost()
        {
            if (_sort == SortMode.Cost)
            {
                _descending = !_descending;
            }
            else
            {
                _sort = SortMode.Cost;
                _descending = false;
            }
            ApplySort();
        }

        private void ApplySort()
        {
            if (EquipmentRows == null || EquipmentRows.Count == 0)
                return;

            bool IsEmpty(EquipmentRowVM r) => r.RowItem == null;
            bool IsEnabled(EquipmentRowVM r) => r.IsUnlocked && r.IsAvailable;

            string NameOf(EquipmentRowVM r) => r.RowItem?.Name ?? "";
            int TierOf(EquipmentRowVM r) => r.RowItem?.Tier ?? -1;
            string CategoryOf(EquipmentRowVM r) => r.RowItem?.Class ?? "";
            int CostOf(EquipmentRowVM r) => EquipmentManager.GetItemCost(r.RowItem, State.Troop);

            int dir = _descending ? -1 : 1;

            int ModeCompare(EquipmentRowVM a, EquipmentRowVM b)
            {
                int Primary(int val) => _descending ? -val : val;

                switch (_sort)
                {
                    case SortMode.Name:
                    {
                        return Primary(
                            string.Compare(NameOf(a), NameOf(b), StringComparison.Ordinal)
                        );
                    }
                    case SortMode.Category:
                    {
                        int r = Primary(
                            string.Compare(CategoryOf(a), CategoryOf(b), StringComparison.Ordinal)
                        );
                        if (r != 0)
                            return r;
                        return string.Compare(NameOf(a), NameOf(b), StringComparison.Ordinal);
                    }
                    case SortMode.Tier:
                    {
                        int r = Primary(TierOf(a).CompareTo(TierOf(b)));
                        if (r != 0)
                            return r;
                        if (
                            (
                                r = string.Compare(
                                    CategoryOf(a),
                                    CategoryOf(b),
                                    StringComparison.Ordinal
                                )
                            ) != 0
                        )
                            return r;
                        return string.Compare(NameOf(a), NameOf(b), StringComparison.Ordinal);
                    }
                    case SortMode.Cost:
                    {
                        int r = Primary(CostOf(a).CompareTo(CostOf(b)));
                        if (r != 0)
                            return r;
                        return string.Compare(NameOf(a), NameOf(b), StringComparison.Ordinal);
                    }
                }
                return 0;
            }

            var comparer = Comparer<EquipmentRowVM>.Create(
                (a, b) =>
                {
                    // 1) Empty row must be first - never affected by direction.
                    bool ae = IsEmpty(a),
                        be = IsEmpty(b);
                    if (ae || be)
                        return ae == be ? 0 : (ae ? -1 : 1);

                    // 2) Enabled above disabled - also not affected by direction.
                    int ar = IsEnabled(a) ? 0 : 1;
                    int br = IsEnabled(b) ? 0 : 1;
                    if (ar != br)
                        return ar - br;

                    // 3) Mode comparison with direction + strong tie-breakers.
                    return ModeCompare(a, b);
                }
            );

            EquipmentRows.Sort(comparer);

            // Keep your visibility propagation.
            EquipmentRows.ApplyActionOnAllItems(r => r.IsVisible = IsVisible);

            OnPropertyChanged(nameof(SortByNameSelected));
            OnPropertyChanged(nameof(SortByNameState));
            OnPropertyChanged(nameof(SortByCategorySelected));
            OnPropertyChanged(nameof(SortByCategoryState));
            OnPropertyChanged(nameof(SortByTierSelected));
            OnPropertyChanged(nameof(SortByTierState));
            OnPropertyChanged(nameof(SortByCostSelected));
            OnPropertyChanged(nameof(SortByCostState));

            RefreshFilter();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override List<BaseListElementVM> Rows => [.. EquipmentRows];

        public override void Show()
        {
            base.Show();
            foreach (var r in EquipmentRows)
                r.Show();

            Log.Info($"[EquipmentList] Show called. Needs rebuild: " + _needsRebuild);
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
