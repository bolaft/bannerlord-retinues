using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Registers built-in doctrine categories and doctrines.
    /// </summary>
    public static partial class Doctrines
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Loot                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterLoot()
        {
            Feats.RegisterLootFeats();

            DoctrinesCatalog.RegisterCategory(
                new DoctrineCategoryDefinition(
                    id: "cat_loot",
                    name: L.T("doctrine_cat_loot", "Loot"),
                    doctrineIds:
                    [
                        "doc_loot_lions_share",
                        "doc_loot_battlefield_tithes",
                        "doc_loot_pragmatic_scavengers",
                        "doc_loot_ancestral_heritage",
                    ]
                )
            );

            RegisterDoctrine(
                id: "doc_loot_lions_share",
                categoryId: "cat_loot",
                indexInCategory: 0,
                nameId: "doctrine_loot_lions_share",
                nameFallback: "Lion's Share",
                descId: "doctrine_loot_lions_share_desc",
                descFallback: "Hero kills count twice toward unlocking enemy gear.",
                feats: Feats.LionsShare
            );

            RegisterDoctrine(
                id: "doc_loot_battlefield_tithes",
                categoryId: "cat_loot",
                indexInCategory: 1,
                nameId: "doctrine_loot_battlefield_tithes",
                nameFallback: "Battlefield Tithes",
                descId: "doctrine_loot_battlefield_tithes_desc",
                descFallback: "Enemy gear used by troops killed by your allies can also be unlocked.",
                feats: Feats.BattlefieldTithes
            );

            RegisterDoctrine(
                id: "doc_loot_pragmatic_scavengers",
                categoryId: "cat_loot",
                indexInCategory: 2,
                nameId: "doctrine_loot_pragmatic_scavengers",
                nameFallback: "Pragmatic Scavengers",
                descId: "doctrine_loot_pragmatic_scavengers_desc",
                descFallback: "Gear can be unlocked from allied casualties.",
                feats: Feats.PragmaticScavengers
            );

            RegisterDoctrine(
                id: "doc_loot_ancestral_heritage",
                categoryId: "cat_loot",
                indexInCategory: 3,
                nameId: "doctrine_loot_ancestral_heritage",
                nameFallback: "Ancestral Heritage",
                descId: "doctrine_loot_ancestral_heritage_desc",
                descFallback: "All items of your clan's culture are unlocked.",
                feats: Feats.AncestralHeritage
            );
        }
    }
}
