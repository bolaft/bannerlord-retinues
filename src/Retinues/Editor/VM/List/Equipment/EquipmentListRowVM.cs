using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Controllers.Equipment;
using Retinues.Editor.Events;
using Retinues.UI.Services;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Equipment
{
    /// <summary>
    /// Row representing an item in the list.
    /// </summary>
    public sealed class EquipmentListRowVM : BaseListRowVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WItem _item;

        public EquipmentListRowVM(ListHeaderVM header, WItem item)
            : base(header, item?.StringId ?? string.Empty)
        {
            _item = item;
            UpdateBrush();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsEquipment => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public override bool IsSelected => PreviewController.GetItem(State.Slot) == _item;

        [DataSourceMethod]
        public override void ExecuteSelect() => ItemController.Equip.Execute(_item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Enabled                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Item, Global = true)]
        private void UpdateBrush() => OnPropertyChanged(nameof(Brush));

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public override bool IsEnabled => ItemController.Equip.Allow(_item);

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public string DisabledReason =>
            ItemController.Equip.Reason(_item)?.ToString() ?? string.Empty;

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
        //                         Roster                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool AvailableInRoster => State.Equipment.IsAvailableInRoster(State.Slot, _item);
        private bool EconomyEnabled =>
            !PreviewController.Enabled
            && State.Mode == EditorMode.Player
            && Settings.EquipmentCostsGold;

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public bool ShowEquipped => IsEnabled && EconomyEnabled && AvailableInRoster;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlock                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IsUnlocked => _item.IsUnlocked;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Stock                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public bool ShowStock =>
            IsEnabled && EconomyEnabled && !AvailableInRoster && _item.Stock > 0;

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public string StockText =>
            L.T("in_stock", "In Stock ({STOCK})").SetTextVariable("STOCK", _item.Stock).ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Cost                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public bool ShowCost =>
            IsEnabled && EconomyEnabled && !AvailableInRoster && _item.Stock <= 0;

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public string CostFontColor
        {
            get
            {
                // if (EquipmentRebateBehavior.HasRebate(RowItem))
                //     return "#c5eb89ff"; // Light green

                return "#F4E1C4FF"; // Default color
            }
        }

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public int Cost
        {
            get
            {
                if (!ShowCost)
                    return 0;

                if (_item == null)
                    return 0;

                double multiplier = Settings.EquipmentCostMultiplier.Value;
                double raw = _item.Value * multiplier;

                int cost = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
                return Math.Max(cost, 0);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Tooltip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public CharacterEquipmentItemVM Tooltip => new(_item.Base);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Images                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public object Image => _item.Image;

        [DataSourceProperty]
        public object CultureImage => _item.Culture?.Image;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
        //                    Comparison Icons                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private int _positiveChevrons;
        private int _negativeChevrons;
        private string _chevronsKey = string.Empty;

        private WItem CurrentItem => PreviewController.GetItem(State.Slot);

        private void EnsureComparisonChevrons()
        {
            string currentId = CurrentItem?.StringId ?? "__NULL__";
            string enabled = IsEnabled ? "1" : "0";
            string key = enabled + "|" + currentId;

            if (string.Equals(key, _chevronsKey, StringComparison.Ordinal))
                return;

            _chevronsKey = key;
            _positiveChevrons = 0;
            _negativeChevrons = 0;

            if (!IsEnabled)
                return;

            if (_item == null || CurrentItem == null)
                return;

            _item.GetComparisonChevrons(CurrentItem, out _positiveChevrons, out _negativeChevrons);

            if (_positiveChevrons > 3)
                _positiveChevrons = 3;

            if (_negativeChevrons > 3)
                _negativeChevrons = 3;
        }

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public bool ShowComparisonIcon
        {
            get
            {
                EnsureComparisonChevrons();
                return _positiveChevrons > 0 || _negativeChevrons > 0;
            }
        }

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public string PositiveComparisonSprite
        {
            get
            {
                EnsureComparisonChevrons();
                if (_positiveChevrons <= 0)
                    return string.Empty;

                return $"General\\TroopTierIcons\\icon_tier_{_positiveChevrons}_big";
            }
        }

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public string NegativeComparisonSprite
        {
            get
            {
                EnsureComparisonChevrons();
                if (_negativeChevrons <= 0)
                    return string.Empty;

                return $"General\\TroopTierIcons\\icon_tier_{_negativeChevrons}_big";
            }
        }

        [EventListener(UIEvent.Item, Global = true)]
        [DataSourceProperty]
        public int NegativeComparisonSpriteOffset
        {
            get
            {
                EnsureComparisonChevrons();
                if (_negativeChevrons > 0 && _positiveChevrons > 0)
                    return 5;

                return 0;
            }
        }
    }
}
