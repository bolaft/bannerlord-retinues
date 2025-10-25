using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Features.Retinues.Behaviors;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Helpers;
using Retinues.GUI.Editor.VM.Troop.List;
using Retinues.GUI.Editor.VM.Troop.Panel;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

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
        public BasicTooltipViewModel CustomizationHint =>
            Tooltip.MakeTooltip(
                null,
                L.S(
                    "customization_hint",
                    ShowCustomization ? "Hide customization controls" : "Show customization controls"
                )
            );

        [DataSourceProperty]
        public BasicTooltipViewModel GenderHint => Tooltip.MakeTooltip( null, L.S("gender_hint", "Gender") );

        [DataSourceProperty]
        public BasicTooltipViewModel AgeHint => Tooltip.MakeTooltip( null, L.S("age_hint", "Age") );

        [DataSourceProperty]
        public BasicTooltipViewModel HeightHint => Tooltip.MakeTooltip(null, L.S("height_hint", "Height"));

        [DataSourceProperty]
        public BasicTooltipViewModel WeightHint => Tooltip.MakeTooltip( null, L.S("weight_hint", "Weight") );

        [DataSourceProperty]
        public BasicTooltipViewModel BuildHint => Tooltip.MakeTooltip(null, L.S("build_hint", "Build"));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
        //                      Customization                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteToggleCustomization()
        {
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
            CharacterCustomization.ApplyNextAgePreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevAgePreset()
        {
            CharacterCustomization.ApplyPrevAgePreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextHeightPreset()
        {
            CharacterCustomization.ApplyNextHeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevHeightPreset()
        {
            CharacterCustomization.ApplyPrevHeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextWeightPreset()
        {
            CharacterCustomization.ApplyNextWeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevWeightPreset()
        {
            CharacterCustomization.ApplyPrevWeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextBuildPreset()
        {
            CharacterCustomization.ApplyNextBuildPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevBuildPreset()
        {
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
