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
                    "Removes all costs and unlock requirements. Doctrine acquisition is free. Equipment costs and unlock progression are disabled. Clan and kingdom troops are available from the start and can be recruited anywhere. Skill points can be assigned freely without being earned in battle.",
                SettingsPreset.Realistic =>
                    "Enforces skill and equipment limits in the Universal Editor. Troop editing is restricted to owned fiefs. Retinue stat buffs are disabled. Only root troops are generated at game start. Troops can only be recruited in same-culture settlements. Equipping and training take time. Equipment is subject to tier-based weight and value limits.",
                _ =>
                    "The default configuration, balanced for a first playthrough. Troop stat buffs are active, clan troops unlock with your first fief, equipment has a cost but no unlock restrictions, and skill points must be earned in battle. Resets all settings to their original default values.",
            };

            return intro;
        }
    }
}
