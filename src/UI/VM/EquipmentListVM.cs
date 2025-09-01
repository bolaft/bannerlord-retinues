using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentListVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        [DataSourceProperty] public MBBindingList<EquipmentRowVM> Equipments { get; } = new();

        public EquipmentListVM(ClanManagementMixinVM owner) => _owner = owner;

        public void Refresh()
        {
            // // Placeholder: get only the item in slot
            // var selectedItem = _owner.EquipmentEditor.SelectedSlot?.Item;
            // var items = selectedItem != null ? new[] { selectedItem } : Array.Empty<ItemObject>();

            // Equipments.Clear();

            // foreach (var item in items)
            //     Equipments.Add(new EquipmentRowVM(item, _owner.EquipmentEditor.HandleRowSelected));

            // OnPropertyChanged(nameof(Equipments));
        }
    }
}
