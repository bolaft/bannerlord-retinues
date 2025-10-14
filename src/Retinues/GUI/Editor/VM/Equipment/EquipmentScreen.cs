using System;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment.List;
using Retinues.GUI.Editor.VM.Equipment.Panel;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment
{
    [SafeClass]
    public sealed class EquipmentScreenVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WEquipment _equipment = Editor.TroopScreen.TroopList.Selection.Troop.Loadout.Battle;
        public WEquipment Equipment
        {
            get => _equipment;
            set
            {
                if (_equipment == value || value == null)
                    return;
                _equipment = value;

                // New panel
                EquipmentPanel = new EquipmentPanelVM();
                OnPropertyChanged(nameof(EquipmentPanel));

                // List refresh
                OnPropertyChanged(nameof(EquipmentList));

                // Model refresh
                OnPropertyChanged(nameof(Editor.Model));

                // Update bindings
                RefreshOnEquipmentChange();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private EquipmentPanelVM _equipmentPanel = new();

        [DataSourceProperty]
        public EquipmentPanelVM EquipmentPanel
        {
            get => _equipmentPanel;
            set
            {
                if (_equipmentPanel == value)
                    return;
                _equipmentPanel = value;
                OnPropertyChanged(nameof(EquipmentPanel));
            }
        }

        private EquipmentListVM _equipmentList = new();

        [DataSourceProperty]
        public EquipmentListVM EquipmentList
        {
            get => _equipmentList;
            set
            {
                if (_equipmentList == value)
                    return;
                _equipmentList = value;
                OnPropertyChanged(nameof(EquipmentList));
            }
        }

        /* ━━━ Unequip / Unstage ━━ */

        [DataSourceProperty]
        public bool CanUnequip => SelectedEquipment.Items.Count() > 0;

        [DataSourceProperty]
        public bool CanUnstage => EquipmentPanel.Slots.Any(s => s.IsStaged);

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
                return SelectedEquipment.Category switch
                {
                    EquipmentCategory.Battle => L.S("set_battle", "Battle"),
                    EquipmentCategory.Civilian => L.S("set_civilian", "Civilian"),
                    _ => L.T("set_alt_n", "Alt {N}")
                        .SetTextVariable(
                            "N",
                            SelectedEquipment.Loadout.Alternates.IndexOf(SelectedEquipment) + 1
                        )
                        .ToString(),
                };
            }
        }

        [DataSourceProperty]
        public bool CanSelectPrevSet => SelectedEquipment.Index > 0;

        [DataSourceProperty]
        public bool CanSelectNextSet =>
            SelectedEquipment.Index < SelectedTroop.Loadout.Equipments.Count - 1;

        [DataSourceProperty]
        public bool CanRemoveSet => SelectedEquipment.Category == EquipmentCategory.Alternate;

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
                        .SetTextVariable("TROOP_NAME", SelectedTroop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        SelectedTroop.UnequipAll(SelectedEquipment.Index, stock: true);
                        RefreshOnEquipmentChange();
                        OnPropertyChanged(nameof(EquipmentList));
                        // also refresh slot visuals:
                        foreach (var s in EquipmentPanel.Slots)
                        {
                            s.OnPropertyChanged(nameof(s.ItemText));
                            s.OnPropertyChanged(nameof(s.IsStaged));
#if BL13
                            s.OnPropertyChanged(nameof(s.ImageId));
                            s.OnPropertyChanged(nameof(s.ImageAdditionalArgs));
                            s.OnPropertyChanged(nameof(s.ImageTextureProviderName));
#else
                            s.OnPropertyChanged(nameof(s.ImageId));
                            s.OnPropertyChanged(nameof(s.ImageAdditionalArgs));
                            s.OnPropertyChanged(nameof(s.ImageTypeCode));
#endif
                        }
                        OnPropertyChanged(nameof(Editor.Model));
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
                        .SetTextVariable("TROOP_NAME", SelectedTroop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        foreach (var slot in EquipmentPanel.Slots)
                            slot.Unstage();
                        RefreshOnEquipmentChange();
                        OnPropertyChanged(nameof(EquipmentList));
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

            Equipment = Equipment.Loadout.Equipments[SelectedEquipment.Index - 1];
        }

        [DataSourceMethod]
        public void ExecuteNextSet()
        {
            if (CanSelectNextSet == false)
                return;

            Equipment = Equipment.Loadout.Equipments[SelectedEquipment.Index + 1];
        }

        [DataSourceMethod]
        public void ExecuteCreateSet()
        {
            Equipment = SelectedTroop.Loadout.CreateAlternate(); // re-assign to trigger any bindings
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
                        .SetTextVariable("TROOP", SelectedTroop.Name)
                        .ToString(),
                    true,
                    true,
                    L.S("confirm", "Confirm"),
                    L.S("cancel", "Cancel"),
                    () =>
                    {
                        // Clear all staged changes
                        foreach (var s in EquipmentPanel.Slots)
                            s.Unstage();

                        // Unequip all items
                        SelectedTroop.UnequipAll(SelectedEquipment.Index, stock: true);

                        // Remove the set
                        SelectedTroop.Loadout.RemoveAlternate(SelectedEquipment);

                        // Select the battle set after removal
                        Equipment = SelectedTroop.Loadout.Battle;
                    },
                    () => { }
                )
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Refresh                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void RefreshOnTroopChange()
        {
            Equipment = SelectedTroop.Loadout.Battle; // reset to battle set on troop change
        }

        public void RefreshOnEquipmentChange()
        {
            OnPropertyChanged(nameof(Equipment));
            OnPropertyChanged(nameof(CanUnequip));
            OnPropertyChanged(nameof(CanUnstage));
            OnPropertyChanged(nameof(CanSelectPrevSet));
            OnPropertyChanged(nameof(CanSelectNextSet));
            OnPropertyChanged(nameof(CanRemoveSet));
            OnPropertyChanged(nameof(EquipmentName));
            OnPropertyChanged(nameof(UnequipAllButtonText));
            OnPropertyChanged(nameof(UnstageAllButtonText));
        }
    }
}
