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

        /// <summary>
        /// Represents a custom (non-matching) settings state. Clicking this does nothing.
        /// </summary>
        public static ControllerAction<object> CustomPreset { get; } =
            Action<object>("CustomPreset").ExecuteWith(_ => { });

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ShowPresetConfirmation(SettingsPreset preset)
        {
            TextObject title = preset switch
            {
                SettingsPreset.Freeform => L.T(
                    "preset_confirmation_freeform_title",
                    "Apply Freeform Preset"
                ),
                SettingsPreset.Realistic => L.T(
                    "preset_confirmation_realistic_title",
                    "Apply Realistic Preset"
                ),
                _ => L.T("preset_confirmation_default_title", "Reset to Defaults"),
            };

            TextObject confirmLabel = L.T("preset_confirm_apply", "Apply");

            Inquiries.Popup(
                title: title,
                onConfirm: () => ConfigurationManager.ApplyPreset(preset),
                description: BuildPresetDescription(preset),
                confirmText: confirmLabel,
                cancelText: GameTexts.FindText("str_cancel"),
                pauseGame: true
            );
        }

        private static TextObject BuildPresetDescription(SettingsPreset preset) =>
            preset switch
            {
                SettingsPreset.Freeform => L.T(
                    "preset_description_freeform",
                    "A creative sandbox: no costs, no requirements, no restrictions.\n\nDoctrine acquisition is free. Equipment is free and all items are immediately available. Skill points can be assigned freely with no experience requirement. Clan troops are available from the start and can be recruited anywhere."
                ),
                SettingsPreset.Realistic => L.T(
                    "preset_description_realistic",
                    "A stricter experience that emphasises earned progression and faction identity.\n\nSkill and equipment limits are enforced in the Universal Editor. Troops can only be modified while in an owned fief. Retinue buffs are disabled. Equipping and training take time. Equipment is subject to tier-based weight and value limits. Troops can only be recruited in same-culture settlements, and only culture roots are generated at game start."
                ),
                _ => L.T(
                    "preset_description_default",
                    "The default configuration, balanced for a first playthrough.\n\nRetinue buffs are active, clan troops unlock with your first fief, equipment has a cost, and items must be unlocked through kills and workshops before they can be equipped. Skill points must be earned in battle."
                ),
            };
    }
}
