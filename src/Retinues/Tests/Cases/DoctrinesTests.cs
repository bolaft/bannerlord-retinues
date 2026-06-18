using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Managers;
using Retinues.Troops;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the doctrine catalog, feat progression API, and a doctrine effect (Iron
    /// Discipline's skill-cap bonus). Doctrine state is restored by the sandbox.
    /// </summary>
    public static class DoctrinesTests
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Catalog                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The doctrine catalog is discovered with grid positions and known doctrines present.
        /// </summary>
        [GameTest(
            "DoctrineCatalogDiscovery",
            "doctrines",
            "Doctrine catalog is discovered with grid positions and known doctrines"
        )]
        public static void DoctrineCatalogDiscovery(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var docs = DoctrineAPI.AllDoctrines();
            if (docs.Count == 0)
                return; // doctrines disabled in this save; nothing to assert

            Tests.AssertTrue(docs.Count >= 16, $"Catalog has many doctrines (got {docs.Count}).");
            Tests.AssertNotNull(
                DoctrineAPI.GetDoctrine<IronDiscipline>(),
                "Iron Discipline is in the catalog."
            );

            foreach (var d in docs)
            {
                Tests.AssertTrue(
                    d.Column >= 0 && d.Row >= 0,
                    $"Doctrine '{d.Name}' has a valid grid position."
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Feats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Feat progress accumulates, completes at the target, and advancing clamps at the target.
        /// </summary>
        [GameTest(
            "FeatProgressAndCompletion",
            "doctrines",
            "Feat progress accumulates, completes at target, and advancing clamps at target"
        )]
        public static void FeatProgressAndCompletion(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            using var _feats = TestConfig.Set(Config.EnableFeatRequirements, true);

            var featKey = FirstFeatWithTarget(out int target);
            if (featKey == null)
                return; // doctrines disabled; skip

            DoctrineAPI.SetFeatProgress(featKey, 0);
            Tests.AssertEqual(0, DoctrineAPI.GetFeatProgress(featKey), "Progress set to zero.");
            Tests.AssertFalse(DoctrineAPI.IsFeatComplete(featKey), "Not complete at zero.");

            DoctrineAPI.SetFeatProgress(featKey, target);
            Tests.AssertEqual(target, DoctrineAPI.GetFeatProgress(featKey), "Progress set to target.");
            Tests.AssertTrue(DoctrineAPI.IsFeatComplete(featKey), "Complete at target.");

            // Advancing past the target clamps at the target.
            DoctrineAPI.SetFeatProgress(featKey, target > 0 ? target - 1 : 0);
            int after = DoctrineAPI.AdvanceFeat(featKey, 100);
            Tests.AssertEqual(target, after, "AdvanceFeat clamps at the target.");
            Tests.AssertTrue(DoctrineAPI.IsFeatComplete(featKey), "Complete after advancing.");
        }

        /// <summary>
        /// With feat requirements disabled, targets read as zero and feats are auto-complete.
        /// </summary>
        [GameTest(
            "FeatRequirementsDisabledGate",
            "doctrines",
            "Disabling feat requirements zeroes targets and auto-completes feats"
        )]
        public static void FeatRequirementsDisabledGate(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            string featKey;
            using (TestConfig.Set(Config.EnableFeatRequirements, true))
            {
                featKey = FirstFeatWithTarget(out _);
            }
            if (featKey == null)
                return; // doctrines disabled; skip

            using (TestConfig.Set(Config.EnableFeatRequirements, false))
            {
                Tests.AssertEqual(
                    0,
                    DoctrineAPI.GetFeatTarget(featKey),
                    "Target is zero when feat requirements are disabled."
                );
                Tests.AssertTrue(
                    DoctrineAPI.IsFeatComplete(featKey),
                    "Feats are auto-complete when requirements are disabled."
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Effect                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unlocking Iron Discipline raises a regular troop's skill cap by exactly 5.
        /// </summary>
        [GameTest(
            "IronDisciplineRaisesSkillCap",
            "doctrines",
            "Iron Discipline raises a regular troop's skill cap by 5"
        )]
        public static void IronDisciplineRaisesSkillCap(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            if (DoctrineAPI.AllDoctrines().Count == 0)
                return; // doctrines disabled; skip
            if (DoctrineAPI.IsDoctrineUnlocked<IronDiscipline>())
                return; // already unlocked; can't measure a clean delta

            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

            var troop = faction.RootBasic;
            Tests.AssertNotNull(troop, "Basic root exists.");
            Tests.AssertFalse(troop.IsRetinue, "Troop is a regular (not a retinue).");

            int baseCap = SkillManager.SkillCapByTier(troop);

            TestDoctrines.Unlock<IronDiscipline>();
            Tests.AssertTrue(
                DoctrineAPI.IsDoctrineUnlocked<IronDiscipline>(),
                "Iron Discipline is unlocked."
            );

            int boostedCap = SkillManager.SkillCapByTier(troop);
            Tests.AssertEqual(baseCap + 5, boostedCap, "Iron Discipline raises the skill cap by 5.");
        }

        /// <summary>
        /// Unlocking Steadfast Soldiers raises a regular troop's skill point total by exactly 10.
        /// </summary>
        [GameTest(
            "SteadfastSoldiersRaisesSkillTotal",
            "doctrines",
            "Steadfast Soldiers raises a regular troop's skill total by 10"
        )]
        public static void SteadfastSoldiersRaisesSkillTotal(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            if (DoctrineAPI.AllDoctrines().Count == 0)
                return; // doctrines disabled; skip
            if (DoctrineAPI.IsDoctrineUnlocked<SteadfastSoldiers>())
                return; // already unlocked; can't measure a clean delta

            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

            var troop = faction.RootBasic;
            Tests.AssertNotNull(troop, "Basic root exists.");
            Tests.AssertFalse(troop.IsRetinue, "Troop is a regular (not a retinue).");

            int baseTotal = SkillManager.SkillTotalByTier(troop);

            TestDoctrines.Unlock<SteadfastSoldiers>();
            Tests.AssertTrue(
                DoctrineAPI.IsDoctrineUnlocked<SteadfastSoldiers>(),
                "Steadfast Soldiers is unlocked."
            );

            int boostedTotal = SkillManager.SkillTotalByTier(troop);
            Tests.AssertEqual(
                baseTotal + 10,
                boostedTotal,
                "Steadfast Soldiers raises the skill total by 10."
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the key of the first feat with a positive target, or null if none/disabled.
        /// </summary>
        private static string FirstFeatWithTarget(out int target)
        {
            target = 0;
            foreach (var d in DoctrineAPI.AllDoctrines())
            {
                if (d.Feats == null)
                    continue;
                foreach (var f in d.Feats)
                {
                    int t = DoctrineAPI.GetFeatTarget(f.Key);
                    if (t > 0)
                    {
                        target = t;
                        return f.Key;
                    }
                }
            }
            return null;
        }
    }
}
