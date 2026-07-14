using Retinues.Behaviors.Experience;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
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

        [GameTest(
            "XpSourceTogglesControlCapture",
            "xp",
            "Each XP-source setting enables exactly its own source and nothing else"
        )]
        public static void XpSourceTogglesControlCapture(GameTestContext ctx)
        {
            using (TestConfig.Set(Configuration.XpFromManualBattles, true))
            using (TestConfig.Set(Configuration.XpFromAutoResolve, false))
            using (TestConfig.Set(Configuration.XpFromTraining, false))
            {
                Tests.AssertTrue(
                    SkillPointExperienceGain.IsSourceEnabled(SkillXpSource.ManualBattle),
                    "Manual-battle XP is enabled when its toggle is on."
                );
                Tests.AssertFalse(
                    SkillPointExperienceGain.IsSourceEnabled(SkillXpSource.AutoResolve),
                    "Auto-resolve XP is disabled when its toggle is off."
                );
                Tests.AssertFalse(
                    SkillPointExperienceGain.IsSourceEnabled(SkillXpSource.Training),
                    "Training XP is disabled when its toggle is off."
                );
            }

            using (TestConfig.Set(Configuration.XpFromManualBattles, false))
            using (TestConfig.Set(Configuration.XpFromAutoResolve, true))
            using (TestConfig.Set(Configuration.XpFromTraining, true))
            {
                Tests.AssertFalse(
                    SkillPointExperienceGain.IsSourceEnabled(SkillXpSource.ManualBattle),
                    "Manual-battle XP is disabled when its toggle is off."
                );
                Tests.AssertTrue(
                    SkillPointExperienceGain.IsSourceEnabled(SkillXpSource.AutoResolve),
                    "Auto-resolve XP is enabled when its toggle is on."
                );
                Tests.AssertTrue(
                    SkillPointExperienceGain.IsSourceEnabled(SkillXpSource.Training),
                    "Training XP is enabled when its toggle is on."
                );
            }
        }

        [GameTest(
            "SkillPointsAccrueFromEnabledSource",
            "xp",
            "A player-faction troop accrues skill points from an enabled source and none from a disabled one"
        )]
        public static void SkillPointsAccrueFromEnabledSource(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var clan = Player.Clan;
            Tests.AssertNotNull(clan?.Base, "Player has a clan.");

            var previous = clan.MeleeMilitiaTroop;
            var wc = sandbox.NewStub();

            try
            {
                // Assign the stub to the player clan so it counts as a player-faction troop, which is
                // what the conversion requires before it will credit skill points.
                clan.SetMeleeMilitiaTroop(wc);
                WCharacter.InvalidateTroopSourceCaches();
                Tests.AssertTrue(
                    wc.IsPlayerFactionTroop,
                    "A stub assigned to the player clan is a player-faction troop."
                );

                // Enough XP for a few skill points regardless of gain rate.
                int bigXp = SkillPointExperienceGain.GetXpRequiredForSkillPoint(wc.Base) * 3;

                using (TestConfig.Set(Configuration.SkillPointsMustBeEarned, true))
                using (TestConfig.Set(Configuration.SharedSkillPointsPool, false))
                {
                    // Disabled source: not one point, even at a large XP grant. This is the guarantee
                    // the per-source toggles rely on.
                    using (TestConfig.Set(Configuration.XpFromManualBattles, false))
                    {
                        int before = wc.SkillPoints;
                        SkillPointExperienceGain.ApplyXpToSkillPointProgress(
                            wc,
                            Player.Party?.PartyBase,
                            bigXp,
                            SkillXpSource.ManualBattle
                        );
                        Tests.AssertEqual(
                            before,
                            wc.SkillPoints,
                            "A disabled XP source grants no skill points."
                        );
                    }

                    // Enabled source: the same grant crosses at least one skill point. This is the
                    // path that used to freeze for permanently max-rank retinues.
                    using (TestConfig.Set(Configuration.XpFromManualBattles, true))
                    {
                        int before = wc.SkillPoints;
                        SkillPointExperienceGain.ApplyXpToSkillPointProgress(
                            wc,
                            Player.Party?.PartyBase,
                            bigXp,
                            SkillXpSource.ManualBattle
                        );
                        Tests.AssertTrue(
                            wc.SkillPoints > before,
                            "An enabled XP source grants skill points once the threshold is crossed."
                        );
                    }
                }
            }
            finally
            {
                clan.SetMeleeMilitiaTroop(previous);
                WCharacter.InvalidateTroopSourceCaches();
            }
        }
    }
}
