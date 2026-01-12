using System.Collections.Generic;

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
                new("feat_tr_iron_discipline_lead_army_days_10", worth: 40),
                new("feat_tr_iron_discipline_defeat_twice_size_faction_only_1", worth: 40),
                new("feat_tr_iron_discipline_upgrade_faction_troops_100", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> SteadfastSoldiers =>
            [
                new("feat_tr_steadfast_max_skills_faction_troops_15", worth: 40),
                new("feat_tr_steadfast_raise_fief_security_60", worth: 40),
                new("feat_tr_steadfast_win_siege_defense_faction_only_1", worth: 20),
            ];

        public static IReadOnlyList<DoctrineFeatLink> MastersAtArms =>
            [
                new("feat_tr_masters_arena_knockouts_50", worth: 40),
                new("feat_tr_masters_elite_faction_kills_1000", worth: 40),
                new("feat_tr_masters_upgrade_elite_faction_troops_100", worth: 20),
            ];

        public static IReadOnlyList<DoctrineFeatLink> AdvancedTactics =>
            [
                new("feat_tr_adv_tactics_win_100plus_even_split_clan_1", worth: 40),
                new("feat_tr_adv_tactics_five_weapon_classes_one_battle_1", worth: 40),
                new("feat_tr_adv_tactics_three_defensive_wins_in_row_3", worth: 20),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Iron Discipline                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterIronDiscipline()
        {
            RegisterFeat(
                id: "feat_tr_iron_discipline_lead_army_days_10",
                nameId: "feat_tr_iron_discipline_lead_army_days_10",
                nameFallback: "General",
                descId: "feat_tr_iron_discipline_lead_army_days_10_desc",
                descFallback: "Lead an army for 10 days in a row.",
                target: 10,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_iron_discipline_defeat_twice_size_faction_only_1",
                nameId: "feat_tr_iron_discipline_defeat_twice_size_faction_only_1",
                nameFallback: "Disciplined Victory",
                descId: "feat_tr_iron_discipline_defeat_twice_size_faction_only_1_desc",
                descFallback: "Defeat a party twice your size using only faction troops.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_iron_discipline_upgrade_faction_troops_100",
                nameId: "feat_tr_iron_discipline_upgrade_faction_troops_100",
                nameFallback: "Forged in Battle",
                descId: "feat_tr_iron_discipline_upgrade_faction_troops_100_desc",
                descFallback: "Upgrade 100 faction troops to the next tier.",
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
                id: "feat_tr_steadfast_max_skills_faction_troops_15",
                nameId: "feat_tr_steadfast_max_skills_faction_troops_15",
                nameFallback: "Peak Performance",
                descId: "feat_tr_steadfast_max_skills_faction_troops_15_desc",
                descFallback: "Max out the skills of 15 faction troops.",
                target: 15,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_steadfast_raise_fief_security_60",
                nameId: "feat_tr_steadfast_raise_fief_security_60",
                nameFallback: "Secure Holdings",
                descId: "feat_tr_steadfast_raise_fief_security_60_desc",
                descFallback: "Raise the security value of a fief to 60%.",
                target: 60,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_steadfast_win_siege_defense_faction_only_1",
                nameId: "feat_tr_steadfast_win_siege_defense_faction_only_1",
                nameFallback: "Hold the Walls",
                descId: "feat_tr_steadfast_win_siege_defense_faction_only_1_desc",
                descFallback: "Win a siege defense fielding only faction troops.",
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
                id: "feat_tr_masters_arena_knockouts_50",
                nameId: "feat_tr_masters_arena_knockouts_50",
                nameFallback: "Brawler",
                descId: "feat_tr_masters_arena_knockouts_50_desc",
                descFallback: "Knock out 50 opponents in the arena.",
                target: 50,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_masters_elite_faction_kills_1000",
                nameId: "feat_tr_masters_elite_faction_kills_1000",
                nameFallback: "Battle Hardened",
                descId: "feat_tr_masters_elite_faction_kills_1000_desc",
                descFallback: "Get 1,000 kills with elite faction troops.",
                target: 1000,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_masters_upgrade_elite_faction_troops_100",
                nameId: "feat_tr_masters_upgrade_elite_faction_troops_100",
                nameFallback: "Distinguished Service",
                descId: "feat_tr_masters_upgrade_elite_faction_troops_100_desc",
                descFallback: "Upgrade 100 elite faction troops to the next tier.",
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
                id: "feat_tr_adv_tactics_win_100plus_even_split_clan_1",
                nameId: "feat_tr_adv_tactics_win_100plus_even_split_clan_1",
                nameFallback: "Combined Arms",
                descId: "feat_tr_adv_tactics_win_100plus_even_split_clan_1_desc",
                descFallback: "Win a battle against over 100 enemies using a party evenly split among infantry, cavalry and ranged clan troops.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_adv_tactics_five_weapon_classes_one_battle_1",
                nameId: "feat_tr_adv_tactics_five_weapon_classes_one_battle_1",
                nameFallback: "Lethal Versatility",
                descId: "feat_tr_adv_tactics_five_weapon_classes_one_battle_1_desc",
                descFallback: "In a single battle, get a kill using five different weapon classes.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_tr_adv_tactics_three_defensive_wins_in_row_3",
                nameId: "feat_tr_adv_tactics_three_defensive_wins_in_row_3",
                nameFallback: "Unyielding Defense",
                descId: "feat_tr_adv_tactics_three_defensive_wins_in_row_3_desc",
                descFallback: "Win three defensive battles in a row.",
                target: 3,
                repeatable: true
            );
        }
    }
}
