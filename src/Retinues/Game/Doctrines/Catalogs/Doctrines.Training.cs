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
                        Feats = [ID_General, ID_DisciplinedVictory, ID_ForgedInBattle],
                    },
                    new DoctrineData
                    {
                        Id = TR_SteadfastSoldiers,
                        Name = L.T("doctrine_training_steadfast_soldiers", "Steadfast Soldiers"),
                        Description = L.T(
                            "doctrine_training_steadfast_soldiers_desc",
                            "Grants +10 skill points to basic troops."
                        ),
                        Feats = [SS_PeakPerformance, SS_SecureHoldings, SS_HoldTheWalls],
                    },
                    new DoctrineData
                    {
                        Id = TR_MastersAtArms,
                        Name = L.T("doctrine_training_masters_at_arms", "Masters-at-Arms"),
                        Description = L.T(
                            "doctrine_training_masters_at_arms_desc",
                            "Grants +10 skill points to elite troops."
                        ),
                        Feats = [MA_Brawler, MA_BattleHardened, MA_DistinguishedService],
                    },
                    new DoctrineData
                    {
                        Id = TR_AdvancedTactics,
                        Name = L.T("doctrine_training_advanced_tactics", "Advanced Tactics"),
                        Description = L.T(
                            "doctrine_training_advanced_tactics_desc",
                            "+10% skill point gain rate."
                        ),
                        Feats = [AT_CombinedArms, AT_LethalVersatility, AT_UnyieldingDefense],
                    },
                ],
            };
    }
}
