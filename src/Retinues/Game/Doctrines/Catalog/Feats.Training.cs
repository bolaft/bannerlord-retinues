using System.Collections.Generic;
using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Feat definitions and doctrine links for the Training category.
    /// </summary>
    public static partial class Feats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Register                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RegisterTrainingFeats()
        {
            RegisterIronDiscipline();
            RegisterSteadfastSoldiers();
            RegisterMastersAtArms();
            RegisterAdvancedTactics();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Links                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IReadOnlyList<DoctrineFeatLink> IronDiscipline =>
            [
                new("feat_tr_general", worth: 40),
                new("feat_tr_disciplined_victory", worth: 40),
                new("feat_tr_forged_in_battle", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> SteadfastSoldiers =>
            [
                new("feat_tr_peak_performance", worth: 40),
                new("feat_tr_secure_holdings", worth: 40),
                new("feat_tr_hold_the_walls", worth: 20),
            ];

        public static IReadOnlyList<DoctrineFeatLink> MastersAtArms =>
            [
                new("feat_tr_brawler", worth: 40),
                new("feat_tr_battle_hardened", worth: 40),
                new("feat_tr_distinguished_service", worth: 20),
            ];

        public static IReadOnlyList<DoctrineFeatLink> AdvancedTactics =>
            [
                new("feat_tr_combined_arms", worth: 40),
                new("feat_tr_lethal_versatility", worth: 40),
                new("feat_tr_unyielding_defense", worth: 20),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Iron Discipline                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterIronDiscipline()
        {
            RegisterFeat(
                id: "feat_tr_general",
                name: L.T("feat_tr_general_name", "General"),
                description: L.T(
                    "feat_tr_general_desc",
                    "Lead an army for {TARGET} days in a row."
                ),
                target: 10,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_disciplined_victory",
                name: L.T("feat_tr_disciplined_victory_name", "Disciplined Victory"),
                description: L.T(
                    "feat_tr_disciplined_victory_desc",
                    "Defeat a party twice your size using only faction troops."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_forged_in_battle",
                name: L.T("feat_tr_forged_in_battle_name", "Forged in Battle"),
                description: L.T(
                    "feat_tr_forged_in_battle_desc",
                    "Upgrade {TARGET} faction troops to the next tier."
                ),
                target: 100,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Steadfast Soldiers                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterSteadfastSoldiers()
        {
            RegisterFeat(
                id: "feat_tr_peak_performance",
                name: L.T("feat_tr_peak_performance_name", "Peak Performance"),
                description: L.T(
                    "feat_tr_peak_performance_desc",
                    "Max out the skills of {TARGET} faction troops."
                ),
                target: 10,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_secure_holdings",
                name: L.T("feat_tr_secure_holdings_name", "Secure Holdings"),
                description: L.T(
                    "feat_tr_secure_holdings_desc",
                    "Raise the security value of a fief to {TARGET}."
                ),
                target: 60,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_hold_the_walls",
                name: L.T("feat_tr_hold_the_walls_name", "Hold the Walls"),
                description: L.T(
                    "feat_tr_hold_the_walls_desc",
                    "Win a siege defense fielding only faction troops."
                ),
                target: 1,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Masters-at-Arms                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterMastersAtArms()
        {
            RegisterFeat(
                id: "feat_tr_brawler",
                name: L.T("feat_tr_brawler_name", "Brawler"),
                description: L.T(
                    "feat_tr_brawler_desc",
                    "Knock out {TARGET} opponents in the arena."
                ),
                target: 50,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_battle_hardened",
                name: L.T("feat_tr_battle_hardened_name", "Battle Hardened"),
                description: L.T(
                    "feat_tr_battle_hardened_desc",
                    "Get {TARGET} kills with elite faction troops."
                ),
                target: 1000,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_distinguished_service",
                name: L.T("feat_tr_distinguished_service_name", "Distinguished Service"),
                description: L.T(
                    "feat_tr_distinguished_service_desc",
                    "Upgrade {TARGET} elite faction troops to the next tier."
                ),
                target: 100,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Advanced Tactics                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterAdvancedTactics()
        {
            RegisterFeat(
                id: "feat_tr_combined_arms",
                name: L.T("feat_tr_combined_arms_name", "Combined Arms"),
                description: L.T(
                    "feat_tr_combined_arms_desc",
                    "Win a battle against over 100 enemies using a party evenly split among infantry, cavalry and ranged clan troops."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_lethal_versatility",
                name: L.T("feat_tr_lethal_versatility_name", "Lethal Versatility"),
                description: L.T(
                    "feat_tr_lethal_versatility_desc",
                    "In a single battle, get a kill using five different weapon classes."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_unyielding_defense",
                name: L.T("feat_tr_unyielding_defense_name", "Unyielding Defense"),
                description: L.T(
                    "feat_tr_unyielding_defense_desc",
                    "Win {TARGET} defensive battles in a row."
                ),
                target: 3,
                repeatable: true
            );
        }
    }
}
