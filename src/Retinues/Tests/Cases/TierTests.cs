using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using Retinues.Troops;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the Maximum Troop Tier option: it must raise the engine tier cap (so troops can
    /// actually rank past 6) and tiers 8-10 must draw their own skill-total budgets.
    /// </summary>
    public static class TierTests
    {
        /// <summary>
        /// With the cap raised to 10, a high-level troop reaches tiers 8-10 (proving the
        /// MaxCharacterTier engine patch took effect) and uses the matching per-tier skill total.
        /// </summary>
        [GameTest(
            "MaxTierRaisesCapAndBudget",
            "tiers",
            "Maximum Troop Tier raises the engine cap and tiers 8-10 use their own budgets"
        )]
        public static void MaxTierRaisesCapAndBudget(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            using (TestConfig.Set(Config.MaxTroopTier, 10))
            {
                Tests.AssertEqual(
                    10,
                    WCharacter.EliteMaxTier,
                    "EliteMaxTier follows the configured cap."
                );

                var troop = sandbox.NewStub();

                // Tier = clamp(ceil((Level - 5) / 5), 0, MaxCharacterTier). Level 45 -> tier 8.
                troop.Level = 45;
                Tests.AssertEqual(
                    8,
                    troop.Tier,
                    "A level-45 troop reaches tier 8 once the engine cap is raised."
                );
                int total8 = Config.SkillTotalTier8;
                Tests.AssertEqual(
                    total8,
                    SkillManager.SkillTotalByTier(troop),
                    "Tier 8 troop draws the Tier 8 skill total."
                );

                // Level 55 -> tier 10.
                troop.Level = 55;
                Tests.AssertEqual(
                    10,
                    troop.Tier,
                    "A level-55 troop reaches tier 10 once the engine cap is raised."
                );
                int total10 = Config.SkillTotalTier10;
                Tests.AssertEqual(
                    total10,
                    SkillManager.SkillTotalByTier(troop),
                    "Tier 10 troop draws the Tier 10 skill total."
                );
            }
        }

        /// <summary>
        /// The basic line caps one tier below the elite line, matching the vanilla relationship.
        /// </summary>
        [GameTest(
            "BasicLineCapsOneBelowElite",
            "tiers",
            "Basic troop max tier is one below the configured elite cap"
        )]
        public static void BasicLineCapsOneBelowElite(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            using (TestConfig.Set(Config.MaxTroopTier, 9))
            {
                var faction = sandbox.NewFaction();
                Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
                TroopBuilder.CreateTroops(faction, isElite: true, copyWholeTree: false);
                TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

                var elite = faction.RootElite;
                var basic = faction.RootBasic;
                Tests.AssertNotNull(elite, "Elite root exists.");
                Tests.AssertNotNull(basic, "Basic root exists.");

                Tests.AssertEqual(9, elite.MaxTier, "Elite line caps at the configured tier.");
                Tests.AssertEqual(8, basic.MaxTier, "Basic line caps one tier below the elite line.");
            }
        }
    }
}
