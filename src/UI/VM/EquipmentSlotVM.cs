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

        public EquipmentSlotVM(Equipment equipment, EquipmentIndex slot)
        {
            _equipment = equipment;
            _slot = slot;
            Refresh();
        }

        [DataSourceProperty]
        public string DisplayName
        {
            get
            {
                var item = GetElement().Item;
                return item?.Name?.ToString() ?? "";
            }
        }

        [DataSourceProperty] public int ImageTypeCode => Image?.ImageTypeCode ?? 0;

        [DataSourceProperty] public string ImageId => Image?.Id ?? "";

        [DataSourceProperty] public string ImageAdditionalArgs => Image?.AdditionalArgs ?? "";

        private ImageIdentifierVM Image
        {
            get
            {
                var item = GetElement().Item;
                return item != null ? new ImageIdentifierVM(item, "") : null;
            }
        }

        public void Refresh()
        {
            Log.Debug($"EquipmentSlotVM.Refresh({_slot})");

            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
        }

        private EquipmentElement GetElement()
        {
            try
            {
                var el = _equipment[_slot];
                if (el.IsEmpty)
                    Log.Debug($"GetElement({_slot}): empty");
                else
                    Log.Debug($"GetElement({_slot}): item={el.Item?.StringId ?? "null"}");
                return el;
            }
            catch (Exception ex)
            {
                Log.Debug($"GetElement({_slot}): exception {ex.Message}");
                return default;
            }
        }
    }
}
