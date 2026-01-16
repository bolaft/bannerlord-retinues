using Retinues.Game.Doctrines.Definitions;
using Retinues.GUI.Services;

namespace Retinues.Game.Doctrines.Catalogs
{
    /// <summary>
    /// Doctrines definitions for the Retinues category.
    /// </summary>
    public static partial class DoctrineCatalog
    {
        public const string Category_Retinues = "cat_retinues";
        public const string RE_Indomitable = "doc_retinues_indomitable";
        public const string RE_BoundByHonor = "doc_retinues_bound_by_honor";
        public const string RE_Vanguard = "doc_retinues_vanguard";
        public const string RE_Immortals = "doc_retinues_immortals";

        public static Doctrine Indomitable => Doctrine.Get(RE_Indomitable);
        public static Doctrine BoundByHonor => Doctrine.Get(RE_BoundByHonor);
        public static Doctrine Vanguard => Doctrine.Get(RE_Vanguard);
        public static Doctrine Immortals => Doctrine.Get(RE_Immortals);

        public static DoctrineCategoryData CategoryRetinues { get; } =
            new()
            {
                Id = Category_Retinues,
                Name = L.T("doctrine_cat_retinues", "Retinues"),
                Doctrines =
                [
                    new DoctrineData
                    {
                        Id = RE_Indomitable,
                        Name = L.T("doctrine_retinues_indomitable", "Indomitable"),
                        Description = L.T(
                            "doctrine_retinues_indomitable_desc",
                            "+10% to retinue health."
                        ),
                        Sprite = "PerkDieHard",
                        Feats =
                        [
                            FeatCatalog.IN_FlawlessExecution,
                            FeatCatalog.IN_AgainstAllOdds,
                            FeatCatalog.IN_HoldTheLine,
                        ],
                    },
                    new DoctrineData
                    {
                        Id = RE_BoundByHonor,
                        Name = L.T("doctrine_retinues_bound_by_honor", "Bound by Honor"),
                        Description = L.T(
                            "doctrine_retinues_bound_by_honor_desc",
                            "+20% to retinue morale."
                        ),
                        Sprite = "PerkGeneral",
                        Feats =
                        [
                            FeatCatalog.BH_HighSpirits,
                            FeatCatalog.BH_BountyHunters,
                            FeatCatalog.BH_SafeTravels,
                        ],
                    },
                    new DoctrineData
                    {
                        Id = RE_Vanguard,
                        Name = L.T("doctrine_retinues_vanguard", "Vanguard"),
                        Description = L.T(
                            "doctrine_retinues_vanguard_desc",
                            "+15% to max retinue number."
                        ),
                        Sprite = "PerkSpearhead",
                        Feats =
                        [
                            FeatCatalog.VA_FirstThroughTheBreach,
                            FeatCatalog.VA_ShockAssault,
                            FeatCatalog.VA_RaiseTheVanguard,
                        ],
                    },
                    new DoctrineData
                    {
                        Id = RE_Immortals,
                        Name = L.T("doctrine_retinues_immortals", "Immortals"),
                        Description = L.T(
                            "doctrine_retinues_immortals_desc",
                            "+20% to retinue survival chance."
                        ),
                        Sprite = "PerkToughness",
                        Feats =
                        [
                            FeatCatalog.IM_PerfectVictory,
                            FeatCatalog.IM_DefyTheTide,
                            FeatCatalog.IM_StillStanding,
                        ],
                    },
                ],
            };
    }
}
