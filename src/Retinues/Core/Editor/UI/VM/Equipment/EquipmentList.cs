using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Equipment
{
    public sealed class EquipmentListVM(EditorScreenVM screen)
        : BaseList<EquipmentListVM, EquipmentRowVM>(screen),
            IView
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

            // Selected slot
            var slot = Screen.EquipmentEditor.SelectedSlot;

            // Only load items if a slot is selected
            if (slot != null)
            {
                var items = EquipmentManager.CollectAvailableItems(
                    Screen.SelectedTroop,
                    Screen.Faction,
                    slot.Slot
                );

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
