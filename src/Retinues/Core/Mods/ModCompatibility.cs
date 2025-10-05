using System.Collections.Generic;
using Retinues.Core.Mods.Shokuho;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Mods
{
    /// <summary>
    /// Handles compatibility checks and integration with other mods.
    /// Registers behaviors for detected mods and warns about known incompatibilities.
    /// </summary>
    [SafeClass]
    public static class ModCompatibility
    {
        private static readonly List<string> IncompatibleMods =
        [
            "WarlordsBattlefield",
            "SimpleBank",
        ];

        /// <summary>
        /// Adds mod-specific behaviors to the campaign starter if compatible mods are detected.
        /// </summary>
        public static void AddBehaviors(CampaignGameStarter cs)
        {
            if (ModuleChecker.GetModule("Shokuho") != null)
            {
                Log.Debug("Shokuho detected, using ShokuhoVolunteerSwapBehavior.");
                cs.AddBehavior(new ShokuhoVolunteerSwapBehavior());
            }
        }

        /// <summary>
        /// Checks for incompatible mods and logs critical warnings if any are found.
        /// </summary>
        public static void IncompatibilityCheck()
        {
            foreach (var modId in IncompatibleMods)
            {
                var mod = ModuleChecker.GetModule(modId);
                if (mod != null)
                {
                    Log.Critical($"[Retinues] WARNING: incompatible mod detected: '{mod}'.");
                }
            }
        }
    }
}
