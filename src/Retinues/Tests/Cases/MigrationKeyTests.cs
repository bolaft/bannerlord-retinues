using Retinues.Migration;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Regression tests for the v1 → v2 doctrine/feat key maps. The v1 mod persisted doctrine and
    /// feat keys as Type.FullName in the "Retinues.Doctrines.Catalog" namespace; a temporary
    /// comparison module ("RetinuesLegacy") once leaked its prefix into these maps, which silently
    /// dropped all doctrine/feat progress on migration. These guard the correct prefix.
    /// </summary>
    public static class MigrationKeyTests
    {
        [GameTest(
            "DoctrineKeysUseRetinuesPrefix",
            "migration",
            "v1 doctrine keys (Retinues.* namespace) resolve to their v2 ids"
        )]
        public static void DoctrineKeysUseRetinuesPrefix()
        {
            Tests.AssertEqual(
                "doc_loot_lions_share",
                DoctrineKeyMap.ToV2Id("Retinues.Doctrines.Catalog.LionsShare"),
                "LionsShare maps to its v2 id."
            );
            Tests.AssertEqual(
                "doc_armory_honor_guard",
                DoctrineKeyMap.ToV2Id("Retinues.Doctrines.Catalog.ClanicTraditions"),
                "ClanicTraditions maps to HonorGuard."
            );
            Tests.AssertEqual(
                "doc_retinues_immortals",
                DoctrineKeyMap.ToV2Id("Retinues.Doctrines.Catalog.Immortals"),
                "Immortals maps to its v2 id."
            );

            // The stale temporary-module prefix must NOT resolve (the bug this guards).
            Tests.AssertEqual(
                null,
                DoctrineKeyMap.ToV2Id("RetinuesLegacy.Doctrines.Catalog.LionsShare"),
                "The stale 'RetinuesLegacy' prefix does not resolve."
            );
        }

        [GameTest(
            "FeatKeysUseRetinuesPrefix",
            "migration",
            "v1 feat keys (Retinues.*+Nested namespace) resolve to their v2 feat ids"
        )]
        public static void FeatKeysUseRetinuesPrefix()
        {
            var mappings = FeatKeyMap.GetMappings(
                "Retinues.Doctrines.Catalog.LionsShare+LS_25PersonalKills"
            );
            Tests.AssertNotNull(mappings, "LS_25PersonalKills has a v2 mapping.");
            Tests.AssertEqual(1, mappings.Length, "Single mapping for LS_25PersonalKills.");
            Tests.AssertEqual(
                "feat_sp_blood_price",
                mappings[0].V2FeatId,
                "LS_25PersonalKills maps to feat_sp_blood_price."
            );

            Tests.AssertEqual(
                null,
                FeatKeyMap.GetMappings(
                    "RetinuesLegacy.Doctrines.Catalog.LionsShare+LS_25PersonalKills"
                ),
                "The stale 'RetinuesLegacy' prefix does not resolve for feats."
            );
        }
    }
}
