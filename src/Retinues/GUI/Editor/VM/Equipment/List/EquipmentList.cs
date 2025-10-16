using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment.Panel;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment.List
{
    [SafeClass]
    public sealed class EquipmentListVM : BaseList<EquipmentListVM, EquipmentRowVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Dictionary<
            EquipmentIndex,
            List<(WItem item, int? progress)>
        > _eligibilityCache = [];

        public readonly EditorVM Editor;

        public EquipmentListVM(EditorVM editor)
        {
            Log.Info("Building EquipmentListVM...");

            Editor = editor;
        }

        public void Initialize()
        {
            Log.Info("Initializing EquipmentListVM...");

            // Subscribe to events
            EventManager.FactionChange.Register(_eligibilityCache.Clear);
            EventManager.EquipmentChange.Register(() => {
                _eligibilityCache.Clear();
                BuildRows();
            });
            EventManager.EquipmentSlotChange.Register(BuildRows);
            EventManager.TroopChange.Register(BuildRows);
        }

        public void BuildRows()
        {
            var mapping = EquipmentManager.CollectAvailableItems(
                SelectedFaction,
                SelectedSlot?.Index ?? EquipmentIndex.Head,
                civilian: SelectedEquipment?.IsCivilian ?? false,
                cache: _eligibilityCache
            );

            Rows.Clear();
            EquipmentRows.Clear();

            foreach (var (item, progress, available) in mapping)
            {
                var row = new EquipmentRowVM(Editor, item, progress, available);

                if (item == SelectedSlot?.Item)
                    row.IsSelected = true;

                Rows.Add(row);
            }

            Select(SelectedSlot?.Item);

            foreach (var row in Rows)
            {
                row.Initialize();
                EquipmentRows.Add(row);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private EquipmentSlotVM SelectedSlot => Editor?.EquipmentScreen?.EquipmentPanel?.Selection;
        private WEquipment SelectedEquipment => Editor?.EquipmentScreen?.Equipment;
        private WFaction SelectedFaction => Editor?.Faction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override List<EquipmentRowVM> Rows { get; protected set; } = [];

        [DataSourceProperty]
        public MBBindingList<EquipmentRowVM> EquipmentRows { get; private set; } = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Selects the row for the given item, if present.
        /// </summary>
        public void Select(WItem item)
        {
            Select(Rows.FirstOrDefault(r => r.Item == item));
        }
    }
}
