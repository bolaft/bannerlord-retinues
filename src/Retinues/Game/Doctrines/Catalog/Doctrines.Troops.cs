using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Registers built-in doctrine categories and doctrines.
    /// </summary>
    public static partial class Doctrines
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Troops                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterTroops()
        {
            Feats.RegisterTroopsFeats();

            DoctrinesCatalog.RegisterCategory(
                new DoctrineCategoryDefinition(
                    id: "cat_troops",
                    name: L.T("doctrine_cat_troops", "Troops"),
                    description: L.T(
                        "doctrine_cat_troops_desc",
                        "Doctrines focused on troop access and new troop variants."
                    ),
                    doctrineIds:
                    [
                        "doc_troops_stalwart_militia",
                        "doc_troops_road_wardens",
                        "doc_troops_armed_peasantry",
                        "doc_troops_captains",
                    ]
                )
            );

            RegisterDoctrine(
                id: "doc_troops_stalwart_militia",
                categoryId: "cat_troops",
                indexInCategory: 0,
                nameId: "doctrine_troops_stalwart_militia",
                nameFallback: "Stalwart Militia",
                descId: "doctrine_troops_stalwart_militia_desc",
                descFallback: "Unlocks custom militia troops for your faction's towns and castles.",
                feats: Feats.StalwartMilitia
            );

            RegisterDoctrine(
                id: "doc_troops_road_wardens",
                categoryId: "cat_troops",
                indexInCategory: 1,
                nameId: "doctrine_troops_road_wardens",
                nameFallback: "Road Wardens",
                descId: "doctrine_troops_road_wardens_desc",
                descFallback: "Unlocks custom caravan troop guards.",
                feats: Feats.RoadWardens
            );

            RegisterDoctrine(
                id: "doc_troops_armed_peasantry",
                categoryId: "cat_troops",
                indexInCategory: 2,
                nameId: "doctrine_troops_armed_peasantry",
                nameFallback: "Armed Peasantry",
                descId: "doctrine_troops_armed_peasantry_desc",
                descFallback: "Unlocks custom villager troops for village parties.",
                feats: Feats.ArmedPeasantry
            );

            RegisterDoctrine(
                id: "doc_troops_captains",
                categoryId: "cat_troops",
                indexInCategory: 3,
                nameId: "doctrine_troops_captains",
                nameFallback: "Captains",
                descId: "doctrine_troops_captains_desc",
                descFallback: "Unlocks Captain variants for all regular troops.",
                feats: Feats.Captains
            );
        }
    }
}
