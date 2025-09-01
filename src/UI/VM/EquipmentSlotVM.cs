using System;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.PlayerServices;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentSlotVM : ViewModel
    {
        private readonly Equipment _equipment;

        private readonly EquipmentIndex _slot;

        private readonly EquipmentEditorVM _owner;

        public EquipmentSlotVM(Equipment equipment, EquipmentIndex slot, EquipmentEditorVM owner)
        {
            _equipment = equipment;
            _slot = slot;
            _owner = owner;

            Refresh();
        }

        [DataSourceProperty] public string DisplayName => Item?.Name?.ToString() ?? "";

        [DataSourceProperty] public bool IsSelected => _owner.SelectedSlot == this;

        [DataSourceProperty] public string Brush => IsSelected ? "ButtonBrush1" : "ButtonBrush2";

        [DataSourceProperty] public int ImageTypeCode => Image?.ImageTypeCode ?? 0;

        [DataSourceProperty] public string ImageId => Image?.Id ?? "";

        [DataSourceProperty] public string ImageAdditionalArgs => Image?.AdditionalArgs ?? "";

        private ImageIdentifierVM Image => Item != null ? new ImageIdentifierVM(Item, "") : null;

        public ItemObject Item
        {
            get
            {
                try
                {
                    return _equipment[_slot].Item;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public void Refresh()
        {
            Log.Debug($"EquipmentSlotVM.Refresh({_slot})");

            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(Brush));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
            OnPropertyChanged(nameof(IsSelected));
        }

        [DataSourceMethod] public void ExecuteSelect()
        {
            _owner.HandleSlotSelected(this);
        }
    }
}
