using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.Integration.Barber;
using Retinues.Editor.MVC.Common.Column.Controllers;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Common.Column.Views
{
    /// <summary>
    /// ViewModel for the customization controls in the top panel.
    /// </summary>
    public class CustomizationControlsVM : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible =>
            State.Page == EditorPage.Character || State.Page == EditorPage.Equipment;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Show / Hide                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool ShowCustomization { get; set; } = false;

        [DataSourceProperty]
        public Tooltip CustomizationHint =>
            new(
                State.Character.Hero is WHero
                    ? L.S("customization_hint_hero", "Edit character appearance")
                : ShowCustomization ? L.S("customization_hint_show", "Hide customization controls")
                : L.S("customization_hint_hide", "Show customization controls")
            );

        [DataSourceMethod]
        public void ExecuteToggleCustomization()
        {
            if (State.Character.Hero is WHero wh)
            {
                BarberHelper.OpenForHero(wh.Base, () => EventManager.Fire(UIEvent.Appearance));
            }
            else
            {
                ShowCustomization = !ShowCustomization;

                OnPropertyChanged(nameof(ShowCustomization));
                OnPropertyChanged(nameof(CustomizationHint));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Gender                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<WCharacter> GenderCycleButton { get; } =
            new(
                action: GenderController.CycleGender,
                arg: () => State.Character,
                refresh: [UIEvent.Gender, UIEvent.Character, UIEvent.Culture],
                spriteFactory: () =>
                    State.Character?.IsFemale == true
                        ? "SPGeneral\\GeneralFlagIcons\\female_only"
                        : "SPGeneral\\GeneralFlagIcons\\male_only",
                colorFactory: () =>
                    GenderController.CycleGender.Allow(State.Character) ? "#c7ac85ff" : "#808080ff"
            );

        [EventListener(UIEvent.Gender, UIEvent.Character)]
        [DataSourceProperty]
        public bool IsGenderMixed => State.Character?.IsMixedGender ?? false;

        [EventListener(UIEvent.Gender, UIEvent.Character)]
        [DataSourceProperty]
        public bool IsGenderSingle => !(State.Character?.IsMixedGender ?? false);

        [EventListener(UIEvent.Gender, UIEvent.Character, UIEvent.Culture)]
        [DataSourceProperty]
        public string GenderCycleSprite => GenderCycleButton.Sprite;

        [EventListener(UIEvent.Gender, UIEvent.Character, UIEvent.Culture)]
        [DataSourceProperty]
        public string GenderCycleColor => GenderCycleButton.Color;

        [EventListener(UIEvent.Gender, UIEvent.Character)]
        [DataSourceProperty]
        public string GenderMixedOverlaySprite =>
            State.Character?.IsFemale == true
                ? "SPGeneral\\GeneralFlagIcons\\male_only"
                : "SPGeneral\\GeneralFlagIcons\\female_only";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Presets                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Tooltip AgeHint => new(L.T("age_hint", "Age"));

        [DataSourceProperty]
        public Tooltip HeightHint => new(L.T("height_hint", "Height"));

        [DataSourceProperty]
        public Tooltip WeightHint => new(L.T("weight_hint", "Weight"));

        [DataSourceProperty]
        public Tooltip BuildHint => new(L.T("build_hint", "Build"));

        [DataSourceProperty]
        public Button<WCharacter> AgePrevButton { get; } =
            new(
                action: BodyController.DecreaseAge,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Appearance]
            );

        [DataSourceProperty]
        public Button<WCharacter> AgeNextButton { get; } =
            new(
                action: BodyController.IncreaseAge,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Appearance]
            );

        [DataSourceProperty]
        public Button<WCharacter> HeightPrevButton { get; } =
            new(
                action: BodyController.DecreaseHeight,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Appearance]
            );

        [DataSourceProperty]
        public Button<WCharacter> HeightNextButton { get; } =
            new(
                action: BodyController.IncreaseHeight,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Appearance]
            );

        [DataSourceProperty]
        public Button<WCharacter> WeightPrevButton { get; } =
            new(
                action: BodyController.DecreaseWeight,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Appearance]
            );

        [DataSourceProperty]
        public Button<WCharacter> WeightNextButton { get; } =
            new(
                action: BodyController.IncreaseWeight,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Appearance]
            );

        [DataSourceProperty]
        public Button<WCharacter> BuildPrevButton { get; } =
            new(
                action: BodyController.DecreaseBuild,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Appearance]
            );

        [DataSourceProperty]
        public Button<WCharacter> BuildNextButton { get; } =
            new(
                action: BodyController.IncreaseBuild,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Appearance]
            );
    }
}
