using System.Collections.Generic;
using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Feat definitions and doctrine links for the Retinues category.
    /// </summary>
    public static partial class Feats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Register                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RegisterRetinueFeats()
        {
            RegisterIndomitable();
            RegisterBoundByHonor();
            RegisterVanguard();
            RegisterImmortals();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Links                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IReadOnlyList<DoctrineFeatLink> Indomitable =>
            [
                new("feat_ret_flawless_execution", worth: 40),
                new("feat_ret_against_all_odds", worth: 30),
                new("feat_ret_hold_the_line", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> BoundByHonor =>
            [
                new("feat_ret_high_spirits", worth: 50),
                new("feat_ret_bounty_hunters", worth: 30),
                new("feat_ret_safe_travels", worth: 20),
            ];

        public static IReadOnlyList<DoctrineFeatLink> Vanguard =>
            [
                new("feat_ret_first_through_the_breach", worth: 40),
                new("feat_ret_shock_assault", worth: 20),
                new("feat_ret_raise_the_vanguard", worth: 40),
            ];

        public static IReadOnlyList<DoctrineFeatLink> Immortals =>
            [
                new("feat_ret_perfect_victory", worth: 30),
                new("feat_ret_defy_the_tide", worth: 50),
                new("feat_ret_still_standing", worth: 10),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Indomitable                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterIndomitable()
        {
            RegisterFeat(
                id: "feat_ret_flawless_execution",
                name: L.T("feat_ret_flawless_execution_name", "Flawless Execution"),
                description: L.T(
                    "feat_ret_flawless_execution_desc",
                    "Have your retinues defeat {TARGET} enemy troops of equivalent tier without a single casualty."
                ),
                target: 20,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_against_all_odds",
                name: L.T("feat_ret_against_all_odds_name", "Against All Odds"),
                description: L.T(
                    "feat_ret_against_all_odds_desc",
                    "Outnumbered at least 2 to 1, win a battle while fielding only retinues."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_hold_the_line",
                name: L.T("feat_ret_hold_the_line_name", "Hold the Line"),
                description: L.T(
                    "feat_ret_hold_the_line_desc",
                    "Win a defensive battle with a retinue-only party."
                ),
                target: 1,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Bound by Honor                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterBoundByHonor()
        {
            RegisterFeat(
                id: "feat_ret_high_spirits",
                name: L.T("feat_ret_high_spirits_name", "High Spirits"),
                description: L.T(
                    "feat_ret_high_spirits_desc",
                    "Maintain a retinue-only party's morale above 90 for {TARGET} days."
                ),
                target: 15,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_bounty_hunters",
                name: L.T("feat_ret_bounty_hunters_name", "Bounty Hunters"),
                description: L.T(
                    "feat_ret_bounty_hunters_desc",
                    "Eliminate {TARGET} bandit parties."
                ),
                target: 5,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_safe_travels",
                name: L.T("feat_ret_safe_travels_name", "Safe Travels"),
                description: L.T(
                    "feat_ret_safe_travels_desc",
                    "Save a caravan or villager party from an enemy attack."
                ),
                target: 1,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Vanguard                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterVanguard()
        {
            RegisterFeat(
                id: "feat_ret_first_through_the_breach",
                name: L.T("feat_ret_first_through_the_breach_name", "First Through the Breach"),
                description: L.T(
                    "feat_ret_first_through_the_breach_desc",
                    "Have a retinue get the first melee kill in a siege assault."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_shock_assault",
                name: L.T("feat_ret_shock_assault_name", "Shock Assault"),
                description: L.T(
                    "feat_ret_shock_assault_desc",
                    "Win a battle of 100 or more combatants using only your retinues."
                ),
                target: 1,
                repeatable: true
            );

            RegisterFeat(
                id: "feat_ret_raise_the_vanguard",
                name: L.T("feat_ret_raise_the_vanguard_name", "Raise the Vanguard"),
                description: L.T("feat_ret_raise_the_vanguard_desc", "Hire {TARGET} retinues."),
                target: 100,
                repeatable: false
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Immortals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterImmortals()
        {
            RegisterFeat(
                id: "feat_ret_perfect_victory",
                name: L.T("feat_ret_perfect_victory_name", "Perfect Victory"),
                description: L.T(
                    "feat_ret_perfect_victory_desc",
                    "Win by yourself against 100 or more enemies without a single death on your side."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_defy_the_tide",
                name: L.T("feat_ret_defy_the_tide_name", "Defy the Tide"),
                description: L.T(
                    "feat_ret_defy_the_tide_desc",
                    "Win a battle against overwhelming odds while fielding mostly retinues."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_still_standing",
                name: L.T("feat_ret_still_standing_name", "Still Standing"),
                description: L.T(
                    "feat_ret_still_standing_desc",
                    "Have {TARGET} retinues survive being struck down in battle."
                ),
                target: 20,
                repeatable: true
            );
        }
    }
}
