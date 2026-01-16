using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalogs
{
    /// <summary>
    /// Feat definitions for the Retinues category.
    /// </summary>
    public static partial class FeatCatalog
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Indomitable                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData IN_FlawlessExecution = new()
        {
            Id = "feat_ret_flawless_execution",
            Name = L.T("feat_ret_flawless_execution_name", "Flawless Execution"),
            Description = L.T(
                "feat_ret_flawless_execution_desc",
                "Have your retinues defeat {TARGET} enemy troops of equivalent tier without a single casualty."
            ),
            Target = 20,
            Worth = 30,
            Repeatable = false,
        };

        public static FeatData IN_AgainstAllOdds = new()
        {
            Id = "feat_ret_against_all_odds",
            Name = L.T("feat_ret_against_all_odds_name", "Against All Odds"),
            Description = L.T(
                "feat_ret_against_all_odds_desc",
                "Outnumbered at least 2 to 1, win a battle while fielding only retinues."
            ),
            Target = 1,
            Worth = 30,
            Repeatable = false,
        };

        public static FeatData IN_HoldTheLine = new()
        {
            Id = "feat_ret_hold_the_line",
            Name = L.T("feat_ret_hold_the_line_name", "Hold the Line"),
            Description = L.T(
                "feat_ret_hold_the_line_desc",
                "Win a defensive battle with a retinue-only party."
            ),
            Target = 1,
            Worth = 20,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Bound by Honor                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData BH_HighSpirits = new()
        {
            Id = "feat_ret_high_spirits",
            Name = L.T("feat_ret_high_spirits_name", "High Spirits"),
            Description = L.T(
                "feat_ret_high_spirits_desc",
                "Maintain a retinue-only party's morale above 90 for {TARGET} days."
            ),
            Target = 15,
            Worth = 30,
            Repeatable = false,
        };

        public static FeatData BH_BountyHunters = new()
        {
            Id = "feat_ret_bounty_hunters",
            Name = L.T("feat_ret_bounty_hunters_name", "Bounty Hunters"),
            Description = L.T("feat_ret_bounty_hunters_desc", "Eliminate {TARGET} bandit parties."),
            Target = 5,
            Worth = 30,
            Repeatable = false,
        };

        public static FeatData BH_SafeTravels = new()
        {
            Id = "feat_ret_safe_travels",
            Name = L.T("feat_ret_safe_travels_name", "Safe Travels"),
            Description = L.T(
                "feat_ret_safe_travels_desc",
                "Save a caravan or villager party from an enemy attack."
            ),
            Target = 1,
            Worth = 20,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Vanguard                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData VA_FirstThroughTheBreach = new()
        {
            Id = "feat_ret_first_through_the_breach",
            Name = L.T("feat_ret_first_through_the_breach_name", "First Through the Breach"),
            Description = L.T(
                "feat_ret_first_through_the_breach_desc",
                "Have a retinue get the first melee kill in a siege assault."
            ),
            Target = 1,
            Worth = 50,
            Repeatable = false,
        };

        public static FeatData VA_ShockAssault = new()
        {
            Id = "feat_ret_shock_assault",
            Name = L.T("feat_ret_shock_assault_name", "Shock Assault"),
            Description = L.T(
                "feat_ret_shock_assault_desc",
                "Win a battle of 100 or more combatants using only your retinues."
            ),
            Target = 1,
            Worth = 30,
            Repeatable = true,
        };

        public static FeatData VA_RaiseTheVanguard = new()
        {
            Id = "feat_ret_raise_the_vanguard",
            Name = L.T("feat_ret_raise_the_vanguard_name", "Raise the Vanguard"),
            Description = L.T("feat_ret_raise_the_vanguard_desc", "Hire {TARGET} retinues."),
            Target = 100,
            Worth = 20,
            Repeatable = false,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Immortals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData IM_PerfectVictory = new()
        {
            Id = "feat_ret_perfect_victory",
            Name = L.T("feat_ret_perfect_victory_name", "Perfect Victory"),
            Description = L.T(
                "feat_ret_perfect_victory_desc",
                "Win by yourself against 100 or more enemies without a single death on your side."
            ),
            Target = 1,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData IM_DefyTheTide = new()
        {
            Id = "feat_ret_defy_the_tide",
            Name = L.T("feat_ret_defy_the_tide_name", "Defy the Tide"),
            Description = L.T(
                "feat_ret_defy_the_tide_desc",
                "Win a battle against overwhelming odds while fielding mostly retinues."
            ),
            Target = 1,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData IM_StillStanding = new()
        {
            Id = "feat_ret_still_standing",
            Name = L.T("feat_ret_still_standing_name", "Still Standing"),
            Description = L.T(
                "feat_ret_still_standing_desc",
                "Have {TARGET} retinues survive being struck down in battle."
            ),
            Target = 20,
            Worth = 10,
            Repeatable = true,
        };
    }
}
