using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Registers built-in doctrine categories and doctrines.
    /// </summary>
    public static partial class Doctrines
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterRetinues()
        {
            Feats.RegisterRetinueFeats();

            DoctrinesCatalog.RegisterCategory(
                new DoctrineCategoryDefinition(
                    id: "cat_retinues",
                    name: L.T("doctrine_cat_retinues", "Retinues"),
                    doctrineIds:
                    [
                        "doc_retinues_indomitable",
                        "doc_retinues_bound_by_honor",
                        "doc_retinues_vanguard",
                        "doc_retinues_immortals",
                    ]
                )
            );

            RegisterDoctrine(
                id: "doc_retinues_indomitable",
                categoryId: "cat_retinues",
                indexInCategory: 0,
                nameId: "doctrine_retinues_indomitable",
                nameFallback: "Indomitable",
                descId: "doctrine_retinues_indomitable_desc",
                descFallback: "Retinues gain +5 HP each.",
                feats: Feats.Indomitable
            );

            RegisterDoctrine(
                id: "doc_retinues_bound_by_honor",
                categoryId: "cat_retinues",
                indexInCategory: 1,
                nameId: "doctrine_retinues_bound_by_honor",
                nameFallback: "Bound by Honor",
                descId: "doctrine_retinues_bound_by_honor_desc",
                descFallback: "Retinues gain +20% morale.",
                feats: Feats.BoundByHonor
            );

            RegisterDoctrine(
                id: "doc_retinues_vanguard",
                categoryId: "cat_retinues",
                indexInCategory: 2,
                nameId: "doctrine_retinues_vanguard",
                nameFallback: "Vanguard",
                descId: "doctrine_retinues_vanguard_desc",
                descFallback: "Increases retinue cap by +15%.",
                feats: Feats.Vanguard
            );

            RegisterDoctrine(
                id: "doc_retinues_immortals",
                categoryId: "cat_retinues",
                indexInCategory: 3,
                nameId: "doctrine_retinues_immortals",
                nameFallback: "Immortals",
                descId: "doctrine_retinues_immortals_desc",
                descFallback: "Retinues are more likely to be wounded instead of killed (+20% survival chance).",
                feats: Feats.Immortals
            );
        }
    }
}
