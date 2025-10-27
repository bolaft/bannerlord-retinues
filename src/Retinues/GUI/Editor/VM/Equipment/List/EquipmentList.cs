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

        private const int MaxRowsAbsolute = 1000;

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

            // weapon slot “same family” optimization, keep your existing logic
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
                    return;
                else if (weaponSlots.Contains(_lastSlotId) && weaponSlots.Contains(slotId))
                    return;
            }

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
                cache: cache
            );

            // Keep cache fully updated
            if (fillCache)
            {
                _cache[factionId][slotId].Clear();
                foreach (var (it, _, unlocked, progress) in raw)
                    _cache[factionId][slotId].Add((it, unlocked, progress));
            }

            // 2) Deduplicate and precompute keys (critical for speed & stability)
            var seen = new HashSet<string>(StringComparer.Ordinal);
            _fullTuples = new List<ItemTuple>(raw.Count);

            foreach (var (item, isAvailable, isUnlocked, progress) in raw)
            {
                // Defensive: skip nulls
                if (item == null)
                    continue;

                // Dedupe by StringId (or fallback to reference hash if needed)
                var id = item.StringId ?? item.GetHashCode().ToString();
                if (!seen.Add(id))
                    continue;

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

            Log.Info(
                $"[Equipment List] Snapshot: {_fullTuples.Count} unique items for {slotId} (pre-keyed)."
            );
        }

        private void RebuildVisibleFromSnapshot()
        {
            // Guard
            _fullTuples ??= [];

            // 1) Filter over the full precomputed set
            bool hasFilter = !string.IsNullOrWhiteSpace(FilterText);
            bool Pass(ItemTuple t)
            {
                if (!hasFilter)
                    return true;
                // Simple example: search in name or category; adapt if you have richer filters
                return (
                        t.Name?.IndexOf(FilterText, StringComparison.CurrentCultureIgnoreCase) ?? -1
                    ) >= 0
                    || (
                        t.Category?.IndexOf(FilterText, StringComparison.CurrentCultureIgnoreCase)
                        ?? -1
                    ) >= 0;
            }

            // 2) Sort comparator over tuples (no UI rows involved)
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

            // 3) Filter → Sort → Cap
            var display = new List<ItemTuple>(Math.Min(_fullTuples.Count, MaxRowsAbsolute));
            foreach (var t in _fullTuples)
                if (Pass(t))
                    display.Add(t);

            display.Sort(Comparer<ItemTuple>.Create(Compare));
            if (display.Count > MaxRowsAbsolute)
                display.RemoveRange(MaxRowsAbsolute, display.Count - MaxRowsAbsolute);

            // 4) Publish to UI (only these capped items become VMs)
            EquipmentRows.Clear();
            EquipmentRows.Add(new EquipmentRowVM(null, true, true, 0) { IsVisible = IsVisible }); // Empty row

            foreach (var t in display)
            {
                EquipmentRows.Add(
                    new EquipmentRowVM(t.Item, t.IsAvailable, t.IsUnlocked, t.Progress)
                    {
                        IsVisible = IsVisible,
                    }
                );
            }

            // We rebuilt in sorted order already; no need to resort EquipmentRows.
            // Keep RefreshFilter only if it updates UI state (counts/icons). Otherwise omit.
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
