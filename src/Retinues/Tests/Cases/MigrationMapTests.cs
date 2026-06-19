using Retinues.Behaviors.Doctrines;
using Retinues.Migration;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Completeness tests for the v1 → v2 doctrine/feat key maps: every v2 id the maps produce must
    /// resolve to a real doctrine/feat in the registry. Catches a typo'd target id, which would
    /// silently drop that doctrine/feat's progress on migration.
    /// </summary>
    public static class MigrationMapTests
    {
        [GameTest(
            "DoctrineMapTargetsExistInRegistry",
            "migration",
            "Every mapped v2 doctrine id resolves to a real doctrine"
        )]
        public static void DoctrineMapTargetsExistInRegistry(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            DoctrinesRegistry.EnsureRegistered();
            if (DoctrinesRegistry.GetDoctrines().Count == 0)
                return; // doctrines disabled in config; nothing to validate

            foreach (var id in DoctrineKeyMap.AllV2Ids)
                Tests.AssertNotNull(
                    DoctrinesRegistry.GetDoctrine(id),
                    $"Mapped v2 doctrine '{id}' exists in the registry."
                );
        }

        [GameTest(
            "FeatMapTargetsExistInRegistry",
            "migration",
            "Every mapped v2 feat id resolves to a real feat"
        )]
        public static void FeatMapTargetsExistInRegistry(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            DoctrinesRegistry.EnsureRegistered();
            if (DoctrinesRegistry.GetFeats().Count == 0)
                return; // doctrines disabled in config; nothing to validate

            foreach (var id in FeatKeyMap.AllV2FeatIds)
                Tests.AssertNotNull(
                    DoctrinesRegistry.GetFeat(id),
                    $"Mapped v2 feat '{id}' exists in the registry."
                );
        }
    }
}
