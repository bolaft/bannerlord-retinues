using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment
{
    /// <summary>
    /// ViewModel for equipment editor. Handles slot selection, unequip logic, and UI refresh.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentEditorVM(EditorScreenVM screen)
        : BaseEditor<EquipmentEditorVM>(screen)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string UnequipAllButtonText => L.S("unequip_all_button_text", "Unequip All");

        [DataSourceProperty]
        public string UnstageAllButtonText => L.S("unstage_all_button_text", "Reset Changes");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool CanUnequip
        {
            get
            {
                if (Screen.IsDefaultMode)
                    return false; // Only show in equipment mode
                if (SelectedTroop == null)
                    return false;
                if (SelectedTroop.Equipment.Items.Count == 0)
                    return false; // No equipment to unequip

                return true;
            }
        }

        [DataSourceProperty]
        public bool HasStagedChanges
        {
            get
            {
                if (SelectedTroop == null)
                    return false;

                if (HeadSlot.IsStaged)
                    return true;
                if (CapeSlot.IsStaged)
                    return true;
                if (BodySlot.IsStaged)
                    return true;
                if (GlovesSlot.IsStaged)
                    return true;
                if (LegSlot.IsStaged)
                    return true;
                if (HorseSlot.IsStaged)
                    return true;
                if (HorseHarnessSlot.IsStaged)
                    return true;
                if (WeaponItemBeginSlotSlot.IsStaged)
                    return true;
                if (Weapon1Slot.IsStaged)
                    return true;
                if (Weapon2Slot.IsStaged)
                    return true;
                if (Weapon3Slot.IsStaged)
                    return true;

                return false;
            }
        }

        /* ━━━━ Equipment Slots ━━━ */

        private EquipmentSlotVM _headSlot;

        [DataSourceProperty]
        public EquipmentSlotVM HeadSlot => _headSlot;

        private EquipmentSlotVM _capeSlot;

        [DataSourceProperty]
        public EquipmentSlotVM CapeSlot => _capeSlot;

        private EquipmentSlotVM _bodySlot;

        [DataSourceProperty]
        public EquipmentSlotVM BodySlot => _bodySlot;

        private EquipmentSlotVM _glovesSlot;

        [DataSourceProperty]
        public EquipmentSlotVM GlovesSlot => _glovesSlot;

        private EquipmentSlotVM _legSlot;

        [DataSourceProperty]
        public EquipmentSlotVM LegSlot => _legSlot;

        private EquipmentSlotVM _horseSlot;

        [DataSourceProperty]
        public EquipmentSlotVM HorseSlot => _horseSlot;

        private EquipmentSlotVM _horseHarnessSlot;

        [DataSourceProperty]
        public EquipmentSlotVM HorseHarnessSlot => _horseHarnessSlot;

        private EquipmentSlotVM _weaponItemBeginSlotSlot;

        [DataSourceProperty]
        public EquipmentSlotVM WeaponItemBeginSlotSlot => _weaponItemBeginSlotSlot;

        private EquipmentSlotVM _weapon1Slot;

        [DataSourceProperty]
        public EquipmentSlotVM Weapon1Slot => _weapon1Slot;

        private EquipmentSlotVM _weapon2Slot;

        [DataSourceProperty]
        public EquipmentSlotVM Weapon2Slot => _weapon2Slot;

        private EquipmentSlotVM _weapon3Slot;

        [DataSourceProperty]
        public EquipmentSlotVM Weapon3Slot => _weapon3Slot;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unequips all items from the selected troop and restocks them.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteUnequipAll()
        {
            if (!CanUnequip)
                return; // No-op if cannot unequip

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("unequip_all", "Unequip All"),
                    text: L.T(
                            "unequip_all_text",
                            "Unequip all items worn by {TROOP_NAME}?\n\nThey will be stocked for later use."
                        )
                        .SetTextVariable("TROOP_NAME", SelectedTroop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        EquipmentManager.UnequipAll(SelectedTroop);

                        // Refresh UI
                        Screen.Refresh();
                        Screen.EquipmentList.Refresh();
                        Refresh();
                    },
                    negativeAction: () => { }
                )
            );
        }

        /// <summary>
        /// Unstages all staged equipment changes for the selected troop.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteUnstageAll()
        {
            if (!HasStagedChanges)
                return; // No-op if nothing to unstage

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("unstage_all", "Unstage All"),
                    text: L.T(
                            "unstage_all_text",
                            "Revert all staged equipment changes for {TROOP_NAME}?"
                        )
                        .SetTextVariable("TROOP_NAME", SelectedTroop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        foreach (var slot in Slots)
                            slot.Unstage();

                        // Refresh UI
                        Screen.Refresh();
                        Screen.EquipmentList.Refresh();
                        Refresh();
                    },
                    negativeAction: () => { }
                )
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns all equipment slot view models.
        /// </summary>
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

        /// <summary>
        /// Returns the currently selected equipment slot.
        /// </summary>
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

        /// <summary>
        /// Selects the given equipment slot.
        /// </summary>
        public void Select(EquipmentSlotVM slot)
        {
            foreach (var r in Slots)
                r.IsSelected = ReferenceEquals(r, slot);
        }

        /// <summary>
        /// Refreshes all equipment slots and bindings.
        /// </summary>
        public void Refresh()
        {
            // Recreate all slot VMs
            CreateAllSlots();

            if (SelectedSlot is null)
                WeaponItemBeginSlotSlot.Select();

            OnPropertyChanged(nameof(HasStagedChanges));
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

        /// <summary>
        /// Creates all equipment slot view models.
        /// </summary>
        public void CreateAllSlots()
        {
            // Helper to create slot VMs
            EquipmentSlotVM CreateSlot(EquipmentIndex slot, string label) =>
                new(slot, label, SelectedTroop, this);

            _headSlot = CreateSlot(EquipmentIndex.Head, L.S("head_slot_text", "Head"));
            _capeSlot = CreateSlot(EquipmentIndex.Cape, L.S("cape_slot_text", "Cape"));
            _bodySlot = CreateSlot(EquipmentIndex.Body, L.S("body_slot_text", "Body"));
            _glovesSlot = CreateSlot(EquipmentIndex.Gloves, L.S("gloves_slot_text", "Gloves"));
            _legSlot = CreateSlot(EquipmentIndex.Leg, L.S("leg_slot_text", "Legs"));
            _horseSlot = CreateSlot(EquipmentIndex.Horse, L.S("horse_slot_text", "Horse"));
            _horseHarnessSlot = CreateSlot(
                EquipmentIndex.HorseHarness,
                L.S("horse_harness_slot_text", "Harness")
            );
            _weaponItemBeginSlotSlot = CreateSlot(
                EquipmentIndex.WeaponItemBeginSlot,
                L.S("weapon_1_slot_text", "Weapon 1")
            );
            _weapon1Slot = CreateSlot(
                EquipmentIndex.Weapon1,
                L.S("weapon_2_slot_text", "Weapon 2")
            );
            _weapon2Slot = CreateSlot(
                EquipmentIndex.Weapon2,
                L.S("weapon_3_slot_text", "Weapon 3")
            );
            _weapon3Slot = CreateSlot(
                EquipmentIndex.Weapon3,
                L.S("weapon_4_slot_text", "Weapon 4")
            );
        }
    }
}
