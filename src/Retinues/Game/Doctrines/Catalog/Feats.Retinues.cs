using System.Collections.Generic;

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
                new("feat_ret_indomitable_flawless_equaltier_kills_25", worth: 40),
                new("feat_ret_indomitable_outnumbered_retinues_only_win_1", worth: 30),
                new("feat_ret_indomitable_defensive_retinues_only_win_1", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> BoundByHonor =>
            [
                new("feat_ret_bound_morale_90_days_15", worth: 50),
                new("feat_ret_bound_eliminate_bandits_5", worth: 30),
                new("feat_ret_bound_save_caravan_or_villagers_1", worth: 20),
            ];

        public static IReadOnlyList<DoctrineFeatLink> Vanguard =>
            [
                new("feat_ret_vanguard_first_melee_kill_siege_1", worth: 40),
                new("feat_ret_vanguard_win_100_battle_retinues_only_1", worth: 20),
                new("feat_ret_vanguard_hire_retinues_100", worth: 40),
            ];

        public static IReadOnlyList<DoctrineFeatLink> Immortals =>
            [
                new("feat_ret_immortals_solo_win_100_no_deaths_1", worth: 30),
                new("feat_ret_immortals_overwhelming_odds_retinues_1", worth: 50),
                new("feat_ret_immortals_survive_struck_down_15", worth: 10),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Indomitable                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterIndomitable()
        {
            RegisterFeat(
                id: "feat_ret_indomitable_flawless_equaltier_kills_25",
                nameId: "feat_ret_indomitable_flawless_equaltier_kills_25",
                nameFallback: "Flawless Execution",
                descId: "feat_ret_indomitable_flawless_equaltier_kills_25_desc",
                descFallback: "Have your retinues defeat 25 enemy troops of equivalent tier without a single casualty.",
                target: 25,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_indomitable_outnumbered_retinues_only_win_1",
                nameId: "feat_ret_indomitable_outnumbered_retinues_only_win_1",
                nameFallback: "Against All Odds",
                descId: "feat_ret_indomitable_outnumbered_retinues_only_win_1_desc",
                descFallback: "Outnumbered at least 2 to 1, win a battle while fielding only retinues.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_indomitable_defensive_retinues_only_win_1",
                nameId: "feat_ret_indomitable_defensive_retinues_only_win_1",
                nameFallback: "Hold the Line",
                descId: "feat_ret_indomitable_defensive_retinues_only_win_1_desc",
                descFallback: "Win a defensive battle with a retinue-only party.",
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
                id: "feat_ret_bound_morale_90_days_15",
                nameId: "feat_ret_bound_morale_90_days_15",
                nameFallback: "High Spirits",
                descId: "feat_ret_bound_morale_90_days_15_desc",
                descFallback: "Maintain a retinue-only party's morale above 90 for 15 days.",
                target: 15,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_bound_eliminate_bandits_5",
                nameId: "feat_ret_bound_eliminate_bandits_5",
                nameFallback: "Bounty Hunters",
                descId: "feat_ret_bound_eliminate_bandits_5_desc",
                descFallback: "Eliminate five bandit parties.",
                target: 5,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_bound_save_caravan_or_villagers_1",
                nameId: "feat_ret_bound_save_caravan_or_villagers_1",
                nameFallback: "Safe Travels",
                descId: "feat_ret_bound_save_caravan_or_villagers_1_desc",
                descFallback: "Save a caravan or villager party from an enemy attack.",
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
                id: "feat_ret_vanguard_first_melee_kill_siege_1",
                nameId: "feat_ret_vanguard_first_melee_kill_siege_1",
                nameFallback: "First Through the Breach",
                descId: "feat_ret_vanguard_first_melee_kill_siege_1_desc",
                descFallback: "Have a retinue get the first melee kill in a siege assault.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_vanguard_win_100_battle_retinues_only_1",
                nameId: "feat_ret_vanguard_win_100_battle_retinues_only_1",
                nameFallback: "Shock Assault",
                descId: "feat_ret_vanguard_win_100_battle_retinues_only_1_desc",
                descFallback: "Win a battle of 100 or more combatants using only your retinues.",
                target: 1,
                repeatable: true
            );

            RegisterFeat(
                id: "feat_ret_vanguard_hire_retinues_100",
                nameId: "feat_ret_vanguard_hire_retinues_100",
                nameFallback: "Raise the Vanguard",
                descId: "feat_ret_vanguard_hire_retinues_100_desc",
                descFallback: "Hire 100 retinues.",
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
                id: "feat_ret_immortals_solo_win_100_no_deaths_1",
                nameId: "feat_ret_immortals_solo_win_100_no_deaths_1",
                nameFallback: "Perfect Victory",
                descId: "feat_ret_immortals_solo_win_100_no_deaths_1_desc",
                descFallback: "Win by yourself against 100 or more enemies without a single death on your side.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_immortals_overwhelming_odds_retinues_1",
                nameId: "feat_ret_immortals_overwhelming_odds_retinues_1",
                nameFallback: "Defy the Tide",
                descId: "feat_ret_immortals_overwhelming_odds_retinues_1_desc",
                descFallback: "Win a battle against overwhelming odds while fielding mostly retinues.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_ret_immortals_survive_struck_down_15",
                nameId: "feat_ret_immortals_survive_struck_down_15",
                nameFallback: "Still Standing",
                descId: "feat_ret_immortals_survive_struck_down_15_desc",
                descFallback: "Have 15 retinues survive being struck down in battle.",
                target: 15,
                repeatable: true
            );
        }
    }
}
