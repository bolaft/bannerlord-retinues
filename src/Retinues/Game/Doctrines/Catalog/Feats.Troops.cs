using System.Collections.Generic;

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
                new("feat_trp_stalwart_siege_defense_personal_kills_50", worth: 40),
                new("feat_trp_stalwart_raise_militia_to_400", worth: 40),
                new("feat_trp_stalwart_defend_city_vs_siege_1", worth: 20),
            ];

        public static IReadOnlyList<DoctrineFeatLink> RoadWardens =>
            [
                new("feat_trp_road_wardens_own_three_caravans_1", worth: 50),
                new("feat_trp_road_wardens_clear_bandit_hideout_1", worth: 10),
                new("feat_trp_road_wardens_complete_merchant_quest_1", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> ArmedPeasantry =>
            [
                new("feat_trp_armed_peasantry_defend_village_vs_raid_1", worth: 50),
                new("feat_trp_armed_peasantry_complete_headman_quest_1", worth: 10),
                new("feat_trp_armed_peasantry_complete_landowner_quest_1", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> Captains =>
            [
                new("feat_trp_captains_max_skills_t6_elite_1", worth: 40),
                new("feat_trp_captains_max_skills_t5_basic_1", worth: 40),
                new("feat_trp_captains_promote_faction_troops_100", worth: 10),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Stalwart Militia                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterStalwartMilitia()
        {
            RegisterFeat(
                id: "feat_trp_stalwart_siege_defense_personal_kills_50",
                nameId: "feat_trp_stalwart_siege_defense_personal_kills_50",
                nameFallback: "None Shall Pass",
                descId: "feat_trp_stalwart_siege_defense_personal_kills_50_desc",
                descFallback: "Personally slay 50 assailants during a siege defense.",
                target: 50,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_stalwart_raise_militia_to_400",
                nameId: "feat_trp_stalwart_raise_militia_to_400",
                nameFallback: "Watchers on the Walls",
                descId: "feat_trp_stalwart_raise_militia_to_400_desc",
                descFallback: "Raise the militia value of a fief to 400.",
                target: 400,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_stalwart_defend_city_vs_siege_1",
                nameId: "feat_trp_stalwart_defend_city_vs_siege_1",
                nameFallback: "Defender of the City",
                descId: "feat_trp_stalwart_defend_city_vs_siege_1_desc",
                descFallback: "Defend a city against a besieging army.",
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
                id: "feat_trp_road_wardens_own_three_caravans_1",
                nameId: "feat_trp_road_wardens_own_three_caravans_1",
                nameFallback: "Trade Network",
                descId: "feat_trp_road_wardens_own_three_caravans_1_desc",
                descFallback: "Own three caravans at the same time.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_road_wardens_clear_bandit_hideout_1",
                nameId: "feat_trp_road_wardens_clear_bandit_hideout_1",
                nameFallback: "Bandit Scourge",
                descId: "feat_trp_road_wardens_clear_bandit_hideout_1_desc",
                descFallback: "Clear a bandit hideout.",
                target: 1,
                repeatable: true
            );

            RegisterFeat(
                id: "feat_trp_road_wardens_complete_merchant_quest_1",
                nameId: "feat_trp_road_wardens_complete_merchant_quest_1",
                nameFallback: "Merchant's Favor",
                descId: "feat_trp_road_wardens_complete_merchant_quest_1_desc",
                descFallback: "Complete a quest for a town merchant notable.",
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
                id: "feat_trp_armed_peasantry_defend_village_vs_raid_1",
                nameId: "feat_trp_armed_peasantry_defend_village_vs_raid_1",
                nameFallback: "Shield of the People",
                descId: "feat_trp_armed_peasantry_defend_village_vs_raid_1_desc",
                descFallback: "Defend a village against an enemy raid.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_armed_peasantry_complete_headman_quest_1",
                nameId: "feat_trp_armed_peasantry_complete_headman_quest_1",
                nameFallback: "Headman's Help",
                descId: "feat_trp_armed_peasantry_complete_headman_quest_1_desc",
                descFallback: "Complete a quest for a village headman.",
                target: 1,
                repeatable: true
            );

            RegisterFeat(
                id: "feat_trp_armed_peasantry_complete_landowner_quest_1",
                nameId: "feat_trp_armed_peasantry_complete_landowner_quest_1",
                nameFallback: "Landowner's Request",
                descId: "feat_trp_armed_peasantry_complete_landowner_quest_1_desc",
                descFallback: "Complete a quest for a village landowner.",
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
                id: "feat_trp_captains_max_skills_t6_elite_1",
                nameId: "feat_trp_captains_max_skills_t6_elite_1",
                nameFallback: "Warrior Class",
                descId: "feat_trp_captains_max_skills_t6_elite_1_desc",
                descFallback: "Max out the skills of a T6 elite troop.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_captains_max_skills_t5_basic_1",
                nameId: "feat_trp_captains_max_skills_t5_basic_1",
                nameFallback: "Veterans",
                descId: "feat_trp_captains_max_skills_t5_basic_1_desc",
                descFallback: "Max out the skills of a T5 basic troop.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_trp_captains_promote_faction_troops_100",
                nameId: "feat_trp_captains_promote_faction_troops_100",
                nameFallback: "Meritorious Service",
                descId: "feat_trp_captains_promote_faction_troops_100_desc",
                descFallback: "Promote 100 faction troops.",
                target: 100,
                repeatable: true
            );
        }
    }
}
