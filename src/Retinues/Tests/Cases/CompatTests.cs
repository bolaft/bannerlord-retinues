using Retinues.Mods;
using Retinues.Troops;
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
            Tests.AssertEqual(
                ModCompatibility.HasTier7Unlocker,
                ModCompatibility.HasTier7Unlocker,
                "HasTier7Unlocker is stable."
            );
        }

        /// <summary>
        /// A troop's max tier reflects the Tier 7 Troop Unlocker flag (+1 when present).
        /// </summary>
        [GameTest(
            "Tier7UnlockerControlsMaxTier",
            "compat",
            "Max tier reflects the Tier 7 Troop Unlocker flag"
        )]
        public static void Tier7UnlockerControlsMaxTier(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: true, copyWholeTree: false);
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

            var elite = faction.RootElite;
            var basic = faction.RootBasic;
            Tests.AssertNotNull(elite, "Elite root exists.");
            Tests.AssertNotNull(basic, "Basic root exists.");

            int bonus = ModCompatibility.HasTier7Unlocker ? 1 : 0;
            Tests.AssertEqual(
                5 + bonus,
                basic.MaxTier,
                "Basic max tier reflects the Tier 7 unlocker flag."
            );
            Tests.AssertEqual(
                6 + bonus,
                elite.MaxTier,
                "Elite max tier reflects the Tier 7 unlocker flag."
            );
        }
    }
}
