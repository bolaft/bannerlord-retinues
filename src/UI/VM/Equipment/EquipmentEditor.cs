using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Equipment
{
    public sealed class EquipmentEditorVM(ClanScreen screen) : BaseEditor<EquipmentEditorVM>(screen), IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private EquipmentSlotVM _headSlot;
        private EquipmentSlotVM _capeSlot;
        private EquipmentSlotVM _bodySlot;
        private EquipmentSlotVM _glovesSlot;
        private EquipmentSlotVM _legSlot;
        private EquipmentSlotVM _horseSlot;
        private EquipmentSlotVM _horseHarnessSlot;
        private EquipmentSlotVM _weaponItemBeginSlotSlot;
        private EquipmentSlotVM _weapon1Slot;
        private EquipmentSlotVM _weapon2Slot;
        private EquipmentSlotVM _weapon3Slot;

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public bool CanUnequip
        {
            get
            {
                if (Screen.IsDefaultMode)
                    return false; // Only show in equipment mode
                if (SelectedTroop.Equipment.Items.Count == 0)
                    return false; // No equipment to unequip

                return true;
            }
        }

        [DataSourceProperty]
        public EquipmentSlotVM HeadSlot => _headSlot;

        [DataSourceProperty]
        public EquipmentSlotVM CapeSlot => _capeSlot;

        [DataSourceProperty]
        public EquipmentSlotVM BodySlot => _bodySlot;

        [DataSourceProperty]
        public EquipmentSlotVM GlovesSlot => _glovesSlot;

        [DataSourceProperty]
        public EquipmentSlotVM LegSlot => _legSlot;

        [DataSourceProperty]
        public EquipmentSlotVM HorseSlot => _horseSlot;

        [DataSourceProperty]
        public EquipmentSlotVM HorseHarnessSlot => _horseHarnessSlot;

        [DataSourceProperty]
        public EquipmentSlotVM WeaponItemBeginSlotSlot => _weaponItemBeginSlotSlot;

        [DataSourceProperty]
        public EquipmentSlotVM Weapon1Slot => _weapon1Slot;

        [DataSourceProperty]
        public EquipmentSlotVM Weapon2Slot => _weapon2Slot;

        [DataSourceProperty]
        public EquipmentSlotVM Weapon3Slot => _weapon3Slot;

        // =========================================================================
        // Action Bindings
        // =========================================================================

        [DataSourceMethod]
        public void ExecuteUnequipAll()
        {
            if (!CanUnequip)
                return; // No-op if cannot unequip

            InformationManager.ShowInquiry(new InquiryData(
                titleText: "Unequip All",
                text: $"Unequip all items worn by {SelectedTroop.Name}?\n\nThey will be stocked for later use.",
                isAffirmativeOptionShown: true, isNegativeOptionShown: true,
                affirmativeText: "Confirm", negativeText: "Cancel",
                affirmativeAction: () =>
                {
                    // Unequip all items
                    foreach (var item in SelectedTroop.UnequipAll())
                    {
                        // If the item had a value, restock it
                        if (item != null && item.Value > 0)
                            item.Stock();
                    }

                    // Refresh UI
                    Screen.Refresh();
                    Screen.EquipmentList.Refresh();
                    Refresh();
                },
                negativeAction: () => { }
            ));
        }

        // =========================================================================
        // Public API
        // =========================================================================

        public IEnumerable<EquipmentSlotVM> Slots
        {
            get
            {
                yield return HeadSlot;
                yield return CapeSlot;
                yield return BodySlot;
                yield return GlovesSlot;
                yield return LegSlot;

                yield return WeaponItemBeginSlotSlot;
                yield return Weapon1Slot;
                yield return Weapon2Slot;
                yield return Weapon3Slot;

                yield return HorseSlot;
                yield return HorseHarnessSlot;
            }
        }

        public EquipmentSlotVM SelectedSlot
        {
            get
            {
                foreach (var r in Slots)
                {
                    if (r.IsSelected)
                        return r;
                }

                return null;
            }
        }

        public void Select(EquipmentSlotVM slot)
        {
            foreach (var r in Slots)
                r.IsSelected = ReferenceEquals(r, slot);
        }

        public void Refresh()
        {
            Log.Debug("Refreshing.");

            // Recreate all slot VMs
            CreateAllSlots();

            if (SelectedSlot is null)
                WeaponItemBeginSlotSlot.Select();

            OnPropertyChanged(nameof(CanUnequip));
            OnPropertyChanged(nameof(HeadSlot));
            OnPropertyChanged(nameof(CapeSlot));
            OnPropertyChanged(nameof(BodySlot));
            OnPropertyChanged(nameof(GlovesSlot));
            OnPropertyChanged(nameof(LegSlot));
            OnPropertyChanged(nameof(HorseSlot));
            OnPropertyChanged(nameof(HorseHarnessSlot));
            OnPropertyChanged(nameof(WeaponItemBeginSlotSlot));
            OnPropertyChanged(nameof(Weapon1Slot));
            OnPropertyChanged(nameof(Weapon2Slot));
            OnPropertyChanged(nameof(Weapon3Slot));
        }

        // =========================================================================
        // Internals
        // =========================================================================

        public void CreateAllSlots()
        {
            // Helper to create slot VMs
            EquipmentSlotVM CreateSlot(EquipmentIndex slot) => new(slot, SelectedTroop, this);

            _headSlot = CreateSlot(EquipmentIndex.Head);
            _capeSlot = CreateSlot(EquipmentIndex.Cape);
            _bodySlot = CreateSlot(EquipmentIndex.Body);
            _glovesSlot = CreateSlot(EquipmentIndex.Gloves);
            _legSlot = CreateSlot(EquipmentIndex.Leg);
            _horseSlot = CreateSlot(EquipmentIndex.Horse);
            _horseHarnessSlot = CreateSlot(EquipmentIndex.HorseHarness);
            _weaponItemBeginSlotSlot = CreateSlot(EquipmentIndex.WeaponItemBeginSlot);
            _weapon1Slot = CreateSlot(EquipmentIndex.Weapon1);
            _weapon2Slot = CreateSlot(EquipmentIndex.Weapon2);
            _weapon3Slot = CreateSlot(EquipmentIndex.Weapon3);
        }
    }
}