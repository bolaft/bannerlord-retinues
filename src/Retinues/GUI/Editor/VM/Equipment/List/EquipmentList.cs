using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
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
        /* ━━━━━━ Rebuilding ━━━━━━ */

        private bool _needsRebuild = true; // first show must build

        /* ━━━━━━━━ Sorting ━━━━━━━ */

        private enum SortMode
        {
            Category,
            Name,
            Tier,
            Cost,
        }

        private SortMode _sort = SortMode.Category;
        private bool _descending = false;

        /* ━━━━━━━━━ Caps ━━━━━━━━━ */

        private const int MaxRows = 1000;

        // Precomputed snapshot for current faction/slot (deduped + keyed for fast sort/filter)
        private List<ItemTuple> _fullTuples;

        // A compact record with precomputed sort keys (avoid recomputing on every compare)
        private sealed class ItemTuple
        {
            public WItem Item;
            public bool IsAvailable;
            public bool IsUnlocked;
            public int Progress;

            // precomputed keys
            public string Name;
            public string Category;
            public int Tier;
            public int Cost;
            public int EnabledRank; // 0 if unlocked+available else 1
        }

        // Crafted
        private bool _showCrafted = false;

        public bool ShowCrafted
        {
            get =>
                DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>()
                && _showCrafted
                && WeaponSlots.Contains(State.Slot.ToString());
            set
            {
                if (_showCrafted != value)
                {
                    _showCrafted = value;
                    _needsRebuild = true;
                    if (IsVisible)
                        Build();
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Caches                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Dictionary<
            string,
            Dictionary<string, List<(WItem item, bool unlocked, int progress)>>
        > _cache = [];

        private string _lastFactionId;
        private string _lastSlotId;
        private readonly Dictionary<string, EquipmentRowVM> _rowsByItemId = new(
            StringComparer.Ordinal
        );
        private readonly HashSet<string> _equipChangeIds = new(StringComparer.Ordinal);
        private EquipmentRowVM _emptyRow;

        public static readonly List<string> WeaponSlots =
        [
            EquipmentIndex.Weapon0.ToString(),
            EquipmentIndex.Weapon1.ToString(),
            EquipmentIndex.Weapon2.ToString(),
            EquipmentIndex.Weapon3.ToString(),
        ];

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
        /// Handle equip changes by refreshing affected rows.
        /// </summary>
        protected override void OnEquipChange()
        {
            if (!IsVisible)
                return;

            var deltaNullable = State.LastEquipChange;
            if (deltaNullable == null)
                return;

            _equipChangeIds.Clear();

            void Handle(string id)
            {
                if (string.IsNullOrEmpty(id))
                {
                    _emptyRow?.OnEquipChanged();
                    return;
                }

                if (!_equipChangeIds.Add(id))
                    return;

                if (_rowsByItemId.TryGetValue(id, out var row) && row != null)
                    row.OnEquipChanged();
            }

            var delta = deltaNullable.Value;
            Handle(delta.OldEquippedId);
            Handle(delta.NewEquippedId);
            Handle(delta.OldStagedId);
            Handle(delta.NewStagedId);

            State.LastEquipChange = null;
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
            {
                var factionId = State.Faction?.StringId;

                if (_lastFactionId == factionId)
                {
                    var slotId = State.Slot.ToString();

                    if (_lastSlotId == slotId)
                    {
                        _needsRebuild = false;
                        return;
                    }
                    else if (WeaponSlots.Contains(slotId) && WeaponSlots.Contains(_lastSlotId))
                    {
                        _needsRebuild = false;
                        _lastSlotId = slotId;

                        foreach (var r in EquipmentRows)
                            r.OnSlotChanged();

                        return;
                    }
                }

                FilterText = string.Empty;

                Build();
            }
        }

        /// <summary>
        /// Handle equipment changes by marking rebuild required.
        /// </summary>
        protected override void OnEquipmentChange()
        {
            if (IsVisible)
                foreach (var r in EquipmentRows)
                    r.OnEquipmentChanged();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      List Building                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Build or refresh the equipment rows for the current faction and slot.
        /// </summary>
        public void Build()
        {
            if (!_needsRebuild)
                return;
            _needsRebuild = false;

            var factionId = State.Faction?.StringId;
            var slotId = State.Slot.ToString();

            _lastFactionId = factionId;
            _lastSlotId = slotId;

            // Ensure caches
            if (!_cache.ContainsKey(factionId))
                _cache[factionId] = [];
            if (!_cache[factionId].ContainsKey(slotId))
                _cache[factionId][slotId] = null;

            var cache = _cache[factionId][slotId];
            bool fillCache = cache == null;
            if (fillCache)
                _cache[factionId][slotId] = [];

            // 1) Collect ALL items once
            var raw = EquipmentManager.CollectAvailableItems(
                State.Faction,
                State.Slot,
                cache: cache,
                crafted: ShowCrafted
            );

            // Keep cache fully updated
            if (fillCache)
            {
                _cache[factionId][slotId].Clear();
                foreach (var (it, _, unlocked, progress) in raw)
                    _cache[factionId][slotId].Add((it, unlocked, progress));
            }

            // 2) Deduplicate and precompute keys (critical for speed & stability)
            _rowsByItemId.Clear();
            _fullTuples = new List<ItemTuple>(raw.Count);

            foreach (var (item, isAvailable, isUnlocked, progress) in raw)
            {
                // Defensive: skip nulls
                if (item == null)
                    continue;

                // Dedupe by StringId (or fallback to reference hash if needed)
                var id = item.StringId ?? item.GetHashCode().ToString();
                if (_rowsByItemId.ContainsKey(id))
                    continue;

                _rowsByItemId[id] = null;

                _fullTuples.Add(
                    new ItemTuple
                    {
                        Item = item,
                        IsAvailable = isAvailable,
                        IsUnlocked = isUnlocked,
                        Progress = progress,

                        Name = item.Name ?? string.Empty,
                        Category = item.Class ?? string.Empty,
                        Tier = item.Tier,
                        Cost = EquipmentManager.GetItemCost(item, State.Troop),
                        EnabledRank = (isUnlocked && isAvailable) ? 0 : 1,
                    }
                );
            }

            // 3) Build the visible list
            RebuildVisibleFromSnapshot();
        }

        private void RebuildVisibleFromSnapshot()
        {
            // Guard
            _fullTuples ??= [];

            // 1) Sort comparator over tuples (no UI rows involved)
            int Primary(int v) => _descending ? -v : v;

            int Compare(ItemTuple a, ItemTuple b)
            {
                // Enabled above disabled (not direction-sensitive)
                if (a.EnabledRank != b.EnabledRank)
                    return a.EnabledRank - b.EnabledRank;

                switch (_sort)
                {
                    case SortMode.Name:
                        return Primary(string.Compare(a.Name, b.Name, StringComparison.Ordinal));

                    case SortMode.Category:
                    {
                        int r = Primary(
                            string.Compare(a.Category, b.Category, StringComparison.Ordinal)
                        );
                        if (r != 0)
                            return r;
                        return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    }

                    case SortMode.Tier:
                    {
                        int r = Primary(a.Tier.CompareTo(b.Tier));
                        if (r != 0)
                            return r;
                        r = string.Compare(a.Category, b.Category, StringComparison.Ordinal);
                        if (r != 0)
                            return r;
                        return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    }

                    case SortMode.Cost:
                    {
                        int r = Primary(a.Cost.CompareTo(b.Cost));
                        if (r != 0)
                            return r;
                        return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    }
                }
                return 0;
            }

            // Display item list
            var display = new List<ItemTuple>(Math.Min(_fullTuples.Count, MaxRows));
            foreach (var t in _fullTuples)
                display.Add(t);

            // Sort
            display.Sort(Comparer<ItemTuple>.Create(Compare));

            // Capping
            if (display.Count > MaxRows)
            {
                // Remove excess items
                display.RemoveRange(MaxRows, display.Count - MaxRows);

                // List of display item IDs
                var displayIds = display.Select(t => t.Item.StringId).ToList();

                // Helper function
                void EnsureItemIsIncluded(WItem item)
                {
                    if (item != null && !displayIds.Contains(item.StringId))
                    {
                        var itemTuple = _fullTuples.FirstOrDefault(t => t.Item.Equals(item));
                        if (itemTuple != null)
                            display.Add(itemTuple);
                    }
                }

                // Ensure staged and equipped items are included
                foreach (var slot in WEquipment.Slots)
                {
                    // Ensure equipped item is included
                    var equippedItem = State.Equipment?.Get(slot);
                    EnsureItemIsIncluded(equippedItem);

                    // Ensure staged item is included
                    var stagedItem =
                        State.EquipData?.TryGetValue(slot, out var equipData) == true
                            ? equipData.Equip != null
                                ? new WItem(equipData.Equip.ItemId)
                                : null
                            : null;
                    EnsureItemIsIncluded(stagedItem);
                }
            }

            // 4) Publish to UI (only these capped items become VMs)
            EquipmentRows.Clear();
            _emptyRow = new EquipmentRowVM(null, true, true, 0) { IsVisible = IsVisible }; // Empty row
            EquipmentRows.Add(_emptyRow);

            foreach (var t in display)
            {
                var row = new EquipmentRowVM(t.Item, t.IsAvailable, t.IsUnlocked, t.Progress)
                {
                    IsVisible = IsVisible,
                };
                EquipmentRows.Add(row);

                var id = t.Item?.StringId ?? t.Item?.GetHashCode().ToString();
                if (!string.IsNullOrEmpty(id))
                    _rowsByItemId[id] = row;
            }

            // Rebuilt in sorted order already.
            RefreshFilter();

            // Update sort state bindings for the header UI
            OnPropertyChanged(nameof(SortByNameSelected));
            OnPropertyChanged(nameof(SortByNameState));
            OnPropertyChanged(nameof(SortByCategorySelected));
            OnPropertyChanged(nameof(SortByCategoryState));
            OnPropertyChanged(nameof(SortByTierSelected));
            OnPropertyChanged(nameof(SortByTierState));
            OnPropertyChanged(nameof(SortByCostSelected));
            OnPropertyChanged(nameof(SortByCostState));
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
                _descending = !_descending;
            else
            {
                _sort = SortMode.Name;
                _descending = false;
            }
            RebuildVisibleFromSnapshot();
        }

        [DataSourceMethod]
        public void ExecuteSortByCategory()
        {
            if (_sort == SortMode.Category)
                _descending = !_descending;
            else
            {
                _sort = SortMode.Category;
                _descending = false;
            }
            RebuildVisibleFromSnapshot();
        }

        [DataSourceMethod]
        public void ExecuteSortByTier()
        {
            if (_sort == SortMode.Tier)
                _descending = !_descending;
            else
            {
                _sort = SortMode.Tier;
                _descending = false;
            }
            RebuildVisibleFromSnapshot();
        }

        [DataSourceMethod]
        public void ExecuteSortByCost()
        {
            if (_sort == SortMode.Cost)
                _descending = !_descending;
            else
            {
                _sort = SortMode.Cost;
                _descending = false;
            }
            RebuildVisibleFromSnapshot();
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
