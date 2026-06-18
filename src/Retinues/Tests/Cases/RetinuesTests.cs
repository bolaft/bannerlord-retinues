using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game;
using Retinues.Managers;
using Retinues.Troops;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for retinue caps, conversion/rank-up/renown costs, and the Vanguard cap bonus.
    /// Cost/cap functions are read-only; the Vanguard effect is restored by the sandbox.
    /// </summary>
    public static class RetinuesTests
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Caps                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Retinue caps are the party size limit times the configured ratio (no Vanguard).
        /// </summary>
        [GameTest(
            "RetinueCapMatchesRatio",
            "retinues",
            "Retinue caps equal party-size-limit times the configured ratio"
        )]
        public static void RetinueCapMatchesRatio(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            if (DoctrineAPI.IsDoctrineUnlocked<Vanguard>())
                return; // the cap base includes Vanguard's bonus; skip the plain-ratio check

            int partySize = Player.Party?.PartySizeLimit ?? 0;
            Tests.AssertTrue(partySize > 0, "Party size limit is available.");

            Tests.AssertEqual(
                (int)(partySize * (float)Config.MaxEliteRetinueRatio),
                RetinueManager.EliteRetinueCap,
                "Elite retinue cap matches the configured ratio."
            );
            Tests.AssertEqual(
                (int)(partySize * (float)Config.MaxBasicRetinueRatio),
                RetinueManager.BasicRetinueCap,
                "Basic retinue cap matches the configured ratio."
            );
        }

        /// <summary>
        /// Unlocking Vanguard raises the retinue cap base by 15%.
        /// </summary>
        [GameTest(
            "VanguardRaisesRetinueCap",
            "retinues",
            "Vanguard applies +15% to the retinue cap base"
        )]
        public static void VanguardRaisesRetinueCap(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            if (DoctrineAPI.AllDoctrines().Count == 0)
                return; // doctrines disabled; skip
            if (DoctrineAPI.IsDoctrineUnlocked<Vanguard>())
                return; // already unlocked; can't measure a clean delta

            using var sandbox = new TestSandbox();

            int partySize = Player.Party?.PartySizeLimit ?? 0;
            Tests.AssertTrue(partySize > 0, "Party size limit is available.");

            int baseCap = RetinueManager.EliteRetinueCap;

            TestDoctrines.Unlock<Vanguard>();
            Tests.AssertTrue(DoctrineAPI.IsDoctrineUnlocked<Vanguard>(), "Vanguard is unlocked.");

            int boostedBase = (int)(partySize * 1.15f);
            int expected = (int)(boostedBase * (float)Config.MaxEliteRetinueRatio);

            int boostedCap = RetinueManager.EliteRetinueCap;
            Tests.AssertEqual(expected, boostedCap, "Vanguard applies +15% to the cap base.");
            Tests.AssertTrue(boostedCap >= baseCap, "Vanguard does not lower the cap.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Costs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Conversion, renown, and rank-up costs for a retinue scale with its tier.
        /// </summary>
        [GameTest(
            "RetinueCostsScaleWithTier",
            "retinues",
            "Conversion / renown / rank-up costs scale with retinue tier"
        )]
        public static void RetinueCostsScaleWithTier(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var retinue = Player.Clan?.RetinueElite;
            if (retinue == null)
                return; // no retinue in this save; skip
            Tests.AssertTrue(retinue.IsRetinue, "The elite retinue is a retinue.");

            int tier = retinue.Tier <= 0 ? 1 : retinue.Tier;

            Tests.AssertEqual(
                tier * (int)Config.GoldConversionCostPerTier,
                RetinueManager.ConversionGoldCostPerUnit(retinue),
                "Gold conversion cost scales with tier."
            );
            Tests.AssertEqual(
                tier * (int)Config.InfluenceConversionCostPerTier,
                RetinueManager.ConversionInfluenceCostPerUnit(retinue),
                "Influence conversion cost scales with tier."
            );
            Tests.AssertEqual(
                tier * (int)Config.RenownRequiredPerTier,
                RetinueManager.RenownRequiredPerUnit(retinue),
                "Renown requirement scales with tier."
            );
            Tests.AssertEqual(
                retinue.Tier * (int)Config.RankUpCostPerTier,
                RetinueManager.RankUpCost(retinue),
                "Rank-up cost scales with tier."
            );
        }

        /// <summary>
        /// A non-retinue troop has no conversion/renown costs.
        /// </summary>
        [GameTest(
            "NonRetinueHasNoConversionCost",
            "retinues",
            "A non-retinue troop has zero conversion and renown costs"
        )]
        public static void NonRetinueHasNoConversionCost(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

            var troop = faction.RootBasic;
            Tests.AssertNotNull(troop, "Basic root exists.");
            Tests.AssertFalse(troop.IsRetinue, "A regular troop is not a retinue.");

            Tests.AssertEqual(
                0,
                RetinueManager.ConversionGoldCostPerUnit(troop),
                "Non-retinue has no gold conversion cost."
            );
            Tests.AssertEqual(
                0,
                RetinueManager.ConversionInfluenceCostPerUnit(troop),
                "Non-retinue has no influence conversion cost."
            );
            Tests.AssertEqual(
                0,
                RetinueManager.RenownRequiredPerUnit(troop),
                "Non-retinue has no renown requirement."
            );
        }
    }
}
