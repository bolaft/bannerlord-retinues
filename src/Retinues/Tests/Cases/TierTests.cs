using Retinues.Domain.Characters.Wrappers;
using Retinues.Settings;

namespace Retinues.Tests.Cases
{
    /// <summary>Tests for the configurable Maximum Troop Tier.</summary>
    public static class TierTests
    {
        [GameTest(
            "EliteMaxTierReflectsConfig",
            "tiers",
            "EliteMaxTier follows the Maximum Troop Tier setting"
        )]
        public static void EliteMaxTierReflectsConfig(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            using (TestConfig.Set(Configuration.MaxTroopTier, 10))
                Tests.AssertEqual(
                    10,
                    WCharacter.EliteMaxTier,
                    "EliteMaxTier equals the configured cap (10)."
                );

            using (TestConfig.Set(Configuration.MaxTroopTier, 6))
                Tests.AssertEqual(
                    6,
                    WCharacter.EliteMaxTier,
                    "EliteMaxTier equals the configured cap (6)."
                );
        }
    }
}
