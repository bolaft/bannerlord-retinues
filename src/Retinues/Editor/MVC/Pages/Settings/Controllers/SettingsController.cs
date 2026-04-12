using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using Retinues.Settings;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Pages.Settings.Controllers
{
    /// <summary>
    /// Controller for settings-page actions.
    /// </summary>
    public sealed class SettingsController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Presets                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Applies the Default preset after confirmation.
        /// </summary>
        public static ControllerAction<object> ApplyDefaultPreset { get; } =
            Action<object>("ApplyDefaultPreset")
                .DefaultTooltip(
                    L.T(
                        "preset_default_tooltip",
                        "Apply the Default preset: a balanced experience designed for a first playthrough"
                    )
                )
                .ExecuteWith(_ => ShowPresetConfirmation(SettingsPreset.Default));

        /// <summary>
        /// Applies the Freeform preset after confirmation.
        /// </summary>
        public static ControllerAction<object> ApplyFreeformPreset { get; } =
            Action<object>("ApplyFreeformPreset")
                .DefaultTooltip(
                    L.T(
                        "preset_freeform_tooltip",
                        "Apply the Freeform preset: removes costs, requirements, and restrictions"
                    )
                )
                .ExecuteWith(_ => ShowPresetConfirmation(SettingsPreset.Freeform));

        /// <summary>
        /// Applies the Realistic preset after confirmation.
        /// </summary>
        public static ControllerAction<object> ApplyRealisticPreset { get; } =
            Action<object>("ApplyRealisticPreset")
                .DefaultTooltip(
                    L.T(
                        "preset_realistic_tooltip",
                        "Apply the Realistic preset: enables time mechanics, location limits, and faction filters"
                    )
                )
                .ExecuteWith(_ => ShowPresetConfirmation(SettingsPreset.Realistic));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ShowPresetConfirmation(SettingsPreset preset)
        {
            string title = preset switch
            {
                SettingsPreset.Freeform => "Apply Freeform Preset",
                SettingsPreset.Realistic => "Apply Realistic Preset",
                _ => "Reset to Defaults",
            };

            string description = BuildPresetDescription(preset);

            string confirmLabel = preset switch
            {
                SettingsPreset.Default => "Reset",
                _ => "Apply",
            };

            Inquiries.Popup(
                title: new TextObject(title),
                onConfirm: () => ConfigurationManager.ApplyPreset(preset),
                description: new TextObject(description),
                confirmText: new TextObject(confirmLabel),
                cancelText: GameTexts.FindText("str_cancel"),
                pauseGame: true
            );
        }

        private static string BuildPresetDescription(SettingsPreset preset)
        {
            string intro = preset switch
            {
                SettingsPreset.Freeform =>
                    "Removes costs, requirements, unlock systems, and availability restrictions for a relaxed, unrestricted experience.",
                SettingsPreset.Realistic =>
                    "Enables location restrictions, time-based mechanics, equipment weight and value limits, and faction-based recruitment filters for a more grounded experience.",
                _ =>
                    "A balanced experience designed for a first playthrough. Resets all settings to their original default values.",
            };

            return intro;
        }
    }
}
