using System;
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
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private TroopPanelVM _troopPanel = new();

        [DataSourceProperty]
        public TroopPanelVM TroopPanel
        {
            get => _troopPanel;
            set
            {
                if (_troopPanel == value)
                    return;
                _troopPanel = value;
                OnPropertyChanged(nameof(TroopPanel));
            }
        }

        private TroopListVM _troopList = new();

        [DataSourceProperty]
        public TroopListVM TroopList
        {
            get => _troopList;
            set
            {
                if (_troopList == value)
                    return;
                _troopList = value;
                OnPropertyChanged(nameof(TroopList));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public event Action<WCharacter> TroopChanged;

        internal void TriggerOnTroopChanged(WCharacter troop)
        {
            TroopChanged?.Invoke(troop);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool RemoveTroopButtonIsVisible =>
            Editor.Screen == Screen.Troop && !SelectedTroop.IsRetinue && !SelectedTroop.IsMilitia;

        [DataSourceProperty]
        public bool RemoveTroopButtonIsEnabled =>
            Editor.Screen == Screen.Troop && SelectedTroop.IsDeletable;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string RemoveButtonText => L.S("remove_button_text", "Remove");

        /* ━━━━━━━━ Tooltip ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel RemoveButtonHint
        {
            get
            {
                if (SelectedTroop.IsDeletable)
                    return null; // No hint if can remove

                return Tooltip.MakeTooltip(
                    null,
                    SelectedTroop?.Parent is null
                            ? L.S("cant_remove_root_troop", "Root troops cannot be removed.")
                        : SelectedTroop?.UpgradeTargets.Count() > 0
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
                        .SetTextVariable("TROOP_NAME", SelectedTroop.Name)
                        .SetTextVariable("CULTURE", SelectedTroop.Culture?.Name)
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
                        SelectedTroop.Remove(stock: true);

                        // Recreate the troop list
                        Editor.TroopScreen.TroopList = new TroopListVM();
                    },
                    negativeAction: () => { }
                )
            );
        }
    }
}
