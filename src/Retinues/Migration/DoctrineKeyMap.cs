using System.Collections.Generic;

namespace Retinues.Migration
{
    /// <summary>
    /// Maps v1 doctrine keys (stored as <c>Type.FullName</c>) to v2 string IDs.
    /// </summary>
    internal static class DoctrineKeyMap
    {
        /// <summary>
        /// Returns the v2 doctrine ID for a v1 <c>Type.FullName</c> key, or
        /// <c>null</c> if there is no v2 equivalent.
        /// </summary>
        internal static string ToV2Id(string v1Key) =>
            s_map.TryGetValue(v1Key, out var id) ? id : null;

        private static readonly Dictionary<string, string> s_map = new()
        {
            // ── Loot doctrines ───────────────────────────────────────────────
            ["RetinuesLegacy.Doctrines.Catalog.LionsShare"] = "doc_loot_lions_share",
            ["RetinuesLegacy.Doctrines.Catalog.BattlefieldTithes"] = "doc_loot_battlefield_tithes",
            ["RetinuesLegacy.Doctrines.Catalog.PragmaticScavengers"] =
                "doc_loot_pragmatic_scavengers",
            ["RetinuesLegacy.Doctrines.Catalog.AncestralHeritage"] = "doc_loot_ancestral_heritage",

            // ── Armory doctrines ─────────────────────────────────────────────
            ["RetinuesLegacy.Doctrines.Catalog.CulturalPride"] = "doc_armory_cultural_pride",
            ["RetinuesLegacy.Doctrines.Catalog.ClanicTraditions"] = "doc_armory_honor_guard",
            ["RetinuesLegacy.Doctrines.Catalog.RoyalPatronage"] = "doc_armory_royal_patronage",
            ["RetinuesLegacy.Doctrines.Catalog.Ironclad"] = "doc_armory_ironclad",

            // ── Troops doctrines ─────────────────────────────────────────────
            ["RetinuesLegacy.Doctrines.Catalog.StalwartMilitia"] = "doc_troops_stalwart_militia",
            ["RetinuesLegacy.Doctrines.Catalog.RoadWardens"] = "doc_troops_road_wardens",
            ["RetinuesLegacy.Doctrines.Catalog.ArmedPeasantry"] = "doc_troops_armed_peasantry",
            ["RetinuesLegacy.Doctrines.Catalog.Captains"] = "doc_troops_captains",

            // ── Training doctrines ───────────────────────────────────────────
            ["RetinuesLegacy.Doctrines.Catalog.IronDiscipline"] = "doc_training_iron_discipline",
            ["RetinuesLegacy.Doctrines.Catalog.SteadfastSoldiers"] =
                "doc_training_steadfast_soldiers",
            ["RetinuesLegacy.Doctrines.Catalog.MastersAtArms"] = "doc_training_masters_at_arms",
            ["RetinuesLegacy.Doctrines.Catalog.AdaptiveTraining"] = "doc_training_advanced_tactics",

            // ── Retinues doctrines ───────────────────────────────────────────
            ["RetinuesLegacy.Doctrines.Catalog.Indomitable"] = "doc_retinues_indomitable",
            ["RetinuesLegacy.Doctrines.Catalog.BoundByHonor"] = "doc_retinues_bound_by_honor",
            ["RetinuesLegacy.Doctrines.Catalog.Vanguard"] = "doc_retinues_vanguard",
            ["RetinuesLegacy.Doctrines.Catalog.Immortals"] = "doc_retinues_immortals",
        };
    }
}
