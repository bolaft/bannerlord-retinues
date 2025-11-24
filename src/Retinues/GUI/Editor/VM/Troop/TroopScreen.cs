using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Features.AutoJoin;
using Retinues.Features.Staging;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Troop.List;
using Retinues.GUI.Editor.VM.Troop.Panel;
using Retinues.GUI.Helpers;
using Retinues.Managers;
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
                [UIEvent.Faction] =
                [
                    nameof(ShowHeroAppearanceButton),
                    nameof(CustomizationIsEnabled),
                ],
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
                    nameof(GenderIcon),
                ],
                [UIEvent.Party] = [nameof(RetinueJoinText), nameof(CountInParty)],
                [UIEvent.Appearance] = [nameof(GenderIcon)],
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
        public int RetinueCapValue => AutoJoinBehavior.GetJoinCap(State.Troop);

        [DataSourceProperty]
        public int RetinueCapMax =>
            State.Troop?.IsElite == true
                ? RetinueManager.EliteRetinueCap
                : RetinueManager.BasicRetinueCap;

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool RemoveTroopButtonIsVisible
        {
            get
            {
                return !ClanScreen.IsStudioMode
                    && State.Troop?.IsRegular == true
                    && State.Troop?.IsCustom == true
                    && IsVisible;
            }
        }

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
        public bool CustomizationIsEnabled =>
            Config.EnableTroopCustomization && (ClanScreen.EditorMode != EditorMode.Heroes);

        [DataSourceProperty]
        public bool ShowHeroAppearanceButton => State.Troop is WHero && IsVisible;

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
                    .SetTextVariable(
                        "COST",
                        RetinueManager.ConversionRenownCostPerUnit(State.Troop)
                    )
                    .ToString();
            }
        }

        [DataSourceProperty]
        public string HeroAppearanceButtonText => L.S("hero_appearance_button_text", "Appearance");

        [DataSourceProperty]
        public string GenderIcon =>
            State.Troop?.IsFemale == true
                ? "SPGeneral\\GeneralFlagIcons\\female_only"
                : "SPGeneral\\GeneralFlagIcons\\male_only";

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
        public BasicTooltipViewModel GenderToggleHint =>
            Tooltip.MakeTooltip(null, L.S("gender_toggle_hint", "Toggle Gender"));

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

        [DataSourceProperty]
        public BasicTooltipViewModel HeroAppearanceHint =>
            Tooltip.MakeTooltip(
                null,
                L.S("hero_appearance_hint", "Open the full appearance editor for this hero.")
            );

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
            AutoJoinBehavior.SetJoinCap(State.Troop, cap);

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
            AutoJoinBehavior.SetJoinCap(State.Troop, cap);

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
                ContextManager.IsAllowedInContextWithPopup(
                    State.Troop,
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
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
                        .SetTextVariable("CULTURE", State.Troop.Culture?.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        var troop = State.Troop;

                        //------------------------------------------------------------------
                        // 1) ROLLBACK ALL STAGED EQUIPMENT CHANGES (ALL SETS, ALL SLOTS)
                        //------------------------------------------------------------------
                        foreach (var set in troop.Loadout.Equipments)
                        {
                            int setIndex = set.Index;

                            foreach (var slot in WEquipment.Slots)
                            {
                                var pending = EquipStagingBehavior.Get(troop, slot, setIndex);
                                if (pending == null)
                                    continue;

                                var stagedItem = new WItem(pending.ItemId);

                                // Undo stock/gold deltas from staging
                                EquipmentManager.RollbackStagedEquip(
                                    troop,
                                    setIndex,
                                    slot,
                                    stagedItem
                                );

                                // Remove staged job
                                EquipStagingBehavior.Unstage(troop, slot, setIndex);
                            }
                        }

                        //------------------------------------------------------------------
                        // 2) ROLLBACK ALL PENDING TRAINING CHANGES
                        //------------------------------------------------------------------
                        TrainStagingBehavior.Unstage(troop);

                        //------------------------------------------------------------------
                        // 3) ACTUALLY REMOVE THE TROOP
                        //------------------------------------------------------------------
                        troop.Remove();

                        //------------------------------------------------------------------
                        // 4) REFRESH UI
                        //------------------------------------------------------------------
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
            if (!Config.EnableTroopCustomization)
                return;

            ShowCustomization = !ShowCustomization;
            OnPropertyChanged(nameof(ShowCustomization));
            OnPropertyChanged(nameof(CustomizationHint));
        }

        [DataSourceMethod]
        public void ExecuteChangeGender()
        {
            AppearanceGuard.TryApply(
                State.Troop,
                State.Equipment.Index,
                applyChange: () =>
                {
                    State.Troop.IsFemale = !State.Troop.IsFemale;
                    BodyHelper.ApplyPropertiesFromCulture(State.Troop, State.Troop.Culture.Base);
                    return true;
                },
                onSuccess: () =>
                {
                    State.UpdateAppearance();
                    OnPropertyChanged(nameof(GenderIcon));
                }
            );
        }

        [DataSourceMethod]
        public void ExecuteNextAgePreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            BodyHelper.ApplyNextAgePreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevAgePreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            BodyHelper.ApplyPrevAgePreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextHeightPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            BodyHelper.ApplyNextHeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevHeightPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            BodyHelper.ApplyPrevHeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextWeightPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            BodyHelper.ApplyNextWeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevWeightPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            BodyHelper.ApplyPrevWeightPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteNextBuildPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            BodyHelper.ApplyNextBuildPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecutePrevBuildPreset()
        {
            if (!Config.EnableTroopCustomization)
                return;

            BodyHelper.ApplyPrevBuildPreset(State.Troop);
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteOpenHeroAppearance()
        {
            if (State.Troop is not WHero hero)
                return;

            var snapshotTroop = State.Troop;
            var snapshotFaction = State.Faction;

            // Reuse AppearanceGuard so config/context rules still apply
            AppearanceGuard.TryApply(
                State.Troop,
                State.Equipment.Index,
                applyChange: () =>
                {
                    // We don't change anything here; we just want to pass the guard.
                    return true;
                },
                onSuccess: () =>
                {
                    // Open the vanilla FaceGen / appearance editor for this hero.
                    HeroAppearanceHelper.OpenForHero(hero.Hero);

                    // Restore troop/faction state after returning from FaceGen.
                    State.PendingFaction = snapshotFaction;
                    State.PendingTroop = snapshotTroop;
                }
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

            // Notify visibility-dependent properties
            OnPropertyChanged(nameof(RemoveTroopButtonIsVisible));
        }

        /// <summary>
        /// Hide this screen and its child components.
        /// </summary>
        public override void Hide()
        {
            TroopList.Hide();
            TroopPanel.Hide();
            base.Hide();

            // Notify visibility-dependent properties
            OnPropertyChanged(nameof(RemoveTroopButtonIsVisible));
        }
    }
}
