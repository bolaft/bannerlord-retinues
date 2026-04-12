using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Equipment.Controllers;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Equipment.Views.List
{
    /// <summary>
    /// Row representing an item in the list.
    /// </summary>
    public sealed class EquipmentListRowVM(ListHeaderVM header, WItem item)
        : BaseListRowVM(header, item?.StringId ?? string.Empty)
    {
        // Per-row cache group key. All cached properties for this row share it.
        private static string CacheKey(WItem item) =>
            $"EquipmentListRowVM_{item.StringId ?? "null"}";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsEquipment => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WItem _item = item;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WItem CurrentItem => PreviewController.GetItem(State.Slot);

        // Selection must be correct immediately on slot changes (auto-scroll),
        // so do not cache it (caching can lag by one event depending on listener order).
        [DataSourceProperty]
        public override bool IsSelected => CurrentItem == _item;

        [DataSourceMethod]
        public override void ExecuteSelect() => ItemController.Equip.Execute(_item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Enabled                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Cache<EquipmentListRowVM, bool> _cacheIsEnabled = new(
            o => ItemController.Equip.Allow(o._item),
            CacheKey(item)
        );

        [DataSourceProperty]
        public override bool IsEnabled => _cacheIsEnabled.Get(this);

        private readonly Cache<EquipmentListRowVM, string> _cacheDisabledReason = new(
            o =>
            {
                // Use a compact message when the block is a context restriction —
                // the full sentence is too long for an item row.
                var contextReason = ContextRestrictionService.GetCharacterEditingBlockReasonShort();
                if (contextReason != null)
                    return contextReason.ToString();

                return ItemController.Equip.Reason(o._item)?.ToString() ?? string.Empty;
            },
            CacheKey(item)
        );

        [DataSourceProperty]
        public string DisabledReason => _cacheDisabledReason.Get(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Main                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => _item.Name;

        [DataSourceProperty]
        public int Value => _item.Value;

        [DataSourceProperty]
        public bool IsCivilian => _item.IsCivilian;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Tooltip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Cache<EquipmentListRowVM, CharacterEquipmentItemVM> _cacheTooltip = new(
            o => o._item != null ? new CharacterEquipmentItemVM(o._item.Base) : null
        );

        [DataSourceProperty]
        public CharacterEquipmentItemVM Tooltip => _cacheTooltip.Get(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Images                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public object Image => _item.Image;

        [DataSourceProperty]
        public object CultureImage => _item.Culture?.Image;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Roster                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Cache<EquipmentListRowVM, bool> _cacheAvailableInRoster = new(
            o => State.Equipment?.IsAvailableInRoster(State.Slot, o._item) ?? false,
            CacheKey(item)
        );

        private bool AvailableInRoster => _cacheAvailableInRoster.Get(this);

        private readonly Cache<EquipmentListRowVM, bool> _cacheEconomyEnabled = new(
            o =>
                !PreviewController.Enabled
                && State.Mode == EditorMode.Player
                && Configuration.EquipmentCostsMoney,
            CacheKey(item)
        );

        private bool EconomyEnabled => _cacheEconomyEnabled.Get(this);

        private readonly Cache<EquipmentListRowVM, bool> _cacheShowEquipped = new(
            o => o.IsEnabled && o.EconomyEnabled && o.AvailableInRoster,
            CacheKey(item)
        );

        [DataSourceProperty]
        public bool ShowEquipped => _cacheShowEquipped.Get(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlock                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IsUnlocked => _item.IsUnlocked;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Stock                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Cache<EquipmentListRowVM, bool> _cacheShowStock = new(
            o =>
                o.IsEnabled
                && o.EconomyEnabled
                && !o.IsSelected
                && !o.AvailableInRoster
                && o._item != null
                && o._item.Stock > 0,
            CacheKey(item)
        );

        [DataSourceProperty]
        public bool ShowStock => _cacheShowStock.Get(this);

        private readonly Cache<EquipmentListRowVM, string> _cacheStockText = new(
            o =>
            {
                if (!o.ShowStock || o._item == null)
                    return string.Empty;

                return L.T("in_stock", "In Stock ({STOCK})")
                    .SetTextVariable("STOCK", o._item.Stock)
                    .ToString();
            },
            CacheKey(item)
        );

        [DataSourceProperty]
        public string StockText => _cacheStockText.Get(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Cost                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Cache<EquipmentListRowVM, bool> _cacheShowCost = new(
            o =>
                o.IsEnabled
                && o.EconomyEnabled
                && !o.IsSelected
                && !o.AvailableInRoster
                && o._item != null
                && o._item.Stock <= 0,
            CacheKey(item)
        );

        [DataSourceProperty]
        public bool ShowCost => _cacheShowCost.Get(this);

        [DataSourceProperty]
        public string CostFontColor => "#F4E1C4FF"; // Default color

        private readonly Cache<EquipmentListRowVM, int> _cacheCost = new(
            o =>
            {
                if (!o.ShowCost || o._item == null)
                    return 0;

                double multiplier = Configuration.EquipmentCostMultiplier;
                double raw = o._item.Value * multiplier;

                int cost = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
                return Math.Max(cost, 0);
            },
            CacheKey(item)
        );

        [DataSourceProperty]
        public int Cost => _cacheCost.Get(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Comparison Icons                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Cache<EquipmentListRowVM, (int pos, int neg)> _cacheChevrons = new(
            o =>
            {
                if (!o.IsEnabled)
                    return (0, 0);

                if (o._item == null)
                    return (0, 0);

                var current = o.CurrentItem;
                if (current == null)
                    return (0, 0);

                o._item.GetComparisonChevrons(current, out int p, out int n);

                if (p > 3)
                    p = 3;
                if (n > 3)
                    n = 3;

                return (p, n);
            },
            CacheKey(item)
        );

        [DataSourceProperty]
        public bool ShowComparisonIcon
        {
            get
            {
                var (p, n) = _cacheChevrons.Get(this);
                return p > 0 || n > 0;
            }
        }

        [DataSourceProperty]
        public string PositiveComparisonSprite
        {
            get
            {
                var (p, _) = _cacheChevrons.Get(this);
                if (p <= 0)
                    return string.Empty;

                return $"General\\TroopTierIcons\\icon_tier_{p}_big";
            }
        }

        [DataSourceProperty]
        public string NegativeComparisonSprite
        {
            get
            {
                var (_, n) = _cacheChevrons.Get(this);
                if (n <= 0)
                    return string.Empty;

                return $"General\\TroopTierIcons\\icon_tier_{n}_big";
            }
        }

        [DataSourceProperty]
        public int NegativeComparisonSpriteOffset
        {
            get
            {
                var (p, n) = _cacheChevrons.Get(this);
                if (n > 0 && p > 0)
                    return 5;

                return 0;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the sort key value used by the base list sorter.
        /// </summary>
        internal override IComparable GetSortValue(ListSortKey sortKey)
        {
            return sortKey switch
            {
                ListSortKey.Name => Name,
                ListSortKey.Tier => _item.Tier,
                ListSortKey.Culture => _item.Culture?.Name ?? string.Empty,
                ListSortKey.Value => _item.Value,
                _ => Name,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Filtering                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines whether this row matches the given filter.
        /// </summary>
        internal override bool MatchesFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var f = filter.Trim();
            var comparison = StringComparison.OrdinalIgnoreCase;

            if (!string.IsNullOrEmpty(Name) && Name.IndexOf(f, comparison) >= 0)
                return true;

            var categoryText = _item.Category.ToString();
            if (!string.IsNullOrEmpty(categoryText) && categoryText.IndexOf(f, comparison) >= 0)
                return true;

            var typeText = _item.Type.ToString();
            if (!string.IsNullOrEmpty(typeText) && typeText.IndexOf(f, comparison) >= 0)
                return true;

            var tierText = _item.Tier.ToString();
            if (!string.IsNullOrEmpty(tierText) && tierText.IndexOf(f, comparison) >= 0)
                return true;

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Invalidation                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Invalidates cached values and refreshes UI-bound properties.
        /// </summary>
        [EventListener(
            UIEvent.Slot,
            UIEvent.Item,
            UIEvent.Preview,
            UIEvent.Crafted,
            UIEvent.Doctrine,
            Global = true
        )]
        private void InvalidateComputed()
        {
            // Clear all cached values for this row (group clear).
            // Any one cache Clear() clears the whole group for CacheKey(item).
            _cacheIsEnabled.Clear();

            // Notify only the properties that actually depend on slot/item/mode.
            OnPropertyChanged(nameof(Brush));

            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(DisabledReason));

            OnPropertyChanged(nameof(ShowEquipped));
            OnPropertyChanged(nameof(ShowStock));
            OnPropertyChanged(nameof(StockText));
            OnPropertyChanged(nameof(ShowCost));
            OnPropertyChanged(nameof(Cost));

            OnPropertyChanged(nameof(ShowComparisonIcon));
            OnPropertyChanged(nameof(PositiveComparisonSprite));
            OnPropertyChanged(nameof(NegativeComparisonSprite));
            OnPropertyChanged(nameof(NegativeComparisonSpriteOffset));
        }
    }
}
