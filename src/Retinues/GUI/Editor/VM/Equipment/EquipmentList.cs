using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment
{
    /// <summary>
    /// ViewModel for equipment list. Handles item search, selection, and refreshing available equipment rows.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentListVM : BaseList<EquipmentListVM, EquipmentRowVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<EquipmentRowVM> Equipments { get; set; } = [];

        [DataSourceProperty]
        public string SearchLabel => L.S("item_search_label", "Filter:");

        private string _searchText;

        [DataSourceProperty]
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;
                _searchText = value;
                foreach (var equipment in Equipments)
                    equipment.UpdateIsVisible(_searchText);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override List<EquipmentRowVM> Rows => [.. Equipments];

        /// <summary>
        /// Selects the row for the given item, if present.
        /// </summary>
        public void Select(WItem item)
        {
            var row = Rows.FirstOrDefault(r =>
                (r.Item == null && item == null) || (r.Item == item)
            );

            if (row is not null)
                Select(row);
        }

        /// <summary>
        /// Rebuilds all equipment rows for the selected slot and troop.
        /// </summary>
        public void Rebuild()
        {
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
                    slot.Slot,
                    civilianOnly: Screen.EquipmentEditor.LoadoutCategory
                        == WLoadout.Category.Civilian
                );

                foreach (var (item, progress, available) in items)
                {
                    // Create row VM
                    var row = new EquipmentRowVM(item, this, progress, available);

                    // Preselect equipped/staged item row
                    if (
                        (item != null && item == slot.StagedItem)
                        || (slot.StagedItem == null && item == slot.Item)
                    )
                        row.IsSelected = true;

                    // Add to list
                    Equipments.Add(row);
                }
            }
        }
    }
}
