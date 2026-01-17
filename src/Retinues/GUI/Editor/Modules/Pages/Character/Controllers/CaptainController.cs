using Retinues.Domain.Characters.Wrappers;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Services;
using Retinues.Utilities;

namespace Retinues.GUI.Editor.Modules.Pages.Character.Controllers
{
    public class CaptainsController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Toggle Mode                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static ControllerAction<WCharacter> ToggleCaptainMode { get; } =
            Action<WCharacter>("ToggleCaptainMode")
                .AddCondition(
                    c => (c ?? State.Character) != null,
                    L.T("captain_no_character_reason", "No unit is selected.")
                )
                .DefaultTooltip(
                    L.T(
                        "captain_toggle_mode_hint",
                        "Switch between the base troop and its Captain variant."
                    )
                )
                .ExecuteWith(c => ToggleCaptainModeImpl(c ?? State.Character))
                .Fire(UIEvent.Character);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Toggle Enabled                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static ControllerAction<WCharacter> ToggleCaptainEnabled { get; } =
            Action<WCharacter>("ToggleCaptainEnabled")
                .AddCondition(
                    c => State.Character.IsCaptain,
                    L.T("captain_toggle_enabled_not_captain_reason", "This unit is not a Captain.")
                )
                .DefaultTooltip(L.T("captain_toggle_enabled_hint", "Enable/Disable this Captain"))
                .ExecuteWith(c => ToggleCaptainEnabledImpl(c ?? State.Character))
                .Fire(UIEvent.Character);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ToggleCaptainModeImpl(WCharacter wc)
        {
            if (wc == null)
                return;

            // Captain -> Base
            if (wc.IsCaptain)
            {
                var baseTroop = wc.CaptainBase;
                if (baseTroop == null)
                {
                    Log.Warning("Could not switch to base troop: Captain base is null for {0}");
                    return;
                }

                State.Character = baseTroop;
                return;
            }

            // Base -> Captain (create if missing)
            var captain = wc.Captain ?? wc.CreateCaptain();
            if (captain == null)
            {
                Log.Warning("Could not create a Captain variant for this troop.");
                return;
            }

            State.Character = captain;
        }

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
