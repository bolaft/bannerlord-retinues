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
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly EditorVM Editor;

        public EquipmentScreenVM(EditorVM editor)
        {
            Log.Info("Building EquipmentScreenVM...");

            Editor = editor;

            // Components
            EquipmentPanel = new EquipmentPanelVM(Editor);
            EquipmentList = new EquipmentListVM(Editor);
        }

        public void Initialize()
        {
            Log.Info("Initializing EquipmentScreenVM...");

            // Components
            EquipmentPanel.Initialize();
            EquipmentList.Initialize();

            // Subscribe to events
            EventManager.TroopChange.Register(() => Equipment = SelectedTroop?.Loadout?.Battle);
            EventManager.EquipmentChange.RegisterProperties(
                this,
                nameof(Equipment),
                nameof(CanUnequip),
                nameof(CanUnstage),
                nameof(CanSelectPrevSet),
                nameof(CanSelectNextSet),
                nameof(CanRemoveSet),
                nameof(EquipmentName),
                nameof(UnequipAllButtonText),
                nameof(UnstageAllButtonText)
            );
            EventManager.EquipmentItemChange.RegisterProperties(
                this,
                nameof(CanUnequip),
                nameof(CanUnstage),
                nameof(UnequipAllButtonText),
                nameof(UnstageAllButtonText)
            );

            // Initial state
            Equipment = SelectedTroop?.Loadout?.Battle;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        private WCharacter SelectedTroop => Editor?.TroopScreen?.TroopList?.Selection?.Troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WEquipment _equipment;
        public WEquipment Equipment
        {
            get => _equipment;
            set
            {
                if (_equipment == value)
                    return;

                _equipment = value;

                EventManager.EquipmentChange.Fire();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public EquipmentPanelVM EquipmentPanel { get; private set; }

        [DataSourceProperty]
        public EquipmentListVM EquipmentList { get; private set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━ Unequip / Unstage ━━ */

        [DataSourceProperty]
        public bool CanUnstage => EquipmentPanel?.Slots?.Any(s => s.IsStaged) ?? false;

        [DataSourceProperty]
        public bool CanUnequip => Equipment?.Items?.Count() > 0;

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
                return Equipment?.Category switch
                {
                    EquipmentCategory.Battle => L.S("set_battle", "Battle"),
                    EquipmentCategory.Civilian => L.S("set_civilian", "Civilian"),
                    _ => L.T("set_alt_n", "Alt {N}")
                        .SetTextVariable(
                            "N",
                            Equipment?.Loadout?.Alternates?.IndexOf(Equipment) + 1 ?? 1
                        )
                        .ToString(),
                };
            }
        }

        [DataSourceProperty]
        public bool CanSelectPrevSet => (Equipment?.Index ?? 0) > 0;

        [DataSourceProperty]
        public bool CanSelectNextSet =>
            (Equipment?.Index ?? 0) < ((SelectedTroop?.Loadout?.Equipments?.Count ?? 1) - 1);

        [DataSourceProperty]
        public bool CanRemoveSet => Equipment?.Category == EquipmentCategory.Alternate;

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
                        .SetTextVariable("TROOP_NAME", SelectedTroop?.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        SelectedTroop?.UnequipAll(Equipment?.Index ?? 0, stock: true);
                        EventManager.EquipmentItemChange.Fire();
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
                        .SetTextVariable("TROOP_NAME", SelectedTroop?.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        foreach (
                            var slot in EquipmentPanel?.Slots ?? Enumerable.Empty<EquipmentSlotVM>()
                        )
                            slot.Unstage(noEvent: true);

                        EventManager.EquipmentItemChange.Fire();
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

            Equipment = Equipment?.Loadout?.Equipments?[(Equipment?.Index ?? 1) - 1];
        }

        [DataSourceMethod]
        public void ExecuteNextSet()
        {
            if (CanSelectNextSet == false)
                return;

            Equipment = Equipment?.Loadout?.Equipments?[(Equipment?.Index ?? -1) + 1];
        }

        [DataSourceMethod]
        public void ExecuteCreateSet()
        {
            Equipment = SelectedTroop?.Loadout?.CreateAlternate(); // re-assign to trigger any bindings
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
                        .SetTextVariable("TROOP", SelectedTroop?.Name)
                        .ToString(),
                    true,
                    true,
                    L.S("confirm", "Confirm"),
                    L.S("cancel", "Cancel"),
                    () =>
                    {
                        // Clear all staged changes
                        foreach (
                            var s in EquipmentPanel?.Slots ?? Enumerable.Empty<EquipmentSlotVM>()
                        )
                            s.Unstage();

                        // Unequip all items
                        SelectedTroop?.UnequipAll(Equipment?.Index ?? 0, stock: true);

                        // Remove the set
                        SelectedTroop?.Loadout?.RemoveAlternate(Equipment);

                        // Select the battle set after removal
                        Equipment = SelectedTroop?.Loadout?.Battle;
                    },
                    () => { }
                )
            );
        }
    }
}
