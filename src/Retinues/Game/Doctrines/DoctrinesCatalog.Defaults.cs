using System.Collections.Generic;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    public static partial class DoctrinesCatalog
    {
        static partial void RegisterDefaults()
        {
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                        Feats                           //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            RegisterFeat(
                new FeatDefinition(
                    id: "feat_complete_quests_3",
                    name: new TextObject("Quest Runner"),
                    description: new TextObject("Complete 3 quests."),
                    target: 3,
                    repeatable: false
                )
            );

            RegisterFeat(
                new FeatDefinition(
                    id: "feat_win_tournaments_1",
                    name: new TextObject("Tournament Winner"),
                    description: new TextObject("Win 1 tournament."),
                    target: 1,
                    repeatable: false
                )
            );

            RegisterFeat(
                new FeatDefinition(
                    id: "feat_win_battles_5",
                    name: new TextObject("Battle Tested"),
                    description: new TextObject("Win 5 battles."),
                    target: 5,
                    repeatable: true
                )
            );

            RegisterFeat(
                new FeatDefinition(
                    id: "feat_defeat_nobles_1",
                    name: new TextObject("Noble Breaker"),
                    description: new TextObject("Defeat a noble party in battle (once)."),
                    target: 1,
                    repeatable: false
                )
            );

            RegisterFeat(
                new FeatDefinition(
                    id: "feat_earn_denars_20000",
                    name: new TextObject("Wealth Builder"),
                    description: new TextObject("Earn 20,000 denars."),
                    target: 20000,
                    repeatable: true
                )
            );

            RegisterFeat(
                new FeatDefinition(
                    id: "feat_smith_items_10",
                    name: new TextObject("Blacksmith"),
                    description: new TextObject("Craft 10 items at the smithy."),
                    target: 10,
                    repeatable: false
                )
            );

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                      Categories                         //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            RegisterCategory(
                new DoctrineCategoryDefinition(
                    id: "cat_warfare",
                    name: new TextObject("Warfare"),
                    description: new TextObject(
                        "Doctrines focused on combat readiness and battlefield effectiveness."
                    ),
                    doctrineIds: ["doc_militia_drills", "doc_veteran_corps"]
                )
            );

            RegisterCategory(
                new DoctrineCategoryDefinition(
                    id: "cat_trade",
                    name: new TextObject("Trade"),
                    description: new TextObject(
                        "Doctrines focused on wealth, crafting, and economic power."
                    ),
                    doctrineIds: ["doc_market_network", "doc_master_artisans"]
                )
            );

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                       Doctrines                         //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            // Warfare #1
            RegisterDoctrine(
                new DoctrineDefinition(
                    id: "doc_militia_drills",
                    categoryId: "cat_warfare",
                    indexInCategory: 0,
                    name: new TextObject("Militia Drills"),
                    description: new TextObject(
                        "A disciplined militia fights above its station. Complete feats to build doctrine progress."
                    ),
                    goldCost: 500,
                    influenceCost: 5,
                    feats:
                    [
                        // Required: you must complete it at least once to acquire the doctrine.
                        new DoctrineFeatLink("feat_win_battles_5", worth: 40, required: true),
                        // Optional repeatables that help fill progress to 100.
                        new DoctrineFeatLink("feat_win_tournaments_1", worth: 20, required: false),
                        new DoctrineFeatLink("feat_complete_quests_3", worth: 25, required: false),
                    ]
                )
            );

            // Warfare #2 (unlocks after #1 reaches 100)
            RegisterDoctrine(
                new DoctrineDefinition(
                    id: "doc_veteran_corps",
                    categoryId: "cat_warfare",
                    indexInCategory: 1,
                    name: new TextObject("Veteran Corps"),
                    description: new TextObject(
                        "Battle-hardened veterans become the backbone of any army."
                    ),
                    goldCost: 1500,
                    influenceCost: 15,
                    feats:
                    [
                        // Mix required one-time and repeatable.
                        new DoctrineFeatLink("feat_defeat_nobles_1", worth: 60, required: true),
                        new DoctrineFeatLink("feat_win_battles_5", worth: 30, required: true),
                        new DoctrineFeatLink("feat_win_tournaments_1", worth: 15, required: false),
                    ]
                )
            );

            // Trade #1
            RegisterDoctrine(
                new DoctrineDefinition(
                    id: "doc_market_network",
                    categoryId: "cat_trade",
                    indexInCategory: 0,
                    name: new TextObject("Market Network"),
                    description: new TextObject(
                        "A web of favors and contracts can be as powerful as a sword."
                    ),
                    goldCost: 800,
                    influenceCost: 5,
                    feats:
                    [
                        new DoctrineFeatLink("feat_earn_denars_20000", worth: 50, required: true),
                        new DoctrineFeatLink("feat_complete_quests_3", worth: 20, required: false),
                        // Shared repeatable feat across categories/doctrines to test multi-doctrine progress.
                        new DoctrineFeatLink("feat_win_tournaments_1", worth: 10, required: false),
                    ]
                )
            );

            // Trade #2 (unlocks after #1 reaches 100)
            RegisterDoctrine(
                new DoctrineDefinition(
                    id: "doc_master_artisans",
                    categoryId: "cat_trade",
                    indexInCategory: 1,
                    name: new TextObject("Master Artisans"),
                    description: new TextObject("Quality tools, quality work, quality profit."),
                    goldCost: 2000,
                    influenceCost: 10,
                    feats:
                    [
                        new DoctrineFeatLink("feat_smith_items_10", worth: 35, required: true),
                        new DoctrineFeatLink("feat_earn_denars_20000", worth: 35, required: false),
                        new DoctrineFeatLink("feat_complete_quests_3", worth: 20, required: false),
                    ]
                )
            );
        }
    }
}
