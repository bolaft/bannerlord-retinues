using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using Retinues.Troops.Save;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the skill-budget floor. Troops cloned from high-skill templates (e.g. modded
    /// units that carry more skill points than vanilla troops of the same tier) must never be
    /// born over their per-tier budget and locked to decrement-only: the effective budget is
    /// floored to the troop's seeded baseline, and legacy saves backfill that baseline.
    /// </summary>
    public static class SkillBudgetTests
    {
        /// <summary>
        /// SkillTotalByTier is max(configured per-tier total, seeded baseline): a baseline above
        /// the per-tier total raises the budget; a baseline below it changes nothing.
        /// </summary>
        [GameTest(
            "BudgetFlooredToBaseline",
            "skills",
            "Skill total budget is floored to the seeded baseline, never below the per-tier total"
        )]
        public static void BudgetFlooredToBaseline(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var troop = sandbox.NewStub();

            // Pin the configured per-tier total by recording a zero baseline (floor inert).
            troop.SetSkillBaseline(0);
            int configTotal = SkillManager.SkillTotalByTier(troop);

            // A baseline above the per-tier total raises the budget to the baseline (the fix).
            troop.SetSkillBaseline(configTotal + 1000);
            Tests.AssertEqual(
                configTotal + 1000,
                SkillManager.SkillTotalByTier(troop),
                "Budget is floored up to a baseline that exceeds the per-tier total."
            );

            // The troop is never over budget: points-left stays non-negative.
            Tests.AssertTrue(
                SkillManager.SkillPointsLeft(troop) >= 0,
                "A troop whose baseline exceeds the per-tier total has non-negative points left."
            );

            // A baseline below the per-tier total leaves the configured budget unchanged
            // (no regression for vanilla troops, whose skills already fit the budget).
            troop.SetSkillBaseline(configTotal > 0 ? configTotal - 1 : 0);
            Tests.AssertEqual(
                configTotal,
                SkillManager.SkillTotalByTier(troop),
                "A baseline below the per-tier total does not lower the configured budget."
            );
        }

        /// <summary>
        /// Retro-compat: a save written before the SkillBaseline field existed deserializes the
        /// field as 0, and the deserializer backfills the baseline from the loaded skill sum so
        /// existing over-budget troops become editable rather than staying locked.
        /// </summary>
        [GameTest(
            "LegacySaveBackfillsBaseline",
            "skills",
            "A pre-baseline save (SkillBaseline=0) backfills the baseline from the loaded skill sum"
        )]
        public static void LegacySaveBackfillsBaseline(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var vanilla = Player.Clan?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "Player culture has a basic root troop to clone from.");

            using var sandbox = new TestSandbox();

            var troop = sandbox.NewStub();
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            var data = new TroopSaveData(troop);

            // Simulate a save written before the SkillBaseline field existed.
            data.SkillBaseline = 0;

            var rebuilt = data.Deserialize();
            Tests.AssertNotNull(rebuilt, "Deserialize produced a troop.");

            int sum = rebuilt.Skills.Values.Sum();
            Tests.AssertTrue(sum > 0, "Rebuilt troop has skills.");
            Tests.AssertEqual(
                sum,
                rebuilt.SkillBaseline,
                "Legacy save backfills the baseline from the loaded skill sum."
            );
        }
    }
}
