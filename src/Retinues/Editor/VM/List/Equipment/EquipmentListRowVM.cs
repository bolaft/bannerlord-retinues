using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Model.Equipments;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Editor.VM.List.Equipment
{
    /// <summary>
    /// Row representing an item in the list.
    /// </summary>
    public sealed class EquipmentListRowVM : ListRowVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WItem _item;

        public EquipmentListRowVM(ListHeaderVM header, WItem item)
            : base(header, item?.StringId ?? string.Empty)
        {
            _item = item;
            UpdateDisabledReason();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsEquipment => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public override bool IsSelected => State.Equipment.GetItem(State.Slot) == _item;

        [DataSourceMethod]
        public override void ExecuteSelect() => EquipmentController.EquipItem(_item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Enabled                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private TextObject _disabledReason;

        [EventListener(UIEvent.Equipment)]
        private void UpdateDisabledReason()
        {
            EquipmentController.CanEquipItem(_item, out _disabledReason);
            OnPropertyChanged(nameof(DisabledReason));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(Brush));
        }

        [DataSourceProperty]
        public string DisabledReason => _disabledReason?.ToString() ?? string.Empty;

        [DataSourceProperty]
        public override bool IsEnabled => _disabledReason == null;

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
        //                          Data                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IsUnlocked => _item.IsUnlocked;

        [DataSourceProperty]
        public bool IsStocked => Stock > 0;

        [DataSourceProperty]
        public int Stock => _item.Stock;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Tooltip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        CharacterEquipmentItemVM _tooltip = null;

        [DataSourceProperty]
        public CharacterEquipmentItemVM Tooltip =>
            _tooltip ??= new CharacterEquipmentItemVM(_item.Base);

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

        private WItem CurrentItem => State.Equipment.GetItem(State.Slot);

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

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public bool ShowComparisonIcon
        {
            get
            {
                EnsureComparisonChevrons();
                return _positiveChevrons > 0 || _negativeChevrons > 0;
            }
        }

        [EventListener(UIEvent.Item)]
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

        [EventListener(UIEvent.Item)]
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

        [EventListener(UIEvent.Item)]
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
