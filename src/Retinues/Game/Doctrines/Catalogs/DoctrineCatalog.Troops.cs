using Retinues.Game.Doctrines.Definitions;
using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalogs
{
    /// <summary>
    /// Doctrines definitions for the Troops category.
    /// </summary>
    public static partial class DoctrineCatalog
    {
        public const string Category_Troops = "cat_troops";
        public const string TR_StalwartMilitia = "doc_troops_stalwart_militia";
        public const string TR_RoadWardens = "doc_troops_road_wardens";
        public const string TR_ArmedPeasantry = "doc_troops_armed_peasantry";
        public const string TR_Captains = "doc_troops_captains";

        public static Doctrine StalwartMilitia => Doctrine.Get(TR_StalwartMilitia);
        public static Doctrine RoadWardens => Doctrine.Get(TR_RoadWardens);
        public static Doctrine ArmedPeasantry => Doctrine.Get(TR_ArmedPeasantry);
        public static Doctrine Captains => Doctrine.Get(TR_Captains);

        public static DoctrineCategoryData CategoryTroops { get; } =
            new()
            {
                Id = Category_Troops,
                Name = L.T("doctrine_cat_troops", "Troops"),
                Doctrines =
                [
                    new DoctrineData
                    {
                        Id = TR_StalwartMilitia,
                        Name = L.T("doctrine_troops_stalwart_militia", "Stalwart Militia"),
                        Description = L.T(
                            "doctrine_troops_stalwart_militia_desc",
                            "Unlocks custom militia troops for your faction's towns and castles."
                        ),
                        Feats =
                        [
                            FeatCatalog.SM_TheyShallNotPass,
                            FeatCatalog.SM_WatchersOnTheWalls,
                            FeatCatalog.SM_DefenderOfTheCity,
                        ],
                    },
                    new DoctrineData
                    {
                        Id = TR_RoadWardens,
                        Name = L.T("doctrine_troops_road_wardens", "Road Wardens"),
                        Description = L.T(
                            "doctrine_troops_road_wardens_desc",
                            "Unlocks custom caravan troop guards."
                        ),
                        Feats =
                        [
                            FeatCatalog.RW_TradeNetwork,
                            FeatCatalog.RW_BanditScourge,
                            FeatCatalog.RW_MerchantsFavor,
                        ],
                    },
                    new DoctrineData
                    {
                        Id = TR_ArmedPeasantry,
                        Name = L.T("doctrine_troops_armed_peasantry", "Armed Peasantry"),
                        Description = L.T(
                            "doctrine_troops_armed_peasantry_desc",
                            "Unlocks custom villager troops for village parties."
                        ),
                        Feats =
                        [
                            FeatCatalog.AP_ShieldOfThePeople,
                            FeatCatalog.AP_HeadmansHelp,
                            FeatCatalog.AP_LandownersRequest,
                        ],
                    },
                    new DoctrineData
                    {
                        Id = TR_Captains,
                        Name = L.T("doctrine_troops_captains", "Captains"),
                        Description = L.T(
                            "doctrine_troops_captains_desc",
                            "Unlocks Captain variants for all regular troops."
                        ),
                        Feats =
                        [
                            FeatCatalog.CA_WarriorClass,
                            FeatCatalog.CA_Veterans,
                            FeatCatalog.CA_MeritoriousService,
                        ],
                    },
                ],
            };
    }
}
