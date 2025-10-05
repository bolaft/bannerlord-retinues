using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Equipment
{
    /// <summary>
    /// ViewModel for equipment list. Handles item search, selection, and refreshing available equipment rows.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentListVM(EditorScreenVM screen)
        : BaseList<EquipmentListVM, EquipmentRowVM>(screen)
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
                {
                    equipment.RefreshVisibility(_searchText);
                }
                OnPropertyChanged(nameof(SearchText));
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
        /// Refreshes all equipment rows for the selected slot and troop.
        /// </summary>
        public void Refresh()
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
                    slot.Slot
                );

                foreach (var (item, progress) in items)
                {
                    // Create row VM
                    var row = new EquipmentRowVM(item, this, progress);

                    // Preselect equipped item row
                    if (item == slot.Item)
                        row.IsSelected = true;

                    // Add to list
                    Equipments.Add(row);
                }
            }

            OnPropertyChanged(nameof(Equipments));
        }
    }
}
