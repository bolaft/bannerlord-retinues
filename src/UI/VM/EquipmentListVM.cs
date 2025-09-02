using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Logic;
using CustomClanTroops.Logic.Items;
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

            var items = UnlockManager.GetUnlockedItems(selectedSlot.Slot)
                .OrderBy(i => i.ItemType)
                .ThenBy(i => i.Name?.ToString() ?? string.Empty);

            // Empty slot
            var emptyRow = new EquipmentRowVM(null, true, _owner.EquipmentEditor.HandleRowClicked);
            emptyRow.IsSelected = selectedItem == null;
            Equipments.Add(emptyRow);

            foreach (var item in items)
            {
                // Items limited by tier
                if (Config.LimitEquipmentByTier)
                {
                    int tierIndex = Array.IndexOf(Enum.GetValues(item.Tier.GetType()), item.Tier);
                    int allowedDifference = 3;
                    if (tierIndex + 1 - allowedDifference > _owner.SelectedTroop.Tier)
                        continue;
                }
                var row = new EquipmentRowVM(item, _owner.SelectedTroop.CanEquip(item), _owner.EquipmentEditor.HandleRowClicked);
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
