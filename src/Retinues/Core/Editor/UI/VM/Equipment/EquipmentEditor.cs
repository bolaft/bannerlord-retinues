using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Equipment
{
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
