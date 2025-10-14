using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment
{
    /// <summary>
    /// ViewModel for equipment list. Handles item search, selection, and refreshing available equipment rows.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentListVM(
        WCharacter troop,
        WFaction faction,
        EquipmentIndex index,
        bool civilian
    ) : BaseList<EquipmentListVM, EquipmentRowVM>
    {
        public IEnumerable<(WItem, int?, bool)> AvailableMapping =
            EquipmentManager.CollectAvailableItems(troop, faction, index, civilianOnly: civilian);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<EquipmentRowVM> Items
        {
            get
            {
                var list = new MBBindingList<EquipmentRowVM>();
                foreach (var (item, progress, available) in AvailableMapping)
                {
                    var row = new EquipmentRowVM(
                        this,
                        Editor.EquipmentPanel.Selection,
                        item,
                        progress,
                        available
                    );

                    if (item == Editor.EquipmentPanel.Selection.Item)
                        row.IsSelected = true;

                    list.Add(row);
                }
                return list;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Override for BaseList functionality.
        /// </summary>
        public override List<EquipmentRowVM> Rows => [.. Items];

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
    }
}
