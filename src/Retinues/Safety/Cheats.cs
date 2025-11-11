using System.Collections.Generic;
using TaleWorlds.Library;

namespace Retinues.Safety
{
    /// <summary>
    /// Console cheats for item unlocks. Allows unlocking items via command.
    /// </summary>
    public static class Cheats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Fixes the main party if bugged (e.g. party size stuck at 20).
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("fix_main_party", "retinues")]
        public static string ResetUnlocks(List<string> args)
        {
            Helpers.EnsureMainPartyLeader();
            return "Fix applied.";
        }
    }
}
