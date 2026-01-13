using System.Collections.Generic;
using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Feat definitions and doctrine links for the Loot category.
    /// </summary>
    public static partial class Feats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Register                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RegisterLootFeats()
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
                new("feat_sp_cut_the_head", worth: 40),
                new("feat_sp_blood_price", worth: 30),
                new("feat_sp_high_value_targets", worth: 30),
            ];

        public static IReadOnlyList<DoctrineFeatLink> BattlefieldTithes =>
            [
                new("feat_sp_turn_the_tide", worth: 40),
                new("feat_sp_second_in_command", worth: 20),
                new("feat_sp_allies_favor", worth: 15),
            ];

        public static IReadOnlyList<DoctrineFeatLink> PragmaticScavengers =>
            [
                new("feat_sp_costly_victory", worth: 40),
                new("feat_sp_rescue_mission", worth: 30),
                new("feat_sp_march_together", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> AncestralHeritage =>
            [
                new("feat_sp_cultural_triumph", worth: 40),
                new("feat_sp_homecoming", worth: 40),
                new("feat_sp_ancestral_duty", worth: 10),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Lion's Share                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterLionsShare()
        {
            RegisterFeat(
                id: "feat_sp_cut_the_head",
                name: L.T("feat_sp_cut_the_head_name", "Cut the Head"),
                description: L.T(
                    "feat_sp_cut_the_head_desc",
                    "Personally defeat an enemy lord in battle."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_blood_price",
                name: L.T("feat_sp_blood_price_name", "Blood Price"),
                description: L.T(
                    "feat_sp_blood_price_desc",
                    "Personally defeat {TARGET} enemies in one battle."
                ),
                target: 25,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_high_value_targets",
                name: L.T("feat_sp_high_value_targets_name", "High Value Targets"),
                description: L.T(
                    "feat_sp_high_value_targets_desc",
                    "Personally defeat 5 tier 5+ troops in one battle."
                ),
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
                id: "feat_sp_turn_the_tide",
                name: L.T("feat_sp_turn_the_tide_name", "Turn the Tide"),
                description: L.T(
                    "feat_sp_turn_the_tide_desc",
                    "Turn the tide of a battle involving an allied army."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_second_in_command",
                name: L.T("feat_sp_second_in_command_name", "Second-in-Command"),
                description: L.T(
                    "feat_sp_second_in_command_desc",
                    "Win a battle where you are not the main commander."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_allies_favor",
                name: L.T("feat_sp_allies_favor_name", "Ally's Favor"),
                description: L.T(
                    "feat_sp_allies_favor_desc",
                    "Complete a quest for an allied lord."
                ),
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
                id: "feat_sp_costly_victory",
                name: L.T("feat_sp_costly_victory_name", "Costly Victory"),
                description: L.T(
                    "feat_sp_costly_victory_desc",
                    "Win a battle in which allies suffer over 100 casualties."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_rescue_mission",
                name: L.T("feat_sp_rescue_mission_name", "Rescue Mission"),
                description: L.T(
                    "feat_sp_rescue_mission_desc",
                    "Rescue a captive lord from an enemy party."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_march_together",
                name: L.T("feat_sp_march_together_name", "March Together"),
                description: L.T(
                    "feat_sp_march_together_desc",
                    "Win a battle while part of an allied lord's army."
                ),
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
                id: "feat_sp_cultural_triumph",
                name: L.T("feat_sp_cultural_triumph_name", "Cultural Triumph"),
                description: L.T(
                    "feat_sp_cultural_triumph_desc",
                    "Single-handedly win a battle against an enemy army of a different culture."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_homecoming",
                name: L.T("feat_sp_homecoming_name", "Homecoming"),
                description: L.T(
                    "feat_sp_homecoming_desc",
                    "Capture a fief of your own culture from an enemy kingdom."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_sp_ancestral_duty",
                name: L.T("feat_sp_ancestral_duty_name", "Ancestral Duty"),
                description: L.T(
                    "feat_sp_ancestral_duty_desc",
                    "Complete a quest for a lord of your clan's culture."
                ),
                target: 1,
                repeatable: true
            );
        }
    }
}
