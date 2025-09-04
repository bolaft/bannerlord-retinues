using System.Linq;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Equipment
{
    public sealed class EquipmentListVM(EquipmentEditorVM owner) : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly EquipmentEditorVM _owner = owner;

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public WCharacter SelectedTroop => _owner.SelectedTroop;

        // =========================================================================
        // Public API
        // =========================================================================

        [DataSourceProperty]
        public EquipmentRowVM SelectedRow => Equipments.FirstOrDefault(t => t.IsSelected);

        [DataSourceProperty]
        public MBBindingList<EquipmentRowVM> Equipments { get; } = [];

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing Equipment List.");

            Equipments.Clear();

            var items = new List<WItem>();

            if (Config.AllEquipmentUnlocked)
                // Get all items
                foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
                items.Add(new WItem(item));
            else
                // Get only unlocked items
                items.AddRange(WItem.UnlockedItems);
            
            foreach (var item in items)
            {
                // Only equippable items
                if (!item.IsEquippable)
                    continue;

                Equipments.Add(new EquipmentRowVM(item, this));
            }

            OnPropertyChanged(nameof(SelectedTroop));
            OnPropertyChanged(nameof(Equipments));
        }
    }
}