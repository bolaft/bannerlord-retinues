using System;
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game.Wrappers;
using Retinues.Managers;
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
            Value,
        }

        private SortMode _sort = SortMode.Category;
        private bool _descending = false;

        /* ━━━━━━━━━ Caps / Paging ━━━━━━━━━ */

        /// <summary>
        /// Number of rows per page (excluding the empty row).
        /// </summary>
        public const int MaxRows = 100; // page size

        // Total number of items after applying the current filter.
        private int _filteredCount;

        /// <summary>
        /// Zero-based index of the current page.
        /// </summary>
        private int _currentPageIndex;

        /// <summary>
        /// Total number of pages for the current filtered result.
        /// </summary>
        private int _totalPages = 1;

        // Precomputed snapshot for current faction/slot (deduped + keyed for fast sort/filter)
        private readonly Dictionary<EquipmentIndex, List<ItemTuple>> _fullTuples = [];

        // Context keys for snapshots so we know when they are still valid
        private readonly Dictionary<EquipmentIndex, string> _fullTupleKeys = [];

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
            public int Value;
            public int Cost;
            public int EnabledRank; // 0 if unlocked+available else 1
        }

        /* ━━━━━━━━ Crafted ━━━━━━━ */

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
                    _currentPageIndex = 0;

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

        // Use a common snapshot key for all weapon slots so they share the same
        // precomputed list; only the equipped/compare state is per-slot.
        private static EquipmentIndex GetSnapshotKey(EquipmentIndex slot)
        {
            if (
                slot == EquipmentIndex.Weapon0
                || slot == EquipmentIndex.Weapon1
                || slot == EquipmentIndex.Weapon2
                || slot == EquipmentIndex.Weapon3
            )
            {
                return EquipmentIndex.Weapon0;
            }

            return slot;
        }

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
            _currentPageIndex = 0;

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
                    _emptyRow?.OnEquipChangedSelective();
                    return;
                }

                if (!_equipChangeIds.Add(id))
                    return;

                if (_rowsByItemId.TryGetValue(id, out var row) && row != null)
                    row.OnEquipChangedSelective();
            }

            var delta = deltaNullable.Value;
            Handle(delta.OldEquippedId);
            Handle(delta.NewEquippedId);
            Handle(delta.OldStagedId);
            Handle(delta.NewStagedId);

            foreach (var row in EquipmentRows)
                row.OnEquipChanged();

            State.LastEquipChange = null;
        }

        /// <summary>
        /// Handle troop changes by marking rebuild required.
        /// </summary>
        protected override void OnTroopChange()
        {
            _needsRebuild = true;
            _currentPageIndex = 0;

            if (IsVisible)
                Build();
        }

        /// <summary>
        /// Handle slot changes by marking rebuild required.
        /// </summary>
        protected override void OnSlotChange()
        {
            _needsRebuild = true;
            _currentPageIndex = 0;

            if (!IsVisible)
                return;

            var factionId = State.Faction?.StringId;

            if (_lastFactionId == factionId)
            {
                var slotId = State.Slot.ToString();

                if (_lastSlotId == slotId)
                {
                    _needsRebuild = false;
                    return;
                }

                // Weapon → weapon: same faction, just changed which weapon slot is "current"
                if (WeaponSlots.Contains(slotId) && WeaponSlots.Contains(_lastSlotId))
                {
                    _needsRebuild = false;

                    var oldSlotId = _lastSlotId;
                    _lastSlotId = slotId;

                    // 2) Only the row that WAS equipped and the row that IS equipped
                    //    need ShowIsEquipped / IsSelected updates.
                    if (
                        TryParseSlot(oldSlotId, out var oldSlot)
                        && TryParseSlot(slotId, out var newSlot)
                    )
                    {
                        NotifyEquippedRowChanged(oldSlot, newSlot);
                    }

                    // 3) Still notify comparison chevrons for all rows that might be affected
                    foreach (var r in EquipmentRows)
                        r.OnSlotChanged();

                    return;
                }
            }

            // Faction changed or non-weapon slot change → full rebuild
            FilterText = string.Empty;
            Build();
        }

        /// <summary>
        /// Try parse equipment slot from string.
        /// </summary>
        private static bool TryParseSlot(string slotId, out EquipmentIndex slot)
        {
            return Enum.TryParse(slotId, out slot);
        }

        /// <summary>
        /// Get the currently equipped or staged item for the given slot.
        /// </summary>
        private WItem GetCurrentItemForSlot(EquipmentIndex slot)
        {
            // Staged first
            if (
                State.EquipData != null
                && State.EquipData.TryGetValue(slot, out var equipData)
                && equipData.Equip != null
            )
            {
                return new WItem(equipData.Equip.ItemId);
            }

            // Then actual equipment
            return State.Equipment?.Get(slot);
        }

        private void NotifyEquippedRowChanged(EquipmentIndex oldSlot, EquipmentIndex newSlot)
        {
            var oldItem = GetCurrentItemForSlot(oldSlot);
            var newItem = GetCurrentItemForSlot(newSlot);

            EquipmentRowVM oldRow = null;

            if (
                oldItem != null
                && !string.IsNullOrEmpty(oldItem.StringId)
                && _rowsByItemId.TryGetValue(oldItem.StringId, out oldRow)
                && oldRow != null
            )
            {
                // Old equipped row loses its "equipped" indicator
                oldRow.OnSlotChangedSelective();
            }

            if (
                newItem != null
                && !string.IsNullOrEmpty(newItem.StringId)
                && _rowsByItemId.TryGetValue(newItem.StringId, out var newRow)
                && newRow != null
                && newRow != oldRow
            )
            {
                // New equipped row gains its "equipped" indicator
                newRow.OnSlotChangedSelective();
            }
        }

        private void NotifySlotSelectionChanged(EquipmentIndex oldSlot, EquipmentIndex newSlot)
        {
            var oldItem = GetCurrentItemForSlot(oldSlot);
            var newItem = GetCurrentItemForSlot(newSlot);

            if (oldItem != null)
            {
                var oldId = oldItem.StringId;
                if (
                    !string.IsNullOrEmpty(oldId)
                    && _rowsByItemId.TryGetValue(oldId, out var oldRow)
                    && oldRow != null
                )
                {
                    oldRow.OnSlotChangedSelective();
                }
            }

            if (newItem != null)
            {
                var newId = newItem.StringId;
                if (
                    !string.IsNullOrEmpty(newId)
                    && _rowsByItemId.TryGetValue(newId, out var newRow)
                    && newRow != null
                )
                {
                    newRow.OnSlotChangedSelective();
                }
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

            var factionId = State.Faction?.StringId;
            var slotId = State.Slot.ToString();
            var currentSlot = State.Slot;

            // Context key: faction + slot + crafted flag
            var contextKey = $"{factionId ?? string.Empty}|{slotId}|{(ShowCrafted ? 1 : 0)}";

            // If we already have a snapshot for this exact context, just reuse it
            if (
                _fullTupleKeys.TryGetValue(currentSlot, out var existingKey)
                && existingKey == contextKey
                && _fullTuples.TryGetValue(currentSlot, out var existingSnapshot)
                && existingSnapshot != null
            )
            {
                Log.Info($"Reusing cached equipment list for slot {State.Slot}");

                _needsRebuild = false;

                _lastFactionId = factionId;
                _lastSlotId = slotId;

                // Rebuild visible rows from cached tuples
                RebuildVisibleFromSnapshot();

                // Notify rows so comparisons and equipped flags update
                foreach (var r in EquipmentRows)
                    r.OnSlotChanged();

                return;
            }

            Log.Info($"Rebuilding equipment list for slot {State.Slot}");

            _needsRebuild = false;

            _lastFactionId = factionId;
            _lastSlotId = slotId;

            // Ensure caches
            if (!_cache.ContainsKey(factionId))
                _cache[factionId] = [];
            if (!_cache[factionId].ContainsKey(slotId))
                _cache[factionId][slotId] = null;

            var cache = _cache[factionId][slotId];
            bool fillCache = cache == null;

            // Do not seed the shared cache from a crafted-only build.
            if (fillCache && ShowCrafted)
                fillCache = false;

            if (fillCache)
                _cache[factionId][slotId] = [];

            // 1) Collect ALL items once
            var raw = EquipmentManager.CollectAvailableItems(
                State.Faction,
                State.Slot,
                cache: cache,
                craftedOnly: ShowCrafted
            );

            // Keep cache fully updated
            if (fillCache)
            {
                _cache[factionId][slotId].Clear();
                foreach (var (it, _, unlocked, progress) in raw)
                    _cache[factionId][slotId].Add((it, unlocked, progress));
            }

            // 2) Deduplicate and precompute keys
            _rowsByItemId.Clear();

            var snapshotKey = GetSnapshotKey(State.Slot);
            var slotList = new List<ItemTuple>(raw.Count);
            _fullTuples[snapshotKey] = slotList;

            foreach (var (item, isAvailable, isUnlocked, progress) in raw)
            {
                if (item == null)
                    continue;

                var id = item.StringId ?? item.GetHashCode().ToString();
                if (_rowsByItemId.ContainsKey(id))
                    continue;

                _rowsByItemId[id] = null;

                slotList.Add(
                    new ItemTuple
                    {
                        Item = item,
                        IsAvailable = isAvailable,
                        IsUnlocked = isUnlocked,
                        Progress = progress,
                        Name = item.Name ?? string.Empty,
                        Category = item.Class ?? string.Empty,
                        Tier = item.Tier,
                        Value = item.Value,
                        Cost = EquipmentManager.GetItemCost(item),
                        EnabledRank = (isUnlocked && isAvailable) ? 0 : 1,
                    }
                );
            }

            // Remember for which context this snapshot was built
            _fullTupleKeys[currentSlot] = contextKey;

            // 3) Build the visible list
            RebuildVisibleFromSnapshot();

            // Notify rows
            foreach (var r in EquipmentRows)
                r.OnSlotChanged();
        }

        /// <summary>
        /// Ensure we have a built snapshot for the current slot before sorting.
        /// </summary>
        private void EnsureSnapshotForCurrentSlot()
        {
            if (_needsRebuild)
            {
                // Build() will populate _fullTuples for State.Slot
                // and also rebuild the visible list once.
                Build();
            }
        }

        private void RebuildVisibleFromSnapshot()
        {
            // Get snapshot for the current slot (weapon slots share one snapshot)
            var snapshotKey = GetSnapshotKey(State.Slot);
            if (!_fullTuples.TryGetValue(snapshotKey, out var snapshot) || snapshot == null)
                snapshot = [];

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

                    case SortMode.Value:
                    {
                        int r = Primary(a.Value.CompareTo(b.Value));
                        if (r != 0)
                            return r;
                        return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    }
                }
                return 0;
            }

            // Sort the full snapshot first
            snapshot.Sort(Comparer<ItemTuple>.Create(Compare));

            // 2) Filter over the full snapshot
            var filtered = new List<ItemTuple>(snapshot.Count);
            var filterText = FilterText ?? string.Empty;
            var hasFilter = !string.IsNullOrWhiteSpace(filterText);

            if (!hasFilter)
            {
                filtered.AddRange(snapshot);
            }
            else
            {
                var search = filterText.Trim().ToLowerInvariant();

                bool Matches(ItemTuple t)
                {
                    if (t.Item == null)
                        return true;

                    // Use precomputed Name/Category plus type/culture
                    var name = t.Name?.ToLowerInvariant() ?? string.Empty;
                    var category = t.Category?.ToLowerInvariant() ?? string.Empty;
                    var type = t.Item.Type.ToString().ToLowerInvariant();
                    var culture =
                        t.Item.Culture?.Name?.ToString().ToLowerInvariant() ?? string.Empty;

                    return name.Contains(search)
                        || category.Contains(search)
                        || type.Contains(search)
                        || culture.Contains(search);
                }

                foreach (var t in snapshot)
                    if (Matches(t))
                        filtered.Add(t);
            }

            _filteredCount = filtered.Count;

            // 3) Compute pagination (using MaxRows as page size)
            if (_filteredCount == 0)
            {
                _totalPages = 1;
                _currentPageIndex = 0;
            }
            else
            {
                _totalPages = (_filteredCount + MaxRows - 1) / MaxRows;

                if (_currentPageIndex < 0)
                    _currentPageIndex = 0;
                if (_currentPageIndex >= _totalPages)
                    _currentPageIndex = _totalPages - 1;
            }

            var start = _currentPageIndex * MaxRows;
            var length = _filteredCount == 0 ? 0 : Math.Min(MaxRows, _filteredCount - start);

            var pageItems = length > 0 ? filtered.GetRange(start, length) : new List<ItemTuple>();

            // 4) Publish to UI (only these page items become VMs)
            EquipmentRows.Clear();
            _emptyRow = new EquipmentRowVM(null, 0, true, true, 0) { IsVisible = IsVisible };
            EquipmentRows.Add(_emptyRow);

            foreach (var t in pageItems)
            {
                var row = new EquipmentRowVM(
                    t.Item,
                    t.Cost,
                    t.IsAvailable,
                    t.IsUnlocked,
                    t.Progress
                )
                {
                    IsVisible = IsVisible,
                };
                EquipmentRows.Add(row);

                var id = t.Item?.StringId ?? t.Item?.GetHashCode().ToString();
                if (!string.IsNullOrEmpty(id))
                    _rowsByItemId[id] = row;
            }

            // Notify rows so comparisons and equipped flags update
            foreach (var r in EquipmentRows)
                r.OnSlotChanged();

            // Update sort state bindings for the header UI
            OnPropertyChanged(nameof(SortByNameSelected));
            OnPropertyChanged(nameof(SortByNameState));
            OnPropertyChanged(nameof(SortByCategorySelected));
            OnPropertyChanged(nameof(SortByCategoryState));
            OnPropertyChanged(nameof(SortByTierSelected));
            OnPropertyChanged(nameof(SortByTierState));
            OnPropertyChanged(nameof(SortByValueSelected));
            OnPropertyChanged(nameof(SortByValueState));

            // Paging bindings
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(CanGoPrevPage));
            OnPropertyChanged(nameof(CanGoNextPage));
            OnPropertyChanged(nameof(PageText));
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
        public bool SortByValueSelected => _sort == SortMode.Value;

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
        public CampaignUIHelper.SortState SortByValueState =>
            _sort == SortMode.Value
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
        public string SortByValueText => L.S("sort_value", "Value");

        /* ━━━━━━━━ Paging ━━━━━━━ */

        [DataSourceProperty]
        public int CurrentPage => _totalPages == 0 ? 0 : _currentPageIndex + 1;

        [DataSourceProperty]
        public int TotalPages => _totalPages;

        [DataSourceProperty]
        public bool CanGoPrevPage => CurrentPage > 1;

        [DataSourceProperty]
        public bool CanGoNextPage => CurrentPage < TotalPages;

        [DataSourceProperty]
        public string PageText => $"{CurrentPage}/{TotalPages}";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteSortByName()
        {
            EnsureSnapshotForCurrentSlot();

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
            EnsureSnapshotForCurrentSlot();

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
            EnsureSnapshotForCurrentSlot();

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
        public void ExecuteSortByValue()
        {
            EnsureSnapshotForCurrentSlot();

            if (_sort == SortMode.Value)
                _descending = !_descending;
            else
            {
                _sort = SortMode.Value;
                _descending = false;
            }
            RebuildVisibleFromSnapshot();
        }

        [DataSourceMethod]
        public void ExecutePrevPage()
        {
            EnsureSnapshotForCurrentSlot();

            if (!CanGoPrevPage)
                return;

            _currentPageIndex--;
            RebuildVisibleFromSnapshot();
        }

        [DataSourceMethod]
        public void ExecuteNextPage()
        {
            EnsureSnapshotForCurrentSlot();

            if (!CanGoNextPage)
                return;

            _currentPageIndex++;
            RebuildVisibleFromSnapshot();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override List<BaseListElementVM> Rows => [.. EquipmentRows];

        protected override void OnFilterTextChanged()
        {
            _currentPageIndex = 0;

            if (!IsVisible)
                return;

            // If a rebuild is pending (faction/slot change), let Build() handle it.
            if (_needsRebuild)
                return;

            RebuildVisibleFromSnapshot();
        }

        public override void RefreshFilter()
        {
            // Called when showing the screen; if a rebuild is pending, perform it.
            if (!IsVisible)
                return;

            if (_needsRebuild)
                Build();
            else
                RebuildVisibleFromSnapshot();
        }

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
