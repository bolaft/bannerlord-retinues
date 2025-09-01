using System;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.PlayerServices;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentEditorVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        private CharacterWrapper Troop => _owner.SelectedTroop;

        public EquipmentEditorVM(ClanManagementMixinVM owner)
        {
            _owner = owner;
            Build();
        }

        public void Refresh()
        {
            Build();

            // notify all bound properties
            OnPropertyChanged(nameof(HeadSlot));
            OnPropertyChanged(nameof(CloakSlot));
            OnPropertyChanged(nameof(BodySlot));
            OnPropertyChanged(nameof(GlovesSlot));
            OnPropertyChanged(nameof(BootsSlot));
            OnPropertyChanged(nameof(MountSlot));
            OnPropertyChanged(nameof(HarnessSlot));
            OnPropertyChanged(nameof(Weapon1Slot));
            OnPropertyChanged(nameof(Weapon2Slot));
            OnPropertyChanged(nameof(Weapon3Slot));
            OnPropertyChanged(nameof(Weapon4Slot));
        }

        [DataSourceProperty] public EquipmentSlotVM HeadSlot    { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM CloakSlot   { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM BodySlot    { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM GlovesSlot  { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM BootsSlot   { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM MountSlot   { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM HarnessSlot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM Weapon1Slot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM Weapon2Slot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM Weapon3Slot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM Weapon4Slot { get; private set; }

        private void Build()
        {
            var eq = GetPrimaryEquipment();

            HeadSlot    = new EquipmentSlotVM(eq, EquipmentIndex.Head);
            CloakSlot   = new EquipmentSlotVM(eq, EquipmentIndex.Cape);
            BodySlot    = new EquipmentSlotVM(eq, EquipmentIndex.Body);
            GlovesSlot  = new EquipmentSlotVM(eq, EquipmentIndex.Gloves);
            BootsSlot   = new EquipmentSlotVM(eq, EquipmentIndex.Leg);
            MountSlot   = new EquipmentSlotVM(eq, EquipmentIndex.Horse);
            HarnessSlot = new EquipmentSlotVM(eq, EquipmentIndex.HorseHarness);

            Weapon1Slot = new EquipmentSlotVM(eq, EquipmentIndex.WeaponItemBeginSlot);
            Weapon2Slot = new EquipmentSlotVM(eq, EquipmentIndex.Weapon1);
            Weapon3Slot = new EquipmentSlotVM(eq, EquipmentIndex.Weapon2);
            Weapon4Slot = new EquipmentSlotVM(eq, EquipmentIndex.Weapon3);

            // refresh each so UI picks up values immediately
            HeadSlot.Refresh();   CloakSlot.Refresh();  BodySlot.Refresh();   GlovesSlot.Refresh();
            BootsSlot.Refresh();  MountSlot.Refresh();  HarnessSlot.Refresh();
            Weapon1Slot.Refresh(); Weapon2Slot.Refresh(); Weapon3Slot.Refresh(); Weapon4Slot.Refresh();
        }

        private Equipment GetPrimaryEquipment()
        {
            var list = Troop?.Equipments;
            return (list != null && list.Count > 0) ? list[0] : default;
        }
    }
}
