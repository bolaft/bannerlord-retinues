using Retinues.Behaviors.Experience;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the XP -> skill-point conversion. This math has regressed before (skill points
    /// earned far too slowly), so lock in its basic invariants.
    /// </summary>
    public static class XpTests
    {
        [GameTest(
            "XpRequiredScalesWithGainRate",
            "xp",
            "A higher skill-point gain rate lowers the XP needed per skill point"
        )]
        public static void XpRequiredScalesWithGainRate(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(looter, "A vanilla 'looter' troop exists.");

            int lowRate;
            using (TestConfig.Set(Configuration.SkillPointsGainRate, 1f))
                lowRate = SkillPointExperienceGain.GetXpRequiredForSkillPoint(looter);

            int highRate;
            using (TestConfig.Set(Configuration.SkillPointsGainRate, 5f))
                highRate = SkillPointExperienceGain.GetXpRequiredForSkillPoint(looter);

            Tests.AssertTrue(lowRate > 0, "XP required per point is positive at 1x gain rate.");
            Tests.AssertTrue(highRate > 0, "XP required per point is positive at 5x gain rate.");
            Tests.AssertTrue(
                highRate < lowRate,
                $"A higher gain rate needs less XP per skill point (1x={lowRate}, 5x={highRate})."
            );
        }

        [GameTest(
            "XpToUpgradeIsFiniteForTargetlessTroop",
            "xp",
            "A troop with no upgrade targets still reports a finite, positive upgrade XP cost"
        )]
        public static void XpToUpgradeIsFiniteForTargetlessTroop(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var wc = sandbox.NewStub();
            wc.Level = 16; // roughly tier 3
            wc.UpgradeTargets = []; // ensure the target-less estimate path is exercised

            int cost = SkillPointExperienceGain.GetXpRequiredToUpgradeThisUnit(wc.Base);

            Tests.AssertTrue(
                cost > 0,
                $"Upgrade XP cost is positive for a target-less stub (got {cost})."
            );
            Tests.AssertTrue(
                cost < 100000000,
                $"Upgrade XP cost is finite/valid, not the vanilla 'invalid' sentinel (got {cost})."
            );
        }
    }
}
