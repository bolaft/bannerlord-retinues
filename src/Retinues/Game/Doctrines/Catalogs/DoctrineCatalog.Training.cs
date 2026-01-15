using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalogs
{
    /// <summary>
    /// Doctrines definitions for the Training category.
    /// </summary>
    public static partial class DoctrineCatalog
    {
        public const string Category_Training = "cat_training";
        public const string TR_IronDiscipline = "doc_training_iron_discipline";
        public const string TR_SteadfastSoldiers = "doc_training_steadfast_soldiers";
        public const string TR_MastersAtArms = "doc_training_masters_at_arms";
        public const string TR_AdvancedTactics = "doc_training_advanced_tactics";

        public static Doctrine IronDiscipline => Doctrine.Get(TR_IronDiscipline);
        public static Doctrine SteadfastSoldiers => Doctrine.Get(TR_SteadfastSoldiers);
        public static Doctrine MastersAtArms => Doctrine.Get(TR_MastersAtArms);
        public static Doctrine AdvancedTactics => Doctrine.Get(TR_AdvancedTactics);

        public static DoctrineCategoryData CategoryTraining { get; } =
            new()
            {
                Id = Category_Training,
                Name = L.T("doctrine_cat_training", "Training"),
                Doctrines =
                [
                    new DoctrineData
                    {
                        Id = TR_IronDiscipline,
                        Name = L.T("doctrine_training_iron_discipline", "Iron Discipline"),
                        Description = L.T(
                            "doctrine_training_iron_discipline_desc",
                            "Increases skill caps by +5 for all troops."
                        ),
                        Feats =
                        [
                            FeatCatalog.ID_General,
                            FeatCatalog.ID_DisciplinedVictory,
                            FeatCatalog.ID_ForgedInBattle,
                        ],
                    },
                    new DoctrineData
                    {
                        Id = TR_SteadfastSoldiers,
                        Name = L.T("doctrine_training_steadfast_soldiers", "Steadfast Soldiers"),
                        Description = L.T(
                            "doctrine_training_steadfast_soldiers_desc",
                            "Grants +10 skill points to basic troops."
                        ),
                        Feats =
                        [
                            FeatCatalog.SS_PeakPerformance,
                            FeatCatalog.SS_SecureHoldings,
                            FeatCatalog.SS_HoldTheWalls,
                        ],
                    },
                    new DoctrineData
                    {
                        Id = TR_MastersAtArms,
                        Name = L.T("doctrine_training_masters_at_arms", "Masters-at-Arms"),
                        Description = L.T(
                            "doctrine_training_masters_at_arms_desc",
                            "Grants +10 skill points to elite troops."
                        ),
                        Feats =
                        [
                            FeatCatalog.MA_Brawler,
                            FeatCatalog.MA_BattleHardened,
                            FeatCatalog.MA_DistinguishedService,
                        ],
                    },
                    new DoctrineData
                    {
                        Id = TR_AdvancedTactics,
                        Name = L.T("doctrine_training_advanced_tactics", "Advanced Tactics"),
                        Description = L.T(
                            "doctrine_training_advanced_tactics_desc",
                            "+10% skill point gain rate."
                        ),
                        Feats =
                        [
                            FeatCatalog.AT_CombinedArms,
                            FeatCatalog.AT_LethalVersatility,
                            FeatCatalog.AT_UnyieldingDefense,
                        ],
                    },
                ],
            };
    }
}
