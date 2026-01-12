using System.Collections.Generic;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Feat definitions and doctrine links for the Spoils category.
    /// </summary>
    public static partial class Feats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Register                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RegisterSpoilsFeats()
        {
            RegisterLionsShare();
            RegisterBattlefieldTithes();
            RegisterPragmaticScavengers();
            RegisterAncestralHeritage();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Links                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IReadOnlyList<DoctrineFeatLink> LionsShare =>
            [
                new("feat_sp_lions_share_defeat_enemy_lord_1", worth: 40),
                new("feat_sp_lions_share_personal_kills_one_battle_25", worth: 30),
                new("feat_sp_lions_share_personal_kills_tier5plus_one_battle_5", worth: 30),
            ];

        public static IReadOnlyList<DoctrineFeatLink> BattlefieldTithes =>
            [
                new("feat_sp_battlefield_tithes_turn_tide_allied_army_1", worth: 40),
                new("feat_sp_battlefield_tithes_win_not_main_commander_1", worth: 20),
                new("feat_sp_battlefield_tithes_complete_quest_allied_lord_1", worth: 15),
            ];

        public static IReadOnlyList<DoctrineFeatLink> PragmaticScavengers =>
            [
                new("feat_sp_pragmatic_scavengers_allied_casualties_over_100_1", worth: 40),
                new("feat_sp_pragmatic_scavengers_rescue_captive_lord_1", worth: 30),
                new("feat_sp_pragmatic_scavengers_win_in_allied_army_1", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> AncestralHeritage =>
            [
                new("feat_sp_ancestral_heritage_solo_win_vs_foreign_culture_army_1", worth: 40),
                new("feat_sp_ancestral_heritage_capture_own_culture_fief_1", worth: 40),
                new("feat_sp_ancestral_heritage_complete_quest_clan_culture_lord_1", worth: 10),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Lion's Share                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterLionsShare()
        {
            RegisterFeat(
                id: "feat_sp_lions_share_defeat_enemy_lord_1",
                nameId: "feat_sp_lions_share_defeat_enemy_lord_1",
                nameFallback: "Cut the Head",
                descId: "feat_sp_lions_share_defeat_enemy_lord_1_desc",
                descFallback: "Personally defeat an enemy lord in battle.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_lions_share_personal_kills_one_battle_25",
                nameId: "feat_sp_lions_share_personal_kills_one_battle_25",
                nameFallback: "Blood Price",
                descId: "feat_sp_lions_share_personal_kills_one_battle_25_desc",
                descFallback: "Personally defeat 25 enemies in one battle.",
                target: 25,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_lions_share_personal_kills_tier5plus_one_battle_5",
                nameId: "feat_sp_lions_share_personal_kills_tier5plus_one_battle_5",
                nameFallback: "High Value Targets",
                descId: "feat_sp_lions_share_personal_kills_tier5plus_one_battle_5_desc",
                descFallback: "Personally defeat 5 tier 5+ troops in one battle.",
                target: 5,
                repeatable: false
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Battlefield Tithes                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterBattlefieldTithes()
        {
            RegisterFeat(
                id: "feat_sp_battlefield_tithes_turn_tide_allied_army_1",
                nameId: "feat_sp_battlefield_tithes_turn_tide_allied_army_1",
                nameFallback: "Turn the Tide",
                descId: "feat_sp_battlefield_tithes_turn_tide_allied_army_1_desc",
                descFallback: "Turn the tide of a battle involving an allied army.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_battlefield_tithes_win_not_main_commander_1",
                nameId: "feat_sp_battlefield_tithes_win_not_main_commander_1",
                nameFallback: "Second-in-Command",
                descId: "feat_sp_battlefield_tithes_win_not_main_commander_1_desc",
                descFallback: "Win a battle where you are not the main commander.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_battlefield_tithes_complete_quest_allied_lord_1",
                nameId: "feat_sp_battlefield_tithes_complete_quest_allied_lord_1",
                nameFallback: "Ally's Favor",
                descId: "feat_sp_battlefield_tithes_complete_quest_allied_lord_1_desc",
                descFallback: "Complete a quest for an allied lord.",
                target: 1,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Pragmatic Scavengers                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterPragmaticScavengers()
        {
            RegisterFeat(
                id: "feat_sp_pragmatic_scavengers_allied_casualties_over_100_1",
                nameId: "feat_sp_pragmatic_scavengers_allied_casualties_over_100_1",
                nameFallback: "Costly Victory",
                descId: "feat_sp_pragmatic_scavengers_allied_casualties_over_100_1_desc",
                descFallback: "Win a battle in which allies suffer over 100 casualties.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_pragmatic_scavengers_rescue_captive_lord_1",
                nameId: "feat_sp_pragmatic_scavengers_rescue_captive_lord_1",
                nameFallback: "Rescue Mission",
                descId: "feat_sp_pragmatic_scavengers_rescue_captive_lord_1_desc",
                descFallback: "Rescue a captive lord from an enemy party.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_pragmatic_scavengers_win_in_allied_army_1",
                nameId: "feat_sp_pragmatic_scavengers_win_in_allied_army_1",
                nameFallback: "March Together",
                descId: "feat_sp_pragmatic_scavengers_win_in_allied_army_1_desc",
                descFallback: "Win a battle while part of an allied lord's army.",
                target: 1,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Ancestral Heritage                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterAncestralHeritage()
        {
            RegisterFeat(
                id: "feat_sp_ancestral_heritage_solo_win_vs_foreign_culture_army_1",
                nameId: "feat_sp_ancestral_heritage_solo_win_vs_foreign_culture_army_1",
                nameFallback: "Cultural Triumph",
                descId: "feat_sp_ancestral_heritage_solo_win_vs_foreign_culture_army_1_desc",
                descFallback: "Single-handedly win a battle against an enemy army of a different culture.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_ancestral_heritage_capture_own_culture_fief_1",
                nameId: "feat_sp_ancestral_heritage_capture_own_culture_fief_1",
                nameFallback: "Homecoming",
                descId: "feat_sp_ancestral_heritage_capture_own_culture_fief_1_desc",
                descFallback: "Capture a fief of your own culture from an enemy kingdom.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_ancestral_heritage_complete_quest_clan_culture_lord_1",
                nameId: "feat_sp_ancestral_heritage_complete_quest_clan_culture_lord_1",
                nameFallback: "Ancestral Duty",
                descId: "feat_sp_ancestral_heritage_complete_quest_clan_culture_lord_1_desc",
                descFallback: "Complete a quest for a lord of your clan's culture.",
                target: 1,
                repeatable: true
            );
        }
    }
}
