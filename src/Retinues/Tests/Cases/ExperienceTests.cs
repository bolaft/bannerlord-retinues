using Retinues.Configuration;
using Retinues.Features.Experience;
using Retinues.Troops;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the troop XP pool economy (set/add/floor/spend). The XP pool is restored by the
    /// sandbox.
    /// </summary>
    public static class ExperienceTests
    {
        /// <summary>
        /// XP pools set/add/floor at zero, and TrySpend respects the available budget.
        /// </summary>
        [GameTest(
            "TroopXpEconomy",
            "experience",
            "XP pools add/floor at zero and TrySpend respects the budget"
        )]
        public static void TroopXpEconomy(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            Tests.AssertNotNull(TroopXpBehavior.Instance, "TroopXpBehavior is registered.");

            using var sandbox = new TestSandbox();
            using var _shared = TestConfig.Set(Config.SharedXpPool, false);

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);
            var troop = faction.RootBasic;
            Tests.AssertNotNull(troop, "Basic root exists.");

            TroopXpBehavior.Set(troop, 100);
            Tests.AssertEqual(100, TroopXpBehavior.Get(troop), "Set then Get returns the value.");

            TroopXpBehavior.Add(troop, 50);
            Tests.AssertEqual(150, TroopXpBehavior.Get(troop), "Add increases the pool.");

            TroopXpBehavior.Add(troop, -1000);
            Tests.AssertEqual(0, TroopXpBehavior.Get(troop), "The pool floors at zero.");

            // TrySpend only deducts when skill XP costs are enabled.
            TroopXpBehavior.Set(troop, 100);
            if (Config.SkillXpCostPerPoint > 0 || Config.BaseSkillXpCost > 0)
            {
                Tests.AssertTrue(
                    TroopXpBehavior.TrySpend(troop, 80),
                    "Spending within budget succeeds."
                );
                Tests.AssertEqual(20, TroopXpBehavior.Get(troop), "Pool reduced after spend.");

                Tests.AssertFalse(
                    TroopXpBehavior.TrySpend(troop, 50),
                    "Overspending fails."
                );
                Tests.AssertEqual(20, TroopXpBehavior.Get(troop), "Pool unchanged after a failed spend.");
            }
        }
    }
}
