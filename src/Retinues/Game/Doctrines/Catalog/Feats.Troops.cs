using System.Collections.Generic;
using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Feat definitions and doctrine links for the Troops category.
    /// </summary>
    public static partial class Feats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Register                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RegisterTroopsFeats()
        {
            RegisterStalwartMilitia();
            RegisterRoadWardens();
            RegisterArmedPeasantry();
            RegisterCaptains();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Links                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IReadOnlyList<DoctrineFeatLink> StalwartMilitia =>
            [
                new("feat_trp_they_shall_not_pass", worth: 40),
                new("feat_trp_watchers_on_the_walls", worth: 40),
                new("feat_trp_defender_of_the_city", worth: 20),
            ];

        public static IReadOnlyList<DoctrineFeatLink> RoadWardens =>
            [
                new("feat_trp_trade_network", worth: 50),
                new("feat_trp_bandit_scourge", worth: 10),
                new("feat_trp_merchants_favor", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> ArmedPeasantry =>
            [
                new("feat_trp_shield_of_the_people", worth: 50),
                new("feat_trp_headmans_help", worth: 10),
                new("feat_trp_landowners_request", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> Captains =>
            [
                new("feat_trp_warrior_class", worth: 40),
                new("feat_trp_veterans", worth: 40),
                new("feat_trp_meritorious_service", worth: 10),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Stalwart Militia                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterStalwartMilitia()
        {
            RegisterFeat(
                id: "feat_trp_they_shall_not_pass",
                name: L.T("feat_trp_they_shall_not_pass_name", "None Shall Pass"),
                description: L.T(
                    "feat_trp_they_shall_not_pass_desc",
                    "Personally slay {TARGET} assailants during a siege defense."
                ),
                target: 50,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_watchers_on_the_walls",
                name: L.T("feat_trp_watchers_on_the_walls_name", "Watchers on the Walls"),
                description: L.T(
                    "feat_trp_watchers_on_the_walls_desc",
                    "Raise the militia value of a fief to {TARGET}."
                ),
                target: 400,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_defender_of_the_city",
                name: L.T("feat_trp_defender_of_the_city_name", "Defender of the City"),
                description: L.T(
                    "feat_trp_defender_of_the_city_desc",
                    "Defend a city against a besieging army."
                ),
                target: 1,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Road Wardens                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterRoadWardens()
        {
            RegisterFeat(
                id: "feat_trp_trade_network",
                name: L.T("feat_trp_trade_network_name", "Trade Network"),
                description: L.T(
                    "feat_trp_trade_network_desc",
                    "Own three caravans at the same time."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_bandit_scourge",
                name: L.T("feat_trp_bandit_scourge_name", "Bandit Scourge"),
                description: L.T("feat_trp_bandit_scourge_desc", "Clear a bandit hideout."),
                target: 1,
                repeatable: true
            );

            RegisterFeat(
                id: "feat_trp_merchants_favor",
                name: L.T("feat_trp_merchants_favor_name", "Merchant's Favor"),
                description: L.T(
                    "feat_trp_merchants_favor_desc",
                    "Complete a quest for a town merchant notable."
                ),
                target: 1,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Armed Peasantry                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterArmedPeasantry()
        {
            RegisterFeat(
                id: "feat_trp_shield_of_the_people",
                name: L.T("feat_trp_shield_of_the_people_name", "Shield of the People"),
                description: L.T(
                    "feat_trp_shield_of_the_people_desc",
                    "Defend a village against an enemy raid."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_headmans_help",
                name: L.T("feat_trp_headmans_help_name", "Headman's Help"),
                description: L.T(
                    "feat_trp_headmans_help_desc",
                    "Complete a quest for a village headman."
                ),
                target: 1,
                repeatable: true
            );

            RegisterFeat(
                id: "feat_trp_landowners_request",
                name: L.T("feat_trp_landowners_request_name", "Landowner's Request"),
                description: L.T(
                    "feat_trp_landowners_request_desc",
                    "Complete a quest for a village landowner."
                ),
                target: 1,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Captains                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterCaptains()
        {
            RegisterFeat(
                id: "feat_trp_warrior_class",
                name: L.T("feat_trp_warrior_class_name", "Warrior Class"),
                description: L.T(
                    "feat_trp_warrior_class_desc",
                    "Max out the skills of a T6 elite troop."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_veterans",
                name: L.T("feat_trp_veterans_name", "Veterans"),
                description: L.T(
                    "feat_trp_veterans_desc",
                    "Max out the skills of a T5 basic troop."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_meritorious_service",
                name: L.T("feat_trp_meritorious_service_name", "Meritorious Service"),
                description: L.T(
                    "feat_trp_meritorious_service_desc",
                    "Promote {TARGET} faction troops."
                ),
                target: 100,
                repeatable: true
            );
        }
    }
}
