using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment.List;
using Retinues.GUI.Editor.VM.Equipment.Panel;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment
{
    /// <summary>
    /// ViewModel for the equipment editor screen, containing slots and list.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentScreenVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Faction] =
                [
                    nameof(Weapon1Slot),
                    nameof(Weapon2Slot),
                    nameof(Weapon3Slot),
                    nameof(Weapon4Slot),
                    nameof(HeadSlot),
                    nameof(CapeSlot),
                    nameof(BodySlot),
                    nameof(GlovesSlot),
                    nameof(LegSlot),
                    nameof(HorseSlot),
                    nameof(HorseHarnessSlot),
                    nameof(CanUnstage),
                    nameof(CanUnequip),
                    nameof(EquipmentName),
                    nameof(CanSelectPrevSet),
                    nameof(CanSelectNextSet),
                    nameof(CanRemoveSet),
                ],
                [UIEvent.Equipment] =
                [
                    nameof(Weapon1Slot),
                    nameof(Weapon2Slot),
                    nameof(Weapon3Slot),
                    nameof(Weapon4Slot),
                    nameof(HeadSlot),
                    nameof(CapeSlot),
                    nameof(BodySlot),
                    nameof(GlovesSlot),
                    nameof(LegSlot),
                    nameof(HorseSlot),
                    nameof(HorseHarnessSlot),
                    nameof(CanUnstage),
                    nameof(CanUnequip),
                    nameof(EquipmentName),
                    nameof(CanSelectPrevSet),
                    nameof(CanSelectNextSet),
                    nameof(CanRemoveSet),
                ],
                [UIEvent.Equip] =
                [
                    nameof(CanUnstage),
                    nameof(CanUnequip),
                ],
                [UIEvent.Slot] = [nameof(CanUnstage)],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public EquipmentListVM EquipmentList { get; set; } = new();

        [DataSourceProperty]
        public EquipmentSlotVM Weapon1Slot { get; set; } = new(EquipmentIndex.Weapon0);

        [DataSourceProperty]
        public EquipmentSlotVM Weapon2Slot { get; set; } = new(EquipmentIndex.Weapon1);

        [DataSourceProperty]
        public EquipmentSlotVM Weapon3Slot { get; set; } = new(EquipmentIndex.Weapon2);

        [DataSourceProperty]
        public EquipmentSlotVM Weapon4Slot { get; set; } = new(EquipmentIndex.Weapon3);

        [DataSourceProperty]
        public EquipmentSlotVM HeadSlot { get; set; } = new(EquipmentIndex.Head);

        [DataSourceProperty]
        public EquipmentSlotVM CapeSlot { get; set; } = new(EquipmentIndex.Cape);

        [DataSourceProperty]
        public EquipmentSlotVM BodySlot { get; set; } = new(EquipmentIndex.Body);

        [DataSourceProperty]
        public EquipmentSlotVM GlovesSlot { get; set; } = new(EquipmentIndex.Gloves);

        [DataSourceProperty]
        public EquipmentSlotVM LegSlot { get; set; } = new(EquipmentIndex.Leg);

        [DataSourceProperty]
        public EquipmentSlotVM HorseSlot { get; set; } = new(EquipmentIndex.Horse);

        [DataSourceProperty]
        public EquipmentSlotVM HorseHarnessSlot { get; set; } = new(EquipmentIndex.HorseHarness);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private IEnumerable<EquipmentSlotVM> EquipmentSlots =>
            [
                Weapon1Slot,
                Weapon2Slot,
                Weapon3Slot,
                Weapon4Slot,
                HeadSlot,
                CapeSlot,
                BodySlot,
                GlovesSlot,
                LegSlot,
                HorseSlot,
                HorseHarnessSlot,
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━ Unequip / Unstage ━━ */

        [DataSourceProperty]
        public bool CanUnstage => EquipmentSlots.Any(s => s.IsStaged);

        [DataSourceProperty]
        public bool CanUnequip => State.Equipment?.Items.Any() == true;

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
                return State.Equipment?.Category switch
                {
                    EquipmentCategory.Battle => L.S("set_battle", "Battle"),
                    EquipmentCategory.Civilian => L.S("set_civilian", "Civilian"),
                    _ => L.T("set_alt_n", "Alt {N}")
                        .SetTextVariable("N", State.Equipment?.Index + -1 ?? '?')
                        .ToString(),
                };
            }
        }

        [DataSourceProperty]
        public bool CanSelectPrevSet => (State.Equipment?.Index ?? 0) > 0;

        [DataSourceProperty]
        public bool CanSelectNextSet =>
            (State.Equipment?.Index ?? 0) < ((State.Troop?.Loadout.Equipments.Count ?? 1) - 1);

        [DataSourceProperty]
        public bool CanRemoveSet => State.Equipment?.Category == EquipmentCategory.Alternate;

        [DataSourceProperty]
        public bool CanCreateSet => ModuleChecker.GetModule("Shokuho") == null; // Disable if Shokuho is present

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        /// <summary>
        /// Unequip all items from the current equipment set (with confirmation).
        /// </summary>
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
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        State.Troop.UnequipAll(State.Equipment.Index, stock: true);
                        State.UpdateEquipData();
                    },
                    negativeAction: () => { }
                )
            );
        }

        [DataSourceMethod]
        /// <summary>
        /// Revert all staged equipment changes for the current equipment set (with confirmation).
        /// </summary>
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
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        State.Troop.UnstageAll(State.Equipment.Index, true);
                        State.UpdateEquipData();
                    },
                    negativeAction: () => { }
                )
            );
        }

        [DataSourceMethod]
        /// <summary>
        /// Select the previous equipment set.
        /// </summary>
        public void ExecutePrevSet() =>
            State.UpdateEquipment(State.Troop.Loadout.Equipments[State.Equipment.Index - 1]);

        [DataSourceMethod]
        /// <summary>
        /// Select the next equipment set.
        /// </summary>
        public void ExecuteNextSet() =>
            State.UpdateEquipment(State.Troop.Loadout.Equipments[State.Equipment.Index + 1]);

        [DataSourceMethod]
        /// <summary>
        /// Create a new alternate equipment set and select it.
        /// </summary>
        public void ExecuteCreateSet()
        {
            State.UpdateEquipment(State.Troop?.Loadout.CreateAlternate());
        }

        [DataSourceMethod]
        /// <summary>
        /// Remove the current alternate equipment set (with confirmation).
        /// </summary>
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
                        .SetTextVariable("TROOP", State.Troop.Name)
                        .ToString(),
                    true,
                    true,
                    L.S("confirm", "Confirm"),
                    L.S("cancel", "Cancel"),
                    () =>
                    {
                        // Clear staged changes
                        State.Troop.UnstageAll(State.Equipment?.Index ?? 0, stock: true);

                        // Unequip all items
                        State.Troop.UnequipAll(State.Equipment?.Index ?? 0, stock: true);

                        // Remove the set
                        State.Troop.Loadout.RemoveAlternate(State.Equipment);

                        // Select the battle set after removal
                        State.UpdateEquipment(State.Troop.Loadout.Battle);
                    },
                    () => { }
                )
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Show the equipment screen and its child components.
        /// </summary>
        public override void Show()
        {
            base.Show();
            EquipmentList.Show();
            foreach (var slot in EquipmentSlots)
                slot.Show();
        }

        /// <summary>
        /// Hide the equipment screen and its child components.
        /// </summary>
        public override void Hide()
        {
            foreach (var slot in EquipmentSlots)
                slot.Hide();
            EquipmentList.Hide();
            base.Hide();
        }
    }
}
