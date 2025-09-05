using System.Linq;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Equipment
{
    public sealed class EquipmentListVM(UI.ClanScreen screen) : BaseList<EquipmentListVM, EquipmentRowVM>(screen), IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public MBBindingList<EquipmentRowVM> Equipments { get; set; } = new();

        // =========================================================================
        // Public API
        // =========================================================================

        public override List<EquipmentRowVM> Rows => Equipments.ToList();

        public void Select(WItem item)
        {
            var row = Rows.FirstOrDefault(r => r.Item.Equals(item));
            if (row is not null)
                Select(row);
        }

        public void Refresh()
        {
            Log.Debug("Refreshing.");

            // Clear existing VM list
            Equipments.Clear();

            // Initialize item list
            var items = new List<WItem>();

            // Selected slot
            var slot = Screen.EquipmentEditor.SelectedSlot;

            // Only load items if a slot is selected
            if (slot != null)
            {
                // Load items, method depends on config
                if (Config.AllEquipmentUnlocked)
                    foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
                        items.Add(new WItem(item));
                else
                    foreach (var item in WItem.UnlockedItems)
                        items.Add(item);

                // Filter by selected slot
                items = items.Where(i => i is null || i.Slots.Contains(slot.Slot)).ToList();

                // Sort by type, then name
                items = items.OrderBy(i => i.Type).ThenBy(i => i.Name).ToList();

                // Empty item to allow unequipping
                items.Insert(0, null);

                foreach (var item in items)
                {
                    // Create row VM
                    var row = new EquipmentRowVM(item, this);

                    // Preselect equipped item row
                    if (item == slot.Item || item?.StringId == slot.Item?.StringId)
                        row.IsSelected = true;

                    // Add to list
                    Equipments.Add(row);
                }
            }

            OnPropertyChanged(nameof(Equipments));
        }
    }
}
