using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
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
    public sealed class EquipmentPanelVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly WCharacter Troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EquipmentPanelVM(WCharacter troop)
        {
            // Troop being edited
            Troop = troop;

            // Start focused on Battle set
            Index = 0;

            // Slot
            BuildSlots();

            // Select first slot by default
            Select(WeaponItemBeginSlotSlot);
        }

        private void BuildSlots()
        {
            // Indexes
            List<EquipmentIndex> indexes =
            [
                EquipmentIndex.Head,
                EquipmentIndex.Cape,
                EquipmentIndex.Body,
                EquipmentIndex.Gloves,
                EquipmentIndex.Leg,
                EquipmentIndex.Horse,
                EquipmentIndex.HorseHarness,
                EquipmentIndex.WeaponItemBeginSlot,
                EquipmentIndex.Weapon1,
                EquipmentIndex.Weapon2,
                EquipmentIndex.Weapon3,
            ];

            // Labels
            List<string> labels =
            [
                L.S("head_slot_text", "Head"),
                L.S("cape_slot_text", "Cape"),
                L.S("body_slot_text", "Body"),
                L.S("gloves_slot_text", "Gloves"),
                L.S("leg_slot_text", "Legs"),
                L.S("horse_slot_text", "Horse"),
                L.S("horse_harness_slot_text", "Harness"),
                L.S("weapon_1_slot_text", "Weapon 1"),
                L.S("weapon_2_slot_text", "Weapon 2"),
                L.S("weapon_3_slot_text", "Weapon 3"),
                L.S("weapon_4_slot_text", "Weapon 4"),
            ];

            // Assignments
            List<Action<EquipmentSlotVM>> slotAssignments =
            [
                vm => HeadSlot = vm,
                vm => CapeSlot = vm,
                vm => BodySlot = vm,
                vm => GlovesSlot = vm,
                vm => LegSlot = vm,
                vm => HorseSlot = vm,
                vm => HorseHarnessSlot = vm,
                vm => WeaponItemBeginSlotSlot = vm,
                vm => Weapon1Slot = vm,
                vm => Weapon2Slot = vm,
                vm => Weapon3Slot = vm,
            ];

            // Definitions
            var definitions = indexes
                .Zip(labels, (slot, label) => (slot, label))
                .Zip(slotAssignments, (tuple, assign) => (tuple.slot, tuple.label, assign));

            // Create and assign all slot VMs
            foreach (var (slot, label, assign) in definitions)
                assign(new(Troop, Equipment, slot, label));

            // Refresh slot bindings
            foreach (var slot in Slots)
                OnPropertyChanged(nameof(slot));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WEquipment Equipment => Troop.Loadout.Equipments[Index];

        public EquipmentCategory EquipmentCategory
        {
            get
            {
                if (Index == 0)
                    return EquipmentCategory.Battle;
                else if (Index == 1)
                    return EquipmentCategory.Civilian;
                else
                    return EquipmentCategory.Alternate;
            }
        }

        private int _index;
        public int Index
        {
            get => _index;
            set
            {
                if (_index == value)
                    return;

                if (value < 0)
                    value = 0;

                if (value >= Troop.Loadout.Equipments.Count)
                    value = Troop.Loadout.Equipments.Count - 1;

                // Refresh slots for new equipment set
                BuildSlots();

                // Refresh all bindings
                OnPropertyChanged(nameof(CanUnequip));
                OnPropertyChanged(nameof(CanUnstage));
                OnPropertyChanged(nameof(EquipmentName));
                OnPropertyChanged(nameof(CanSelectPrevSet));
                OnPropertyChanged(nameof(CanSelectNextSet));
                OnPropertyChanged(nameof(CanRemoveSet));

                // Refresh 3D model
                OnPropertyChanged(nameof(Editor.Model));

                _index = value;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━ Unequip / Unstage ━━ */

        [DataSourceProperty]
        public bool CanUnequip => Equipment.Items.Count() == 0;

        [DataSourceProperty]
        public bool CanUnstage => Slots.Any(s => s.IsStaged);

        [DataSourceProperty]
        public string UnequipAllButtonText => L.S("unequip_all_button_text", "Unequip All");

        [DataSourceProperty]
        public string UnstageAllButtonText => L.S("unstage_all_button_text", "Reset Changes");

        /* ━━━━ Equipment Sets ━━━━ */

        [DataSourceProperty]
        public string EquipmentName
        {
            get
            {
                return EquipmentCategory switch
                {
                    EquipmentCategory.Battle => L.S("set_battle", "Battle"),
                    EquipmentCategory.Civilian => L.S("set_civilian", "Civilian"),
                    _ => L.T("set_alt_n", "Alt {N}").SetTextVariable("N", Index + 1).ToString(),
                };
            }
        }

        [DataSourceProperty]
        public bool CanSelectPrevSet => Index > 0;

        [DataSourceProperty]
        public bool CanSelectNextSet => Index < Troop.Loadout.Equipments.Count - 1;

        [DataSourceProperty]
        public bool CanRemoveSet => EquipmentCategory == EquipmentCategory.Alternate;

        /* ━━━━ Equipment Slots ━━━ */

        [DataSourceProperty]
        public EquipmentSlotVM HeadSlot;

        [DataSourceProperty]
        public EquipmentSlotVM CapeSlot;

        [DataSourceProperty]
        public EquipmentSlotVM BodySlot;

        [DataSourceProperty]
        public EquipmentSlotVM GlovesSlot;

        [DataSourceProperty]
        public EquipmentSlotVM LegSlot;

        [DataSourceProperty]
        public EquipmentSlotVM HorseSlot;

        [DataSourceProperty]
        public EquipmentSlotVM HorseHarnessSlot;

        [DataSourceProperty]
        public EquipmentSlotVM WeaponItemBeginSlotSlot;

        [DataSourceProperty]
        public EquipmentSlotVM Weapon1Slot;

        [DataSourceProperty]
        public EquipmentSlotVM Weapon2Slot;

        [DataSourceProperty]
        public EquipmentSlotVM Weapon3Slot;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unequips all items from the selected troop and restocks them.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteUnequipAll()
        {
            if (CanUnequip == false)
                return; // No-op if cannot unequip

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("unequip_all", "Unequip All"),
                    text: L.T(
                            "unequip_all_text",
                            "Unequip all items worn by {TROOP_NAME}?\n\nThey will be stocked for later use."
                        )
                        .SetTextVariable("TROOP_NAME", Troop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        EquipmentManager.UnequipAll(Troop, EquipmentCategory, Index);
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
            if (CanUnstage == false)
                return; // No-op if nothing to unstage

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("unstage_all", "Unstage All"),
                    text: L.T(
                            "unstage_all_text",
                            "Revert all staged equipment changes for {TROOP_NAME}?"
                        )
                        .SetTextVariable("TROOP_NAME", Troop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        foreach (var slot in Slots)
                            slot.Unstage();
                    },
                    negativeAction: () => { }
                )
            );
        }

        [DataSourceMethod]
        public void ExecutePrevSet()
        {
            if (CanSelectPrevSet == false)
                return;

            Index -= 1;
        }

        [DataSourceMethod]
        public void ExecuteNextSet()
        {
            if (CanSelectNextSet == false)
                return;

            Index += 1;
        }

        [DataSourceMethod]
        public void ExecuteCreateSet()
        {
            // Create a brand-new EMPTY alternate set (independent, no free items)
            var alternates = Troop.Loadout.Alternates;
            alternates.Add(WEquipment.FromCode(null));
            Troop.Loadout.Alternates = alternates; // re-assign to trigger any bindings

            // Focus the newly created set
            Index = Troop.Loadout.Equipments.Count - 1;
        }

        [DataSourceMethod]
        public void ExecuteRemoveSet()
        {
            if (CanRemoveSet == false)
                return;

            InformationManager.ShowInquiry(
                new InquiryData(
                    L.S("remove_set_title", "Remove Set"),
                    L.T(
                            "remove_set_text",
                            "Remove {EQUIPMENT} for {TROOP}?\nAll staged changes will be cleared and items will be unequipped and stocked."
                        )
                        .SetTextVariable("EQUIPMENT", EquipmentName)
                        .SetTextVariable("TROOP", Troop.Name)
                        .ToString(),
                    true,
                    true,
                    L.S("confirm", "Confirm"),
                    L.S("cancel", "Cancel"),
                    () =>
                    {
                        // Clear all staged changes
                        foreach (var s in Slots)
                            s.Unstage();

                        // Unequip all items
                        EquipmentManager.UnequipAll(Troop, EquipmentCategory, Index);

                        var alternates = Troop.Loadout.Alternates;
                        alternates.RemoveAt(Index - 2); // -2 because alternates start at index 2
                        Troop.Loadout.Alternates = alternates; // re-assign to trigger any bindings

                        Index = 0; // Focus back to Battle set
                    },
                    () => { }
                )
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Slots                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the currently selected equipment slot.
        /// </summary>
        public EquipmentSlotVM Selection
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
        /// Selects the given equipment slot.
        /// </summary>
        public void Select(EquipmentSlotVM slot)
        {
            foreach (var r in Slots)
                r.IsSelected = ReferenceEquals(r, slot);
        }
    }
}
