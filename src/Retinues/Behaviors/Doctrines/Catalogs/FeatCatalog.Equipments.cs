using Retinues.GUI.Services;

namespace Retinues.Behaviors.Doctrines.Catalogs
{
    /// <summary>
    /// Feat definitions for the Equipments category.
    /// </summary>
    public static partial class FeatCatalog
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Cultural Pride                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData CP_KingSlayer = new()
        {
            Id = "feat_eq_kingslayer",
            Name = L.T("feat_eq_kingslayer_name", "Kingslayer"),
            Description = L.T(
                "feat_eq_kingslayer_desc",
                "Defeat a ruler of a different culture in battle."
            ),
            Target = 1,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData CP_ProudAndStrong = new()
        {
            Id = "feat_eq_proud_and_strong",
            Name = L.T("feat_eq_proud_and_strong_name", "Proud & Strong"),
            Description = L.T(
                "feat_eq_proud_and_strong_desc",
                "Get {TARGET} kills in battle with troops wearing no foreign gear."
            ),
            Target = 100,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData CP_HometownTournament = new()
        {
            Id = "feat_eq_hometown_tournament",
            Name = L.T("feat_eq_hometown_tournament_name", "Hometown Tournament"),
            Description = L.T(
                "feat_eq_hometown_tournament_desc",
                "Win a tournament in a town of your clan's culture."
            ),
            Target = 1,
            Worth = 20,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Golden Legion                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData HG_GoldenLegion = new()
        {
            Id = "feat_eq_golden_legion",
            Name = L.T("feat_eq_golden_legion_name", "Golden Legion"),
            Description = L.T(
                "feat_eq_golden_legion_desc",
                "Field a unit wearing equipment worth over 100,000 denars."
            ),
            Worth = 40,
            Target = 1,
            Repeatable = false,
        };

        public static FeatData HG_BloodMoney = new()
        {
            Id = "feat_eq_blood_money",
            Name = L.T("feat_eq_blood_money_name", "Blood Money"),
            Description = L.T(
                "feat_eq_blood_money_desc",
                "Loot {TARGET} denars on the battlefield."
            ),
            Target = 25000,
            Worth = 30,
            Repeatable = true,
        };

        public static FeatData HG_PaidInFull = new()
        {
            Id = "feat_eq_paid_in_full",
            Name = L.T("feat_eq_paid_in_full_name", "Paid in Full"),
            Description = L.T("feat_eq_paid_in_full_desc", "Pay {TARGET} denars in troop wages."),
            Target = 50000,
            Worth = 15,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Ironclad                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData IR_Ironmen = new()
        {
            Id = "feat_eq_ironmen",
            Name = L.T("feat_eq_ironmen_name", "Ironmen"),
            Description = L.T(
                "feat_eq_ironmen_desc",
                "Win a battle fielding only troops wearing full metal armor."
            ),
            Target = 1,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData IR_HeavyKit = new()
        {
            Id = "feat_eq_heavy_kit",
            Name = L.T("feat_eq_heavy_kit_name", "Heavy Kit"),
            Description = L.T(
                "feat_eq_heavy_kit_desc",
                "Field a unit wearing equipment weighing over 60 kg."
            ),
            Target = 1,
            Worth = 30,
            Repeatable = false,
        };

        public static FeatData IR_TailorMade = new()
        {
            Id = "feat_eq_tailor_made",
            Name = L.T("feat_eq_tailor_made_name", "Tailor Made"),
            Description = L.T("feat_eq_tailor_made_desc", "Own a smithy for {TARGET} days."),
            Target = 30,
            Worth = 30,
            Repeatable = false,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Royal Patronage                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData RP_RoyalStewardship = new()
        {
            Id = "feat_eq_royal_stewardship",
            Name = L.T("feat_eq_royal_stewardship_name", "Royal Stewardship"),
            Description = L.T(
                "feat_eq_royal_stewardship_desc",
                "Have a companion of the same culture as your kingdom govern a kingdom fief for {TARGET} days."
            ),
            Target = 30,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData RP_RoyalHost = new()
        {
            Id = "feat_eq_royal_host",
            Name = L.T("feat_eq_royal_host_name", "Royal Host"),
            Description = L.T(
                "feat_eq_royal_host_desc",
                "Get {TARGET} kills with custom kingdom troops."
            ),
            Target = 1000,
            Worth = 30,
            Repeatable = false,
        };

        public static FeatData RP_RoyalLevy = new()
        {
            Id = "feat_eq_royal_levy",
            Name = L.T("feat_eq_royal_levy_name", "Royal Levy"),
            Description = L.T("feat_eq_royal_levy_desc", "Recruit {TARGET} custom kingdom troops."),
            Target = 100,
            Worth = 15,
            Repeatable = true,
        };
    }
}
