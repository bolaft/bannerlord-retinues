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

        private readonly EquipmentEditorVM _owner;

        private readonly EquipmentIndex _slot;

        public EquipmentIndex Slot => _slot;

        public EquipmentSlotVM(Equipment equipment, EquipmentIndex slot, bool isEnabled, EquipmentEditorVM owner)
        {
            _equipment = equipment;
            _slot = slot;
            _owner = owner;
            IsEnabled = isEnabled;
        }

        [DataSourceProperty] public string DisplayName => Item?.Name?.ToString() ?? "";

        [DataSourceProperty] public bool IsSelected => _owner.SelectedSlot == this;

        [DataSourceProperty] public int ImageTypeCode => Image?.ImageTypeCode ?? 0;

        [DataSourceProperty] public string ImageId => Image?.Id ?? "";

        [DataSourceProperty] public string ImageAdditionalArgs => Image?.AdditionalArgs ?? "";

        [DataSourceProperty] public bool IsEnabled { get; private set; }

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
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(IsEnabled));
        }

        [DataSourceMethod] public void ExecuteSelect()
        {
            _owner.HandleSlotSelected(this);
        }
    }
}
