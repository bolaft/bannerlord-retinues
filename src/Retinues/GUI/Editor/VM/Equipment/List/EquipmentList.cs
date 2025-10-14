using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment.Panel;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment.List
{
    [SafeClass]
    public sealed class EquipmentListVM : BaseList<EquipmentListVM, EquipmentRowVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static EquipmentSlotVM SelectedSlot =>
            Editor.EquipmentScreen.EquipmentPanel.Selection;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override MBBindingList<EquipmentRowVM> Rows
        {
            get
            {
                var mapping = EquipmentManager.CollectAvailableItems(
                    SelectedTroop,
                    SelectedFaction,
                    SelectedSlot.Index,
                    civilian: SelectedEquipment.IsCivilian
                );

                var list = new MBBindingList<EquipmentRowVM>();
                foreach (var (item, progress, available) in mapping)
                {
                    var row = new EquipmentRowVM(item, progress, available);

                    if (item == SelectedSlot.Item)
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
        /// Selects the row for the given item, if present.
        /// </summary>
        public void Select(WItem item)
        {
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.Label));
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.ItemText));
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.IsStaged));
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.Hint));
#if BL13
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.ImageId));
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.ImageAdditionalArgs));
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.ImageTextureProviderName));
#else
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.ImageId));
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.ImageAdditionalArgs));
            SelectedSlot.OnPropertyChanged(nameof(SelectedSlot.ImageTypeCode));
#endif
            OnPropertyChanged(nameof(Rows));
            OnPropertyChanged(nameof(Selection));
        }
    }
}
