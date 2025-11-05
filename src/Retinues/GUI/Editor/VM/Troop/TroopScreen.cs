using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Features.Retinues.Behaviors;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Helpers;
using Retinues.GUI.Editor.VM.Troop.List;
using Retinues.GUI.Editor.VM.Troop.Panel;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core;
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
                    nameof(ShowRetinueCap),
                    nameof(RetinueCapMax),
                    nameof(RetinueCapValue),
                    nameof(CanRaiseRetinueCap),
                    nameof(CanLowerRetinueCap),
                    nameof(RetinueJoinText),
                    nameof(CountInParty),
                    nameof(FormationClassIcon),
                ],
                [UIEvent.Party] = [nameof(RetinueJoinText), nameof(CountInParty)],
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

        /* ━━━━━━━━ Values ━━━━━━━━ */

        [DataSourceProperty]
        public int CountInParty =>
            State.PartyData.TryGetValue(State.Troop, out var count) ? count : 0;

        [DataSourceProperty]
        public int RetinueCapValue => RetinueHireBehavior.GetRetinueCap(State.Troop);

        [DataSourceProperty]
        public int RetinueCapMax =>
            State.Troop?.IsElite == true ? TroopRules.MaxEliteRetinue : TroopRules.MaxBasicRetinue;

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool RemoveTroopButtonIsVisible =>
            State.Troop?.IsMilitia == false && State.Troop?.IsRetinue == false;

        [DataSourceProperty]
        public bool RemoveTroopButtonIsEnabled => State.Troop?.IsDeletable == true;

        [DataSourceProperty]
        public bool ShowRetinueCap => State.Troop?.IsRetinue == true;

        [DataSourceProperty]
        public bool CanLowerRetinueCap => RetinueCapValue > 0;

        [DataSourceProperty]
        public bool CanRaiseRetinueCap => RetinueCapValue < RetinueCapMax;

        [DataSourceProperty]
        public bool ShowCustomization { get; set; } = false;

        [DataSourceProperty]
        public bool CustomizationIsEnabled => Config.EnableTroopCustomization;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string RemoveTroopButtonText => L.S("remove_button_text", "Remove");

        [DataSourceProperty]
        public string RetinueCapText => L.S("retinue_cap_text", "Hiring Limit");

        [DataSourceProperty]
        public string RetinueJoinText
        {
            get
            {
                if (State.Troop == null || !State.Troop.IsRetinue)
                    return string.Empty;

                if (RetinueCapValue == 0)
                    return L.S("retinue_join_text_none", "No new retinues will join.");

                if (CountInParty >= RetinueCapValue)
                    return L.S("retinue_join_text_full", "The hiring limit has been reached.");

                return L.T("retinue_join_text", "One new retinue per {COST} renown earned.")
                    .SetTextVariable("COST", TroopRules.ConversionRenownCostPerUnit(State.Troop))
                    .ToString();
            }
        }

        /* ━━━━━━━ Tooltips ━━━━━━━ */

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

        [DataSourceProperty]
        public BasicTooltipViewModel RetinueCapHint =>
            Tooltip.MakeTooltip(
                null,
                L.S(
                    "retinue_cap_tooltip_body",
                    "Retinues will join you for free over time as long as you keep earning renown.\n\nYou can set a hiring limit to control how many retinues can join your party."
                )
            );

        [DataSourceProperty]
        public BasicTooltipViewModel CustomizationHint =>
            Tooltip.MakeTooltip(
                null,
                L.S(
                    "customization_hint",
                    ShowCustomization
                        ? "Hide customization controls"
                        : "Show customization controls"
                )
            );

        [DataSourceProperty]
        public BasicTooltipViewModel GenderHint =>
            Tooltip.MakeTooltip(null, L.S("gender_hint", "Gender"));

        [DataSourceProperty]
        public BasicTooltipViewModel AgeHint => Tooltip.MakeTooltip(null, L.S("age_hint", "Age"));

        [DataSourceProperty]
        public BasicTooltipViewModel HeightHint =>
            Tooltip.MakeTooltip(null, L.S("height_hint", "Height"));

        [DataSourceProperty]
        public BasicTooltipViewModel WeightHint =>
            Tooltip.MakeTooltip(null, L.S("weight_hint", "Weight"));

        [DataSourceProperty]
        public BasicTooltipViewModel BuildHint =>
            Tooltip.MakeTooltip(null, L.S("build_hint", "Build"));

        /* ━━━━━━━━━ Icons ━━━━━━━━ */

        [DataSourceProperty]
        public string FormationClassIcon => Icons.GetFormationClassIcon(State.Troop);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Raise the retinue cap for the selected troop.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteRaiseRetinueCap()
        {
            if (State.Troop == null)
                return;

            if (!CanRaiseRetinueCap)
                return;

            var cap = Math.Min(RetinueCapValue + BatchInput(capped: false), RetinueCapMax);
            RetinueHireBehavior.SetRetinueCap(State.Troop, cap);

            OnPropertyChanged(nameof(RetinueCapValue));
            OnPropertyChanged(nameof(CanRaiseRetinueCap));
            OnPropertyChanged(nameof(CanLowerRetinueCap));
            OnPropertyChanged(nameof(RetinueJoinText));
        }

        /// <summary>
        /// Lower the retinue cap for the selected troop.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteLowerRetinueCap()
        {
            if (State.Troop == null)
                return;

            if (!CanLowerRetinueCap)
                return;

            var cap = Math.Max(RetinueCapValue - BatchInput(capped: false), 0);
            RetinueHireBehavior.SetRetinueCap(State.Troop, cap);

            OnPropertyChanged(nameof(RetinueCapValue));
            OnPropertyChanged(nameof(CanRaiseRetinueCap));
            OnPropertyChanged(nameof(CanLowerRetinueCap));
            OnPropertyChanged(nameof(RetinueJoinText));
        }

        /// <summary>
        /// Remove the currently selected troop (with confirmation).
        /// </summary>
        [DataSourceMethod]
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

        /// <summary>
        /// Change the selected troop's formation class override setting.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteChangeFormationClass()
        {
            try
            {
                if (State.Troop == null)
                    return;

                List<FormationClass> classes =
                [
                    FormationClass.Unset,
                    FormationClass.Infantry,
                    FormationClass.Ranged,
                    FormationClass.Cavalry,
                    FormationClass.HorseArcher,
                ];

                var elements = new List<InquiryElement>(classes.Count);

                foreach (var c in classes)
                {
                    string text =
                        c == FormationClass.Unset
                            ? L.S("formation_auto", "Auto")
                            : c.GetLocalizedName().ToString();

                    elements.Add(new InquiryElement(c, text, null, true, null));
                }

                MBInformationManager.ShowMultiSelectionInquiry(
                    new MultiSelectionInquiryData(
                        titleText: L.S("change_formation_class_title", "Change Formation Class"),
                        descriptionText: null,
                        inquiryElements: elements,
                        isExitShown: true,
                        minSelectableOptionCount: 1,
                        maxSelectableOptionCount: 1,
                        affirmativeText: L.S("confirm", "Confirm"),
                        negativeText: L.S("cancel", "Cancel"),
                        affirmativeAction: selected =>
                        {
                            if (selected == null || selected.Count == 0)
                                return;
                            if (selected[0]?.Identifier is not FormationClass formationClass)
                                return;
                            if (formationClass == State.Troop.FormationClassOverride)
                                return; // No change

                            // Set override
                            State.Troop.FormationClassOverride = formationClass;

                            // Update formation class if needed
                            State.Troop.FormationClass = State.Troop.ComputeFormationClass();

                            // Refresh VM bindings
                            State.UpdateTroop(State.Troop);
                        },
                        negativeAction: new Action<List<InquiryElement>>(_ => { })
                    )
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Customization                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteToggleCustomization()
        {
            if (!Config.EnableTroopCustomization)
                return;

            ShowCustomization = !ShowCustomization;
            OnPropertyChanged(nameof(ShowCustomization));
            OnPropertyChanged(nameof(CustomizationHint));
        }

        [DataSourceMethod]
        public void ExecuteChangeGender()
        {
            State.Troop.IsFemale = !State.Troop.IsFemale;
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextAgePreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            CharacterCustomization.ApplyNextAgePreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevAgePreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            CharacterCustomization.ApplyPrevAgePreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextHeightPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            CharacterCustomization.ApplyNextHeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevHeightPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            CharacterCustomization.ApplyPrevHeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextWeightPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            CharacterCustomization.ApplyNextWeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevWeightPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            CharacterCustomization.ApplyPrevWeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextBuildPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            CharacterCustomization.ApplyNextBuildPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevBuildPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            CharacterCustomization.ApplyPrevBuildPreset(State.Troop);
            State.UpdateAppearance();
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
