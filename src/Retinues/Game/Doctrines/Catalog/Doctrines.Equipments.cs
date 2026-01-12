using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Registers built-in doctrine categories and doctrines.
    /// </summary>
    public static partial class Doctrines
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Armory                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterEquipments()
        {
            Feats.RegisterEquipmentFeats();

            DoctrinesCatalog.RegisterCategory(
                new DoctrineCategoryDefinition(
                    id: "cat_equipments",
                    name: L.T("doctrine_cat_equipments", "Equipments"),
                    description: L.T(
                        "doctrine_cat_equipments_desc",
                        "Doctrines focused on outfitting, logistics, and equipment constraints."
                    ),
                    doctrineIds:
                    [
                        "doc_armory_cultural_pride",
                        "doc_armory_honor_guard",
                        "doc_armory_ironclad",
                        "doc_armory_royal_patronage",
                    ]
                )
            );

            RegisterDoctrine(
                id: "doc_armory_cultural_pride",
                categoryId: "cat_equipments",
                indexInCategory: 0,
                nameId: "doctrine_armory_cultural_pride",
                nameFallback: "Cultural Pride",
                descId: "doctrine_armory_cultural_pride_desc",
                descFallback: "20% rebate when equipping gear from your clan culture.",
                feats: Feats.CulturalPride
            );

            RegisterDoctrine(
                id: "doc_armory_honor_guard",
                categoryId: "cat_equipments",
                indexInCategory: 1,
                nameId: "doctrine_armory_honor_guard",
                nameFallback: "Honor Guard",
                descId: "doctrine_armory_honor_guard_desc",
                descFallback: "+15% to total equipment value limit.",
                feats: Feats.HonorGuard
            );

            RegisterDoctrine(
                id: "doc_armory_ironclad",
                categoryId: "cat_equipments",
                indexInCategory: 2,
                nameId: "doctrine_armory_ironclad",
                nameFallback: "Ironclad",
                descId: "doctrine_armory_ironclad_desc",
                descFallback: "+15% to total equipment weight limit.",
                feats: Feats.Ironclad
            );

            RegisterDoctrine(
                id: "doc_armory_royal_patronage",
                categoryId: "cat_equipments",
                indexInCategory: 3,
                nameId: "doctrine_armory_royal_patronage",
                nameFallback: "Royal Patronage",
                descId: "doctrine_armory_royal_patronage_desc",
                descFallback: "20% rebate when equipping gear from your kingdom culture.",
                feats: Feats.RoyalPatronage
            );
        }
    }
}
