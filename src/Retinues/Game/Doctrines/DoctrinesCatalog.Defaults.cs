using Retinues.UI.Services;

namespace Retinues.Game.Doctrines
{
    public static partial class DoctrinesCatalog
    {
        static partial void RegisterDefaults()
        {
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                   Example Definitions                  //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            RegisterFeat(
                new FeatDefinition(
                    id: "tournament_win",
                    name: L.T("feat_tournament_win_name", "Tournament victory"),
                    description: L.T("feat_tournament_win_desc", "Win a tournament."),
                    target: 1
                )
            );

            RegisterCategory(
                new DoctrineCategoryDefinition(
                    id: "discipline",
                    name: L.T("doctrine_cat_discipline_name", "Discipline"),
                    description: L.T("doctrine_cat_discipline_desc", "Improve troop discipline."),
                    doctrineIds: ["drill_1", "drill_2"]
                )
            );

            RegisterDoctrine(
                new DoctrineDefinition(
                    id: "drill_1",
                    categoryId: "discipline",
                    indexInCategory: 0,
                    name: L.T("doctrine_drill_1_name", "Drill I"),
                    description: L.T("doctrine_drill_1_desc", "Introduce basic drills."),
                    progressTarget: 1,
                    goldCost: 500,
                    influenceCost: 10,
                    feats: [new("tournament_win", progress: 1, required: true)]
                )
            );

            RegisterDoctrine(
                new DoctrineDefinition(
                    id: "drill_2",
                    categoryId: "discipline",
                    indexInCategory: 1,
                    name: L.T("doctrine_drill_2_name", "Drill II"),
                    description: L.T("doctrine_drill_2_desc", "Advanced drills and routines."),
                    progressTarget: 2,
                    goldCost: 1500,
                    influenceCost: 25,
                    feats: [new("tournament_win", progress: 2, required: true)]
                )
            );
        }
    }
}
