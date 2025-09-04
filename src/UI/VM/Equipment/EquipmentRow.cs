using TaleWorlds.Library;
using TaleWorlds.Core.ViewModelCollection.Information;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Equipment
{
    public sealed class EquipmentRowVM(WItem item, EquipmentListVM owner) : RowBase<EquipmentListVM, EquipmentRowVM>(owner), IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Helpers.ItemTooltip.Make(Item);

        [DataSourceProperty]
        public string Name => Item?.Name;

        [DataSourceProperty]
        public int Value => Item?.Value ?? 0;

        [DataSourceProperty]
        public bool ShowValue
        {
            get
            {
                if (!Config.PayForTroopEquipment) return false;
                if (Item == null) return false;
                // If paying, we display the value (even if stock is zero)
                return true;
            }
        }

        [DataSourceProperty]
        public int Stock => Item?.GetStock() ?? 0;

        [DataSourceProperty]
        public bool ShowStock
        {
            get
            {
                if (!Config.PayForTroopEquipment) return false;
                if (Item == null) return false;
                if (Stock == 0) return false;
                return true;
            }
        }

        [DataSourceProperty]
        public bool CanEquip => _owner.Owner.SelectedTroop?.CanEquip(Item) ?? false;

        [DataSourceProperty]
        public string ImageId => Item?.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Item?.Image.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item?.Image.AdditionalArgs;

        // =========================================================================
        // Public API
        // =========================================================================

        public WItem Item { get; } = item;

        public void Refresh()
        {
            OnPropertyChanged(nameof(Item));
            OnPropertyChanged(nameof(Hint));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(ShowValue));
            OnPropertyChanged(nameof(Stock));
            OnPropertyChanged(nameof(ShowStock));
            OnPropertyChanged(nameof(CanEquip));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
        }
    }
}