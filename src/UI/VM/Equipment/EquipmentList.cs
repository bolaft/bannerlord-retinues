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
            Log.Debug("Refreshing Equipment List.");

            var items = new List<WItem>();

            if (Config.AllEquipmentUnlocked)
            {
                foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
                    items.Add(new WItem(item));
            }
            else
            {
                items.AddRange(WItem.UnlockedItems);
            }

            items = items.OrderBy(i => i.Type).ThenBy(i => i.Name).ToList();

            Equipments.Clear();
            foreach (var item in items)
                Equipments.Add(new EquipmentRowVM(item, this));

            OnPropertyChanged(nameof(Equipments));
        }
    }
}
