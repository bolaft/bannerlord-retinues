using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using Retinues.Utilities;

namespace Retinues.Editor.MVC.Pages.Character.Controllers
{
    /// <summary>
    /// Controller for managing Captain variants and related UI actions.
    /// </summary>
    public class CaptainsController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Toggle Captain Mode                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Toggles Captain mode for the selected character.
        /// </summary>
        public static ControllerAction<bool> ToggleCaptainMode { get; } =
            Action<bool>("ToggleCaptainMode")
                .DefaultTooltip(value =>
                    value
                        ? L.T("captain_mode_on_hint", "Edit captain")
                        : L.T("captain_mode_off_hint", "Back to base troop")
                )
                .ExecuteWith(ToggleCaptainModeImpl)
                .Fire(UIEvent.Character);

        /// <summary>
        /// Creates or switches to the Captain variant of the selected troop, or back to the base troop.
        /// </summary>
        private static void ToggleCaptainModeImpl(bool captainMode)
        {
            var wc = State.Character;
            if (wc == null)
                return;

            if (wc.IsHero)
                return;

            if (captainMode)
            {
                // Base -> Captain (create if missing)
                if (!wc.IsCaptain)
                {
                    var captain = wc.Captain ?? wc.CreateCaptain();
                    if (captain == null)
                    {
                        Log.Warning("Could not create a Captain variant for this troop.");
                        return;
                    }

                    State.Character = captain;
                }

                return;
            }

            // Captain -> Base
            if (wc.IsCaptain)
            {
                var baseTroop = wc.CaptainBase;
                if (baseTroop == null)
                {
                    Log.Warning($"Could not switch to base troop: Captain base is null for {wc}");
                    return;
                }

                State.Character = baseTroop;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Toggle Enabled                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Enables or disables the Captain variant for the selected unit.
        /// </summary>
        public static ControllerAction<WCharacter> ToggleCaptainEnabled { get; } =
            Action<WCharacter>("ToggleCaptainEnabled")
                .AddCondition(
                    c => State.Character.IsCaptain,
                    L.T("captain_toggle_enabled_not_captain_reason", "Not a captain")
                )
                .DefaultTooltip(c =>
                    State.Character.IsCaptainEnabled
                        ? L.T("captain_toggle_disabled_hint", "Disable this captain")
                        : L.T("captain_toggle_enabled_hint", "Enable this captain")
                )
                .ExecuteWith(c => ToggleCaptainEnabledImpl(c ?? State.Character))
                .Fire(UIEvent.Character);

        /// <summary>
        /// Toggles the enabled flag on the given Captain character.
        /// </summary>
        private static void ToggleCaptainEnabledImpl(WCharacter wc)
        {
            if (wc == null)
                return;

            if (!wc.IsCaptain)
                return;

            wc.IsCaptainEnabled = !wc.IsCaptainEnabled;
        }
    }
}
