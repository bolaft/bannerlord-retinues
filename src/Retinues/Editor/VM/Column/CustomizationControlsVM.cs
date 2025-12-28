using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers.Character;
using Retinues.Helpers;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Editor.VM.Column
{
    public class CustomizationControlsVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible =>
            EditorVM.Page == EditorPage.Character || EditorVM.Page == EditorPage.Equipment;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Show / Hide                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool ShowCustomization { get; set; } = false;

        [DataSourceProperty]
        public Tooltip CustomizationHint =>
            new(
                L.S(
                    "customization_hint",
                    ShowCustomization
                        ? "Hide customization controls"
                        : "Show customization controls"
                )
            );

        [DataSourceMethod]
        public void ExecuteToggleCustomization()
        {
            if (State.Character.Hero is WHero wh)
            {
                Barber.OpenForHero(wh.Base, () => EventManager.Fire(UIEvent.Appearance));
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

        [EventListener(UIEvent.Gender)]
        [DataSourceProperty]
        public string GenderIcon =>
            State.Character.IsFemale == true
                ? "SPGeneral\\GeneralFlagIcons\\female_only"
                : "SPGeneral\\GeneralFlagIcons\\male_only";

        [EventListener(UIEvent.Culture)]
        [DataSourceProperty]
        public Tooltip GenderToggleHint =>
            CharacterController.ToggleGender.Tooltip(State.Character.Editable as WCharacter);

        [EventListener(UIEvent.Culture)]
        [DataSourceProperty]
        public string GenderIconColor => CanToggleGender ? "#c7ac85ff" : "#808080ff";

        [EventListener(UIEvent.Culture)]
        [DataSourceProperty]
        public bool CanToggleGender =>
            CharacterController.ToggleGender.Allow(State.Character.Editable as WCharacter);

        [DataSourceMethod]
        public void ExecuteToggleGender() =>
            CharacterController.ToggleGender.Execute(State.Character.Editable as WCharacter);

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

        [DataSourceMethod]
        public void ExecutePrevAgePreset() => BodyController.PrevAgePreset();

        [DataSourceMethod]
        public void ExecuteNextAgePreset() => BodyController.NextAgePreset();

        [DataSourceMethod]
        public void ExecutePrevHeightPreset() => BodyController.PrevHeightPreset();

        [DataSourceMethod]
        public void ExecuteNextHeightPreset() => BodyController.NextHeightPreset();

        [DataSourceMethod]
        public void ExecutePrevWeightPreset() => BodyController.PrevWeightPreset();

        [DataSourceMethod]
        public void ExecuteNextWeightPreset() => BodyController.NextWeightPreset();

        [DataSourceMethod]
        public void ExecutePrevBuildPreset() => BodyController.PrevBuildPreset();

        [DataSourceMethod]
        public void ExecuteNextBuildPreset() => BodyController.NextBuildPreset();
    }
}
