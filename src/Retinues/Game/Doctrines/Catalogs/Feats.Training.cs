using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalogs
{
    /// <summary>
    /// Feat definitions for the Training category.
    /// </summary>
    public static partial class DoctrineCatalog
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Iron Discipline                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData ID_General = new()
        {
            Id = "feat_tr_general",
            Name = L.T("feat_tr_general_name", "General"),
            Description = L.T("feat_tr_general_desc", "Lead an army for {TARGET} days in a row."),
            Target = 10,
            Repeatable = false,
        };

        public static FeatData ID_DisciplinedVictory = new()
        {
            Id = "feat_tr_disciplined_victory",
            Name = L.T("feat_tr_disciplined_victory_name", "Disciplined Victory"),
            Description = L.T(
                "feat_tr_disciplined_victory_desc",
                "Defeat a party twice your size using only faction troops."
            ),
            Target = 1,
            Repeatable = false,
        };

        public static FeatData ID_ForgedInBattle = new()
        {
            Id = "feat_tr_forged_in_battle",
            Name = L.T("feat_tr_forged_in_battle_name", "Forged in Battle"),
            Description = L.T(
                "feat_tr_forged_in_battle_desc",
                "Upgrade {TARGET} faction troops to the next tier."
            ),
            Target = 100,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Steadfast Soldiers                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData SS_PeakPerformance = new()
        {
            Id = "feat_tr_peak_performance",
            Name = L.T("feat_tr_peak_performance_name", "Peak Performance"),
            Description = L.T(
                "feat_tr_peak_performance_desc",
                "Max out the skills of {TARGET} faction troops."
            ),
            Target = 10,
            Repeatable = false,
        };

        public static FeatData SS_SecureHoldings = new()
        {
            Id = "feat_tr_secure_holdings",
            Name = L.T("feat_tr_secure_holdings_name", "Secure Holdings"),
            Description = L.T(
                "feat_tr_secure_holdings_desc",
                "Raise the security value of a fief to {TARGET}."
            ),
            Target = 60,
            Repeatable = false,
        };

        public static FeatData SS_HoldTheWalls = new()
        {
            Id = "feat_tr_hold_the_walls",
            Name = L.T("feat_tr_hold_the_walls_name", "Hold the Walls"),
            Description = L.T(
                "feat_tr_hold_the_walls_desc",
                "Win a siege defense fielding only faction troops."
            ),
            Target = 1,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Masters-at-Arms                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData MA_Brawler = new()
        {
            Id = "feat_tr_brawler",
            Name = L.T("feat_tr_brawler_name", "Brawler"),
            Description = L.T("feat_tr_brawler_desc", "Knock out {TARGET} opponents in the arena."),
            Target = 50,
            Repeatable = false,
        };

        public static FeatData MA_BattleHardened = new()
        {
            Id = "feat_tr_battle_hardened",
            Name = L.T("feat_tr_battle_hardened_name", "Battle Hardened"),
            Description = L.T(
                "feat_tr_battle_hardened_desc",
                "Get {TARGET} kills with elite faction troops."
            ),
            Target = 1000,
            Repeatable = false,
        };

        public static FeatData MA_DistinguishedService = new()
        {
            Id = "feat_tr_distinguished_service",
            Name = L.T("feat_tr_distinguished_service_name", "Distinguished Service"),
            Description = L.T(
                "feat_tr_distinguished_service_desc",
                "Upgrade {TARGET} elite faction troops to the next tier."
            ),
            Target = 100,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Advanced Tactics                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData AT_CombinedArms = new()
        {
            Id = "feat_tr_combined_arms",
            Name = L.T("feat_tr_combined_arms_name", "Combined Arms"),
            Description = L.T(
                "feat_tr_combined_arms_desc",
                "Win a battle against over 100 enemies using a party evenly split among infantry, cavalry and ranged clan troops."
            ),
            Target = 1,
            Repeatable = false,
        };

        public static FeatData AT_LethalVersatility = new()
        {
            Id = "feat_tr_lethal_versatility",
            Name = L.T("feat_tr_lethal_versatility_name", "Lethal Versatility"),
            Description = L.T(
                "feat_tr_lethal_versatility_desc",
                "In a single battle, get a kill using five different weapon classes."
            ),
            Target = 1,
            Repeatable = false,
        };

        public static FeatData AT_UnyieldingDefense = new()
        {
            Id = "feat_tr_unyielding_defense",
            Name = L.T("feat_tr_unyielding_defense_name", "Unyielding Defense"),
            Description = L.T(
                "feat_tr_unyielding_defense_desc",
                "Win {TARGET} defensive battles in a row."
            ),
            Target = 3,
            Repeatable = true,
        };
    }
}
