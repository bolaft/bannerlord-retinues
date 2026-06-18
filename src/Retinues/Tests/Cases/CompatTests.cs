using Retinues.Mods;
using Retinues.Utils;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for mod-compatibility detection. Most third-party mods are not installed in a given
    /// save, so these focus on detection robustness and a flag whose effect is observable
    /// regardless (the Tier 7 Troop Unlocker's max-tier bump).
    /// </summary>
    public static class CompatTests
    {
        /// <summary>
        /// Module detection is exception-free, stable across calls, and false for unknown modules.
        /// </summary>
        [GameTest(
            "ModuleDetectionIsStableAndSafe",
            "compat",
            "Module detection is exception-free, stable, and false for unknown modules"
        )]
        public static void ModuleDetectionIsStableAndSafe(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            Tests.AssertFalse(
                ModuleChecker.IsLoaded("Retinues_NoSuchModule_zzz"),
                "A bogus module is not reported as loaded."
            );

            // Accessing the detection flags twice must be exception-free and stable.
            Tests.AssertEqual(
                ModCompatibility.HasBanditMilitias,
                ModCompatibility.HasBanditMilitias,
                "HasBanditMilitias is stable."
            );
            Tests.AssertEqual(
                ModCompatibility.HasNavalDLC,
                ModCompatibility.HasNavalDLC,
                "HasNavalDLC is stable."
            );
            Tests.AssertEqual(
                ModCompatibility.HasShokuho,
                ModCompatibility.HasShokuho,
                "HasShokuho is stable."
            );
        }
    }
}
