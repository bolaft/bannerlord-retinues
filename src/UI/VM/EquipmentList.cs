using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Game.Troops.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentListVM(EquipmentEditorVM owner) : ViewModel
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private EquipmentEditorVM _owner = owner;

        private EquipmentRowVM _selectedRow;

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public TroopCharacter SelectedTroop => _owner.SelectedTroop;

        // =========================================================================
        // Public API
        // =========================================================================

        [DataSourceProperty]
        public MBBindingList<EquipmentRowVM> Equipments { get; } = [];

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            Equipments.Clear();

            var items = new List<TroopItem>();

            if (Config.AllEquipmentUnlocked)
                // Get all items
                foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
                items.Add(new TroopItem(item));
            else
                // Get only unlocked items
                items.AddRange(TroopItem.UnlockedItems);
            
            foreach (var item in items)
            {
                // Only equippable items
                if (!item.IsEquippable)
                    continue;

                // Only items that can be equipped by the selected troop
                if (!SelectedTroop.CanEquip(item))
                    continue;

                Equipments.Add(new EquipmentRowVM(this, item));
            }

            OnPropertyChanged(nameof(Equipments));
        }
    }
}