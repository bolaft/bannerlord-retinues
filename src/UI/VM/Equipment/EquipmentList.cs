using System.Linq;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Equipment
{
    public sealed class EquipmentListVM(EditorScreenVM screen) : BaseList<EquipmentListVM, EquipmentRowVM>(screen), IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public MBBindingList<EquipmentRowVM> Equipments { get; set; } = [];

        // =========================================================================
        // Public API
        // =========================================================================

        public override List<EquipmentRowVM> Rows => [.. Equipments];

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
                // Load items
                foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
                {
                    if (Config.AllEquipmentUnlocked)
                        items.Add(new WItem(item));  // All items
                    else
                    {
                        if (WItem.UnlockedItems.Contains(new WItem(item)))
                            items.Add(new WItem(item));  // Unlocked items
                        else if (item.Culture?.StringId == Screen.Faction.Culture.StringId)
                            items.Add(new WItem(item));  // Items of the faction's culture
                    }
                }

                // Filter by selected slot
                items = [.. items.Where(i => i is null || i.Slots.Contains(slot.Slot))];

                // Sort by type, then name
                items = [.. items.OrderBy(i => i.Type).ThenBy(i => i.Name)];

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
