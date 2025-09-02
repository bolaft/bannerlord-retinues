using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.PlayerServices;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;
using CustomClanTroops.Config;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentEditorVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        private CharacterWrapper Troop => _owner.SelectedTroop;

        private EquipmentListVM EquipmentList => _owner.EquipmentList;

        public EquipmentSlotVM SelectedSlot { get; set; }

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

            RefreshSlots();
        }

        public void RefreshSlots()
        {
            HeadSlot.Refresh();
            CloakSlot.Refresh();
            BodySlot.Refresh();
            GlovesSlot.Refresh();
            BootsSlot.Refresh();
            MountSlot.Refresh();
            HarnessSlot.Refresh();
            Weapon1Slot.Refresh();
            Weapon2Slot.Refresh();
            Weapon3Slot.Refresh();
            Weapon4Slot.Refresh();
        }

        public void HandleSlotSelected(EquipmentSlotVM slot)
        {
            SelectedSlot = slot;
            RefreshSlots();

            if (EquipmentList == null) return;

            EquipmentList.Refresh();
        }

        public void HandleRowSelected(EquipmentRowVM row)
        {
            Log.Debug($"EquipmentEditorVM.HandleRowSelected(): row = {row?.Equipment?.StringId}");

            if (row == null) return;

            foreach (var r in EquipmentList.Equipments) r.IsSelected = ReferenceEquals(r, row);
        }

        [DataSourceProperty] public EquipmentSlotVM HeadSlot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM CloakSlot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM BodySlot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM GlovesSlot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM BootsSlot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM MountSlot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM HarnessSlot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM Weapon1Slot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM Weapon2Slot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM Weapon3Slot { get; private set; }
        [DataSourceProperty] public EquipmentSlotVM Weapon4Slot { get; private set; }

        private void Build()
        {
            var eq = GetPrimaryEquipment();

            bool mountsAllowed = true;

            // No mounts for tier 1 troops
            if (ModConfig.DisallowMountsForTier1)
                mountsAllowed = Troop?.Tier > 1;

            HeadSlot = new EquipmentSlotVM(eq, EquipmentIndex.Head, true, this);
            CloakSlot = new EquipmentSlotVM(eq, EquipmentIndex.Cape, true, this);
            BodySlot = new EquipmentSlotVM(eq, EquipmentIndex.Body, true, this);
            GlovesSlot = new EquipmentSlotVM(eq, EquipmentIndex.Gloves, true, this);
            BootsSlot = new EquipmentSlotVM(eq, EquipmentIndex.Leg, true, this);
            MountSlot = new EquipmentSlotVM(eq, EquipmentIndex.Horse, mountsAllowed, this);
            HarnessSlot = new EquipmentSlotVM(eq, EquipmentIndex.HorseHarness, mountsAllowed, this);

            Weapon1Slot = new EquipmentSlotVM(eq, EquipmentIndex.WeaponItemBeginSlot, true, this);
            Weapon2Slot = new EquipmentSlotVM(eq, EquipmentIndex.Weapon1, true, this);
            Weapon3Slot = new EquipmentSlotVM(eq, EquipmentIndex.Weapon2, true, this);
            Weapon4Slot = new EquipmentSlotVM(eq, EquipmentIndex.Weapon3, true, this);

            // Set default selected slot
            HandleSlotSelected(Weapon1Slot);

            // refresh each so UI picks up values immediately
            HeadSlot.Refresh(); CloakSlot.Refresh(); BodySlot.Refresh(); GlovesSlot.Refresh();
            BootsSlot.Refresh(); MountSlot.Refresh(); HarnessSlot.Refresh();
            Weapon1Slot.Refresh(); Weapon2Slot.Refresh(); Weapon3Slot.Refresh(); Weapon4Slot.Refresh();

        }

        private Equipment GetPrimaryEquipment()
        {
            // Get only the first variant
            var list = Troop?.Equipments;
            return (list != null && list.Count > 0) ? list[0] : default;
        }
    }
}
