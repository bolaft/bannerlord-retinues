using Retinues.Domain.Characters.Services.Skills;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the skill-budget floor: a troop's per-tier budget is never below the skill sum it
    /// was seeded with, so troops cloned from high-skill templates stay editable.
    /// </summary>
    public static class SkillBudgetTests
    {
        [GameTest(
            "BudgetFlooredToBaseline",
            "skills",
            "GetSkillTotal is floored to the seeded baseline, never below the per-tier total"
        )]
        public static void BudgetFlooredToBaseline(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var wc = sandbox.NewStub();

            // Pin the configured per-tier total by recording a zero baseline (floor inert).
            wc.SkillBaseline = 0;
            int configTotal = SkillRules.GetSkillTotal(wc);

            // A baseline above the per-tier total raises the budget.
            wc.SkillBaseline = configTotal + 1000;
            Tests.AssertEqual(
                configTotal + 1000,
                SkillRules.GetSkillTotal(wc),
                "Budget is floored up to a baseline that exceeds the per-tier total."
            );

            // A baseline below the per-tier total leaves it unchanged (no vanilla regression).
            wc.SkillBaseline = configTotal > 0 ? configTotal - 1 : 0;
            Tests.AssertEqual(
                configTotal,
                SkillRules.GetSkillTotal(wc),
                "A baseline below the per-tier total does not lower the budget."
            );
        }
    }
}
