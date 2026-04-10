using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using TaleWorlds.Localization;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Settings.Controllers
{
    /// <summary>
    /// Controller for settings-page actions.
    /// </summary>
    public sealed class SettingsController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Reset To Defaults                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Resets all configuration options to their default values after confirmation.
        /// </summary>
        public static ControllerAction<object> ResetToDefaults { get; } =
            Action<object>("ResetToDefaults")
                .DefaultTooltip(
                    L.T("reset_defaults_tooltip", "Reset all settings to their default values")
                )
                .ExecuteWith(_ => ResetToDefaultsImpl());

        private static void ResetToDefaultsImpl()
        {
            Inquiries.Popup(
                title: L.T("reset_defaults_confirm_title", "Reset to Defaults"),
                onConfirm: () => ApplyDefaults(),
                description: L.T(
                    "reset_defaults_confirm_body",
                    "This will reset all settings to their default values. Continue?"
                ),
                confirmText: L.T("reset_defaults_confirm", "Reset"),
                cancelText: GameTexts.FindText("str_cancel"),
                pauseGame: true
            );
        }

        private static void ApplyDefaults()
        {
            var options = Retinues.Settings.ConfigurationManager.Options;
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                if (opt != null)
                    opt.SetObject(opt.Default);
            }
        }
    }
}
