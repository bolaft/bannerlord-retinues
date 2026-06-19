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

        /// <summary>Every mapped v2 doctrine id (for completeness tests).</summary>
        internal static IReadOnlyCollection<string> AllV2Ids => s_map.Values;

        private static readonly Dictionary<string, string> s_map = new()
        {
            // ── Loot doctrines ───────────────────────────────────────────────
            ["Retinues.Doctrines.Catalog.LionsShare"] = "doc_loot_lions_share",
            ["Retinues.Doctrines.Catalog.BattlefieldTithes"] = "doc_loot_battlefield_tithes",
            ["Retinues.Doctrines.Catalog.PragmaticScavengers"] =
                "doc_loot_pragmatic_scavengers",
            ["Retinues.Doctrines.Catalog.AncestralHeritage"] = "doc_loot_ancestral_heritage",

            // ── Armory doctrines ─────────────────────────────────────────────
            ["Retinues.Doctrines.Catalog.CulturalPride"] = "doc_armory_cultural_pride",
            ["Retinues.Doctrines.Catalog.ClanicTraditions"] = "doc_armory_honor_guard",
            ["Retinues.Doctrines.Catalog.RoyalPatronage"] = "doc_armory_royal_patronage",
            ["Retinues.Doctrines.Catalog.Ironclad"] = "doc_armory_ironclad",

            // ── Troops doctrines ─────────────────────────────────────────────
            ["Retinues.Doctrines.Catalog.StalwartMilitia"] = "doc_troops_stalwart_militia",
            ["Retinues.Doctrines.Catalog.RoadWardens"] = "doc_troops_road_wardens",
            ["Retinues.Doctrines.Catalog.ArmedPeasantry"] = "doc_troops_armed_peasantry",
            ["Retinues.Doctrines.Catalog.Captains"] = "doc_troops_captains",

            // ── Training doctrines ───────────────────────────────────────────
            ["Retinues.Doctrines.Catalog.IronDiscipline"] = "doc_training_iron_discipline",
            ["Retinues.Doctrines.Catalog.SteadfastSoldiers"] =
                "doc_training_steadfast_soldiers",
            ["Retinues.Doctrines.Catalog.MastersAtArms"] = "doc_training_masters_at_arms",
            ["Retinues.Doctrines.Catalog.AdaptiveTraining"] = "doc_training_advanced_tactics",

            // ── Retinues doctrines ───────────────────────────────────────────
            ["Retinues.Doctrines.Catalog.Indomitable"] = "doc_retinues_indomitable",
            ["Retinues.Doctrines.Catalog.BoundByHonor"] = "doc_retinues_bound_by_honor",
            ["Retinues.Doctrines.Catalog.Vanguard"] = "doc_retinues_vanguard",
            ["Retinues.Doctrines.Catalog.Immortals"] = "doc_retinues_immortals",
        };
    }
}
