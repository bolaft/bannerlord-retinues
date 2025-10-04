using System.Collections.Generic;
using Retinues.Core.Mods.Shokuho;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Mods
{
    [SafeClass]
    public static class ModCompatibility
    {
        private static readonly List<string> IncompatibleMods =
        [
            "WarlordsBattlefield",
            "SimpleBank",
        ];

        public static void AddBehaviors(CampaignGameStarter cs)
        {
            if (ModuleChecker.GetModule("Shokuho") != null)
            {
                Log.Debug("Shokuho detected, using ShokuhoVolunteerSwapBehavior.");
                cs.AddBehavior(new ShokuhoVolunteerSwapBehavior());
            }
        }

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
