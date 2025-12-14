using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Editor.Controllers;
using Retinues.Model.Equipments;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Equipment
{
    /// <summary>
    /// Row representing an item in the list.
    /// </summary>
    public sealed class EquipmentListRowVM(ListHeaderVM header, WItem item)
        : ListRowVM(header, item?.StringId ?? string.Empty)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal readonly WItem Item = item;

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
        public override bool IsSelected => State.Equipment.GetItem(State.Slot) == Item;

        [DataSourceMethod]
        public override void ExecuteSelect() => EquipmentController.EquipItem(Item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Enabled                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsEnabled
        {
            get
            {
                if (!Item.IsEquippableByCharacter(State.Character))
                    return false;

                return true;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Main                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => Item.Name;

        [DataSourceProperty]
        public int Value => Item.Value;

        [DataSourceProperty]
        public bool IsCivilian => Item.IsCivilian;

        private readonly int _tier = item?.Tier ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Data                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IsUnlocked => Item.IsUnlocked;

        [DataSourceProperty]
        public bool IsStocked => Stock > 0;

        [DataSourceProperty]
        public int Stock => Item.Stock;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Tooltip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        CharacterEquipmentItemVM _tooltip = null;

        [DataSourceProperty]
        public CharacterEquipmentItemVM Tooltip =>
            _tooltip ??= new CharacterEquipmentItemVM(Item.Base);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Images                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public object Image => Item.Image;

        [DataSourceProperty]
        public object CultureImage => Item.Culture?.Image;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal override IComparable GetSortValue(ListSortKey sortKey)
        {
            return sortKey switch
            {
                ListSortKey.Name => Name,
                ListSortKey.Tier => _tier,
                ListSortKey.Culture => Item.Culture?.Name ?? string.Empty,
                ListSortKey.Value => Item.Value,
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

            var categoryText = Item.Category.ToString();
            if (!string.IsNullOrEmpty(categoryText) && categoryText.IndexOf(f, comparison) >= 0)
                return true;

            var typeText = Item.Type.ToString();
            if (!string.IsNullOrEmpty(typeText) && typeText.IndexOf(f, comparison) >= 0)
                return true;

            var tierText = _tier.ToString();
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

            if (Item == null || CurrentItem == null)
                return;

            Item.GetComparisonChevrons(CurrentItem, out _positiveChevrons, out _negativeChevrons);

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
