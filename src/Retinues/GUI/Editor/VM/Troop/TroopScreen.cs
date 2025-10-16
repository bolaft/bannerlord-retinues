using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Troop.List;
using Retinues.GUI.Editor.VM.Troop.Panel;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    [SafeClass]
    public class TroopScreenVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly EditorVM Editor;

        public TroopScreenVM(EditorVM editor)
        {
            Log.Info("Building TroopScreenVM...");

            Editor = editor;

            // Components
            TroopList = new TroopListVM(this);
            TroopPanel = new TroopPanelVM(this);
        }

        public void Initialize()
        {
            Log.Info("Initializing TroopScreenVM...");

            // Components
            TroopList.Initialize();
            TroopPanel.Initialize();

            // Subscribe to events
            void RefreshRemoveButton()
            {
                OnPropertyChanged(nameof(RemoveTroopButtonIsVisible));
                OnPropertyChanged(nameof(RemoveTroopButtonIsEnabled));
                OnPropertyChanged(nameof(RemoveButtonHint));
            }

            EventManager.TroopChange.Register(RefreshRemoveButton);
            EventManager.TroopListChange.Register(RefreshRemoveButton);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WCharacter SelectedTroop => Editor?.TroopScreen?.TroopList?.Selection?.Troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public TroopListVM TroopList { get; private set; }

        [DataSourceProperty]
        public TroopPanelVM TroopPanel { get; private set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool RemoveTroopButtonIsVisible =>
            Editor?.Screen == Screen.Troop
            && !SelectedTroop?.IsRetinue == true
            && !SelectedTroop?.IsMilitia == true;

        [DataSourceProperty]
        public bool RemoveTroopButtonIsEnabled =>
            Editor?.Screen == Screen.Troop && SelectedTroop?.IsDeletable == true;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string RemoveTroopButtonText => L.S("remove_button_text", "Remove");

        /* ━━━━━━━━ Tooltip ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel RemoveButtonHint
        {
            get
            {
                if (SelectedTroop?.IsDeletable == true)
                    return null; // No hint if can remove

                return Tooltip.MakeTooltip(
                    null,
                    SelectedTroop?.Parent == null
                            ? L.S("cant_remove_root_troop", "Root troops cannot be removed.")
                        : (SelectedTroop?.UpgradeTargets?.Count() ?? 0) > 0
                            ? L.S(
                                "cant_remove_troop_with_targets",
                                "Troops that have upgrade targets cannot be removed."
                            )
                        : string.Empty
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteRemoveTroop()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    SelectedTroop,
                    L.S("action_remove", "remove")
                ) == false
            )
                return; // Removal not allowed in current context

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("remove_troop", "Remove Troop"),
                    text: L.T(
                            "remove_troop_text",
                            "Are you sure you want to permanently remove {TROOP_NAME}?\n\nTheir equipment will be stocked for later use, and existing troops will be converted to their {CULTURE} counterpart."
                        )
                        .SetTextVariable("TROOP_NAME", SelectedTroop?.Name)
                        .SetTextVariable("CULTURE", SelectedTroop?.Culture?.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        // Clear all staged equipment changes
                        TroopEquipBehavior.Instance.ClearStagedChanges(SelectedTroop);

                        // Clear all staged skill changes
                        TroopTrainBehavior.Instance.ClearStagedChanges(SelectedTroop);

                        // Remove the troop
                        SelectedTroop?.Remove(stock: true);

                        // Fire event
                        EventManager.TroopListChange.Fire();
                    },
                    negativeAction: () => { }
                )
            );
        }
    }
}
