using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Registers built-in doctrine categories and doctrines.
    /// </summary>
    public static partial class Doctrines
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Training                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterTraining()
        {
            Feats.RegisterTrainingFeats();

            DoctrinesCatalog.RegisterCategory(
                new DoctrineCategoryDefinition(
                    id: "cat_training",
                    name: L.T("doctrine_cat_training", "Training"),
                    doctrineIds:
                    [
                        "doc_training_iron_discipline",
                        "doc_training_steadfast_soldiers",
                        "doc_training_masters_at_arms",
                        "doc_training_advanced_tactics",
                    ]
                )
            );

            RegisterDoctrine(
                id: "doc_training_iron_discipline",
                categoryId: "cat_training",
                indexInCategory: 0,
                nameId: "doctrine_training_iron_discipline",
                nameFallback: "Iron Discipline",
                descId: "doctrine_training_iron_discipline_desc",
                descFallback: "Increases skill caps by +5 for all troops.",
                feats: Feats.IronDiscipline
            );

            RegisterDoctrine(
                id: "doc_training_steadfast_soldiers",
                categoryId: "cat_training",
                indexInCategory: 1,
                nameId: "doctrine_training_steadfast_soldiers",
                nameFallback: "Steadfast Soldiers",
                descId: "doctrine_training_steadfast_soldiers_desc",
                descFallback: "Grants +10 skill points to basic troops.",
                feats: Feats.SteadfastSoldiers
            );

            RegisterDoctrine(
                id: "doc_training_masters_at_arms",
                categoryId: "cat_training",
                indexInCategory: 2,
                nameId: "doctrine_training_masters_at_arms",
                nameFallback: "Masters-at-Arms",
                descId: "doctrine_training_masters_at_arms_desc",
                descFallback: "Grants +10 skill points to elite troops.",
                feats: Feats.MastersAtArms
            );

            RegisterDoctrine(
                id: "doc_training_advanced_tactics",
                categoryId: "cat_training",
                indexInCategory: 3,
                nameId: "doctrine_training_advanced_tactics",
                nameFallback: "Advanced Tactics",
                descId: "doctrine_training_advanced_tactics_desc",
                descFallback: "+10% skill point gain rate.",
                feats: Feats.AdvancedTactics
            );
        }
    }
}
