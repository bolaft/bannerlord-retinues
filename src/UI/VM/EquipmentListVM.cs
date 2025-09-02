using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Config;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentListVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        [DataSourceProperty] public MBBindingList<EquipmentRowVM> Equipments { get; } = new();

        public EquipmentListVM(ClanManagementMixinVM owner) => _owner = owner;

        public void Refresh()
        {
            var selectedSlot = _owner.EquipmentEditor?.SelectedSlot;
            var selectedItem = selectedSlot?.Item;

            Log.Debug($"EquipmentListVM.Refresh(): selectedItem = {selectedItem?.StringId}");

            EquipmentRowVM currentRow = null;

            Equipments.Clear();

            var items = EquipmentManager.GetUnlockedItems(selectedSlot.Slot)
                .OrderBy(i => i.ItemType)
                .ThenBy(i => i.Name?.ToString() ?? string.Empty);

            foreach (var item in items)
            {
                // Items limited by tier
                if (ModConfig.DisallowHigherTierEquipment)
                {
                    int tierIndex = Array.IndexOf(Enum.GetValues(item.Tier.GetType()), item.Tier) + 1;
                    if (tierIndex > _owner.SelectedTroop.Tier)
                        continue;
                }
                var row = new EquipmentRowVM(item, _owner.SelectedTroop.CanEquip(item), _owner.EquipmentEditor.HandleRowSelected);
                if (selectedItem != null && item.StringId == selectedItem.StringId)
                    currentRow = row;
                Equipments.Add(row);
            }

            _owner.EquipmentEditor.HandleRowSelected(currentRow);

            OnPropertyChanged(nameof(Equipments));

            Log.Debug($"EquipmentListVM.Refresh(): {Equipments.Count} items loaded");
        }
    }
}
