using System;
using System.Collections.Generic;
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
    public sealed class EquipmentEditorVM(EditorScreenVM screen)
        : BaseEditor<EquipmentEditorVM>(screen)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WLoadout.Category LoadoutCategory = WLoadout.Category.Battle;
        public int LoadoutIndex = 0;
        public WEquipment Equipment => SelectedTroop?.Loadout.Get(LoadoutCategory, LoadoutIndex);

        /// <summary>
        /// Selects the given loadout category and index.
        /// </summary>
        public void SelectEquipment(
            WLoadout.Category category = WLoadout.Category.Battle,
            int index = 0
        )
        {
            if (SelectedTroop == null)
            {
                LoadoutCategory = WLoadout.Category.Battle;
                LoadoutIndex = 0;
                Refresh();
                return;
            }

            var altCount = SelectedTroop.Loadout.Alternates.Count;

            if (category != WLoadout.Category.Alternate)
            {
                LoadoutCategory = category;
                LoadoutIndex = 0; // Battle/Civilian always index 0
            }
            else
            {
                // Clamp to existing alternates
                if (altCount == 0)
                {
                    LoadoutCategory = WLoadout.Category.Civilian;
                    LoadoutIndex = 0;
                    Refresh();
                    return;
                }
                LoadoutCategory = WLoadout.Category.Alternate;
                LoadoutIndex = Math.Max(0, Math.Min(index, altCount - 1));
            }

            Refresh();
            Screen.Refresh();

            Log.Info(
                $"EquipmentEditor: selected loadout {LoadoutCategory} [{LoadoutIndex}] (civilian: {Equipment.IsCivilian})."
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string UnequipAllButtonText => L.S("unequip_all_button_text", "Unequip All");

        [DataSourceProperty]
        public string UnstageAllButtonText => L.S("unstage_all_button_text", "Reset Changes");

        [DataSourceProperty]
        public string CurrentSetText
        {
            get
            {
                if (LoadoutCategory == WLoadout.Category.Alternate)
                    return L.T("set_alt_n", "Alternate {N}")
                        .SetTextVariable("N", LoadoutIndex + 1)
                        .ToString();
                return LoadoutCategory == WLoadout.Category.Civilian
                    ? L.S("set_civilian", "Civilian")
                    : L.S("set_battle", "Battle");
            }
        }

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool CanSelectPreviousSet
        {
            get
            {
                if (SelectedTroop == null)
                    return false;
                // Only Battle has no previous; Civilian always has Battle; Alternates have previous (Civilian or Alt-1)
                return LoadoutCategory != WLoadout.Category.Battle;
            }
        }

        [DataSourceProperty]
        public bool CanSelectNextSet
        {
            get
            {
                if (SelectedTroop == null)
                    return false;
                var altCount = SelectedTroop.Loadout.Alternates.Count;

                switch (LoadoutCategory)
                {
                    case WLoadout.Category.Battle:
                        return true; // → Civilian
                    case WLoadout.Category.Civilian:
                        return altCount > 0; // → Alt 0 if any exist
                    case WLoadout.Category.Alternate:
                        return LoadoutIndex < altCount - 1; // → next Alt
                    default:
                        return false;
                }
            }
        }

        [DataSourceProperty]
        public bool CanUnequip
        {
            get
            {
                if (Screen.IsDefaultMode)
                    return false; // Only show in equipment mode
                if (SelectedTroop == null)
                    return false;
                if (Equipment == null)
                    return false;
                if (Equipment.Items.Count == 0)
                    return false; // No equipment to unequip

                return true;
            }
        }

        [DataSourceProperty]
        public bool CanRemoveSet =>
            SelectedTroop != null
            && LoadoutCategory == WLoadout.Category.Alternate
            && LoadoutIndex >= 0
            && LoadoutIndex < SelectedTroop.Loadout.Alternates.Count;

        [DataSourceProperty]
        public bool HasStagedChanges
        {
            get
            {
                if (SelectedTroop == null)
                    return false;

                foreach (var slot in Slots)
                    if (slot.IsStaged)
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
                        EquipmentManager.UnequipAll(SelectedTroop, LoadoutCategory, LoadoutIndex);

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

        [DataSourceMethod]
        public void ExecutePreviousSet()
        {
            if (SelectedTroop == null)
                return;

            switch (LoadoutCategory)
            {
                case WLoadout.Category.Battle:
                    return; // no previous
                case WLoadout.Category.Civilian:
                    SelectEquipment(WLoadout.Category.Battle);
                    return;
                case WLoadout.Category.Alternate:
                    if (LoadoutIndex > 0)
                        SelectEquipment(WLoadout.Category.Alternate, LoadoutIndex - 1);
                    else
                        SelectEquipment(WLoadout.Category.Civilian);
                    return;
            }
        }

        [DataSourceMethod]
        public void ExecuteNextSet()
        {
            if (SelectedTroop == null)
                return;

            switch (LoadoutCategory)
            {
                case WLoadout.Category.Battle:
                    SelectEquipment(WLoadout.Category.Civilian);
                    return;

                case WLoadout.Category.Civilian:
                    if (SelectedTroop.Loadout.Alternates.Count > 0)
                        SelectEquipment(WLoadout.Category.Alternate, 0);
                    return;

                case WLoadout.Category.Alternate:
                    if (LoadoutIndex < SelectedTroop.Loadout.Alternates.Count - 1)
                        SelectEquipment(WLoadout.Category.Alternate, LoadoutIndex + 1);
                    return;
            }
        }

        [DataSourceMethod]
        public void ExecuteCreateSet()
        {
            if (SelectedTroop == null)
                return;

            // Create a brand-new EMPTY alternate set (independent, no free items)
            var alternates = SelectedTroop.Loadout.Alternates;
            alternates.Add(WEquipment.FromCode(null));
            SelectedTroop.Loadout.Alternates = alternates; // re-assign to trigger any bindings

            // Focus the newly created set
            var newIndex = SelectedTroop.Loadout.Alternates.Count - 1;
            SelectEquipment(WLoadout.Category.Alternate, newIndex);
        }

        [DataSourceMethod]
        public void ExecuteRemoveSet()
        {
            if (!CanRemoveSet)
                return;

            InformationManager.ShowInquiry(
                new InquiryData(
                    L.S("remove_set_title", "Remove Set"),
                    L.T(
                            "remove_set_text",
                            "Remove {SET} for {TROOP_NAME}?\nAll staged changes will be cleared and items will be unequipped and stocked."
                        )
                        .SetTextVariable("SET", CurrentSetText)
                        .SetTextVariable("TROOP_NAME", SelectedTroop.Name)
                        .ToString(),
                    true,
                    true,
                    L.S("confirm", "Confirm"),
                    L.S("cancel", "Cancel"),
                    () =>
                    {
                        foreach (var s in Slots)
                            s.Unstage();
                        EquipmentManager.UnequipAll(SelectedTroop, LoadoutCategory, LoadoutIndex);

                        var alts = SelectedTroop.Loadout.Alternates;
                        alts.RemoveAt(LoadoutIndex);
                        SelectedTroop.Loadout.Alternates = alts;

                        var altCount = SelectedTroop.Loadout.Alternates.Count;
                        if (altCount > 0)
                            SelectEquipment(
                                WLoadout.Category.Alternate,
                                Math.Min(LoadoutIndex, altCount - 1)
                            );
                        else
                            SelectEquipment(WLoadout.Category.Civilian);
                    },
                    () => { }
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
            OnPropertyChanged(nameof(CurrentSetText));
            OnPropertyChanged(nameof(CanSelectPreviousSet));
            OnPropertyChanged(nameof(CanSelectNextSet));
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
