using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalogs
{
    /// <summary>
    /// Feat definitions for the Loot category.
    /// </summary>
    public static partial class FeatCatalog
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Lion's Share                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData LS_CutTheHead = new()
        {
            Id = "feat_sp_cut_the_head",
            Name = L.T("feat_sp_cut_the_head_name", "Cut the Head"),
            Description = L.T(
                "feat_sp_cut_the_head_desc",
                "Personally defeat an enemy lord in battle."
            ),
            Target = 1,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData LS_BloodPrice = new()
        {
            Id = "feat_sp_blood_price",
            Name = L.T("feat_sp_blood_price_name", "Blood Price"),
            Description = L.T(
                "feat_sp_blood_price_desc",
                "Personally defeat {TARGET} enemies in one battle."
            ),
            Target = 25,
            Worth = 30,
            Repeatable = false,
        };

        public static FeatData LS_HighValueTargets = new()
        {
            Id = "feat_sp_high_value_targets",
            Name = L.T("feat_sp_high_value_targets_name", "High Value Targets"),
            Description = L.T(
                "feat_sp_high_value_targets_desc",
                "Personally defeat 5 tier 5+ troops in one battle."
            ),
            Target = 5,
            Worth = 30,
            Repeatable = false,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Battlefield Tithes                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData BT_TurnTheTide = new()
        {
            Id = "feat_sp_turn_the_tide",
            Name = L.T("feat_sp_turn_the_tide_name", "Turn the Tide"),
            Description = L.T(
                "feat_sp_turn_the_tide_desc",
                "Turn the tide of a battle involving an allied army."
            ),
            Target = 1,
            Worth = 35,
            Repeatable = false,
        };

        public static FeatData BT_SecondInCommand = new()
        {
            Id = "feat_sp_second_in_command",
            Name = L.T("feat_sp_second_in_command_name", "Second-in-Command"),
            Description = L.T(
                "feat_sp_second_in_command_desc",
                "Win a battle where you are not the main commander."
            ),
            Target = 1,
            Worth = 35,
            Repeatable = false,
        };

        public static FeatData BT_AlliesFavor = new()
        {
            Id = "feat_sp_allies_favor",
            Name = L.T("feat_sp_allies_favor_name", "Ally's Favor"),
            Description = L.T("feat_sp_allies_favor_desc", "Complete a quest for an allied lord."),
            Target = 1,
            Worth = 15,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Pragmatic Scavengers                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData PR_CostlyVictory = new()
        {
            Id = "feat_sp_costly_victory",
            Name = L.T("feat_sp_costly_victory_name", "Costly Victory"),
            Description = L.T(
                "feat_sp_costly_victory_desc",
                "Win a battle in which allies suffer over 100 casualties."
            ),
            Target = 1,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData PR_RescueMission = new()
        {
            Id = "feat_sp_rescue_mission",
            Name = L.T("feat_sp_rescue_mission_name", "Rescue Mission"),
            Description = L.T(
                "feat_sp_rescue_mission_desc",
                "Rescue a captive lord from an enemy party."
            ),
            Target = 1,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData PR_MarchTogether = new()
        {
            Id = "feat_sp_march_together",
            Name = L.T("feat_sp_march_together_name", "March Together"),
            Description = L.T(
                "feat_sp_march_together_desc",
                "Win a battle while part of an allied lord's army."
            ),
            Target = 1,
            Worth = 10,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Ancestral Heritage                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData AN_CulturalTriumph = new()
        {
            Id = "feat_sp_cultural_triumph",
            Name = L.T("feat_sp_cultural_triumph_name", "Cultural Triumph"),
            Description = L.T(
                "feat_sp_cultural_triumph_desc",
                "Single-handedly win a battle against an enemy army of a different culture."
            ),
            Target = 1,
            Worth = 50,
            Repeatable = false,
        };

        public static FeatData AN_Homecoming = new()
        {
            Id = "feat_sp_homecoming",
            Name = L.T("feat_sp_homecoming_name", "Homecoming"),
            Description = L.T(
                "feat_sp_homecoming_desc",
                "Capture a fief of your own culture from an enemy kingdom."
            ),
            Target = 1,
            Worth = 50,
            Repeatable = false,
        };

        public static FeatData AN_AncestralDuty = new()
        {
            Id = "feat_sp_ancestral_duty",
            Name = L.T("feat_sp_ancestral_duty_name", "Ancestral Duty"),
            Description = L.T(
                "feat_sp_ancestral_duty_desc",
                "Complete a quest for a lord of your clan's culture."
            ),
            Target = 1,
            Worth = 10,
            Repeatable = true,
        };
    }
}
