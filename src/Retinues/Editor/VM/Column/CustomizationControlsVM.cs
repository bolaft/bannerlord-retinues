using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Engine;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Column
{
    public class CustomizationControlsVM : BaseVM
    {
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
            ShowCustomization = !ShowCustomization;

            OnPropertyChanged(nameof(ShowCustomization));
            OnPropertyChanged(nameof(CustomizationHint));
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

        [DataSourceProperty]
        public Tooltip GenderToggleHint => new(L.T("gender_toggle_hint", "Toggle Gender"));

        [DataSourceMethod]
        public void ExecuteToggleGender() => CharacterController.ChangeGender();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Presets                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Hints ━━━━━━━━ */

        [DataSourceProperty]
        public Tooltip AgeHint => new(L.T("age_hint", "Age"));

        [DataSourceProperty]
        public Tooltip HeightHint => new(L.T("height_hint", "Height"));

        [DataSourceProperty]
        public Tooltip WeightHint => new(L.T("weight_hint", "Weight"));

        [DataSourceProperty]
        public Tooltip BuildHint => new(L.T("build_hint", "Build"));

        /* ━━━━━━━ Controls ━━━━━━━ */

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
