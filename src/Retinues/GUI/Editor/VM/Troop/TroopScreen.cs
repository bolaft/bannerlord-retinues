using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.GUI.Editor.VM.Troop.List;
using Retinues.GUI.Editor.VM.Troop.Panel;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    /// <summary>
    /// ViewModel for the troop editor screen, managing list and panel.
    /// </summary>
    [SafeClass]
    public class TroopScreenVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Troop] =
                [
                    nameof(RemoveTroopButtonIsVisible),
                    nameof(RemoveTroopButtonIsEnabled),
                    nameof(RemoveTroopButtonText),
                    nameof(RemoveButtonHint),
                ],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public TroopListVM TroopList { get; private set; } = new();

        [DataSourceProperty]
        public TroopPanelVM TroopPanel { get; private set; } = new();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool RemoveTroopButtonIsVisible =>
            State.Troop?.IsMilitia == false && State.Troop?.IsRetinue == false;

        [DataSourceProperty]
        public bool RemoveTroopButtonIsEnabled => State.Troop?.IsDeletable == true;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string RemoveTroopButtonText => L.S("remove_button_text", "Remove");

        /* ━━━━━━━━ Tooltip ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel RemoveButtonHint
        {
            get
            {
                if (State.Troop?.IsDeletable == true)
                    return null; // No hint if can remove

                return Tooltip.MakeTooltip(
                    null,
                    State.Troop?.Parent == null
                            ? L.S("cant_remove_root_troop", "Root troops cannot be removed.")
                        : (State.Troop?.UpgradeTargets?.Count() ?? 0) > 0
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
        /// <summary>
        /// Remove the currently selected troop (with confirmation).
        /// </summary>
        public void ExecuteRemoveTroop()
        {
            if (State.Troop == null)
                return;
            
            if (State.Troop.IsDeletable == false)
                return;

            if (
                TroopRules.IsAllowedInContextWithPopup(State.Troop, L.S("action_remove", "remove"))
                == false
            )
                return; // Removal not allowed in current context

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("remove_troop", "Remove Troop"),
                    text: L.T(
                            "remove_troop_text",
                            "Are you sure you want to permanently remove {TROOP_NAME}?\n\nTheir equipment will be stocked for later use, and existing troops will be converted to their {CULTURE} counterpart."
                        )
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
                        .SetTextVariable("CULTURE", State.Troop.Culture?.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        // Clear all staged equipment changes
                        TroopEquipBehavior.ClearAllStagedChanges(State.Troop);

                        // Clear all staged skill changes
                        TroopTrainBehavior.ClearAllStagedChanges(State.Troop);

                        // Remove the troop
                        State.Troop.Remove(stock: true);

                        // Update global state
                        State.UpdateFaction(State.Faction);
                    },
                    negativeAction: () => { }
                )
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Show this screen and its child components.
        /// </summary>
        public override void Show()
        {
            base.Show();
            TroopList.Show();
            TroopPanel.Show();
        }

        /// <summary>
        /// Hide this screen and its child components.
        /// </summary>
        public override void Hide()
        {
            TroopList.Hide();
            TroopPanel.Hide();
            base.Hide();
        }
    }
}
