using System.Collections.Generic;
using TaleWorlds.Library;
using Retinues.Safety.Fixes;

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
        /// Fixes party leaders if a party has no leader assigned.
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("fix_party_leaders", "retinues")]
        public static string FixPartyLeaders(List<string> args)
        {
            PartyLeaderFixBehavior.FixPartyLeaders();
            return "Fix applied.";
        }
    }
}
