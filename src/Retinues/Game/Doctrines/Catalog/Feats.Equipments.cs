using System.Collections.Generic;
using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Feat definitions and doctrine links for the Equipments category.
    /// </summary>
    public static partial class Feats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Register                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RegisterEquipmentFeats()
        {
            RegisterCulturalPride();
            RegisterHonorGuard();
            RegisterIronclad();
            RegisterRoyalPatronage();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Links                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IReadOnlyList<DoctrineFeatLink> CulturalPride =>
            [
                new("feat_eq_kingslayer", worth: 40),
                new("feat_eq_proud_and_strong", worth: 30),
                new("feat_eq_hometown_tournament", worth: 15),
            ];

        public static IReadOnlyList<DoctrineFeatLink> HonorGuard =>
            [
                new("feat_eq_golden_legion", worth: 50),
                new("feat_eq_blood_money", worth: 20),
                new("feat_eq_paid_in_full", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> Ironclad =>
            [
                new("feat_eq_heavy_kit", worth: 40),
                new("feat_eq_tailor_made", worth: 30),
                new("feat_eq_ironmen", worth: 30),
            ];

        public static IReadOnlyList<DoctrineFeatLink> RoyalPatronage =>
            [
                new("feat_eq_royal_stewardship", worth: 40),
                new("feat_eq_royal_host", worth: 30),
                new("feat_eq_royal_levy", worth: 15),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Cultural Pride                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterCulturalPride()
        {
            RegisterFeat(
                id: "feat_eq_kingslayer",
                name: L.T("feat_eq_kingslayer_name", "Kingslayer"),
                description: L.T(
                    "feat_eq_kingslayer_desc",
                    "Defeat a ruler of a different culture in battle."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_proud_and_strong",
                name: L.T("feat_eq_proud_and_strong_name", "Proud & Strong"),
                description: L.T(
                    "feat_eq_proud_and_strong_desc",
                    "Get {TARGET} kills in battle with troops wearing no foreign gear."
                ),
                target: 100,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_hometown_tournament",
                name: L.T("feat_eq_hometown_tournament_name", "Hometown Tournament"),
                description: L.T(
                    "feat_eq_hometown_tournament_desc",
                    "Win a tournament in a town of your clan's culture."
                ),
                target: 1,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Honor Guard                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterHonorGuard()
        {
            RegisterFeat(
                id: "feat_eq_golden_legion",
                name: L.T("feat_eq_golden_legion_name", "Golden Legion"),
                description: L.T(
                    "feat_eq_golden_legion_desc",
                    "Field a unit wearing equipment worth over 100,000 denars."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_blood_money",
                name: L.T("feat_eq_blood_money_name", "Blood Money"),
                description: L.T(
                    "feat_eq_blood_money_desc",
                    "Loot {TARGET} denars on the battlefield."
                ),
                target: 25000,
                repeatable: true
            );

            RegisterFeat(
                id: "feat_eq_paid_in_full",
                name: L.T("feat_eq_paid_in_full_name", "Paid in Full"),
                description: L.T(
                    "feat_eq_paid_in_full_desc",
                    "Pay {TARGET} denars in troop wages."
                ),
                target: 50000,
                repeatable: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Ironclad                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterIronclad()
        {
            RegisterFeat(
                id: "feat_eq_heavy_kit",
                name: L.T("feat_eq_heavy_kit_name", "Heavy Kit"),
                description: L.T(
                    "feat_eq_heavy_kit_desc",
                    "Field a unit wearing equipment weighing over 60 kg."
                ),
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_tailor_made",
                name: L.T("feat_eq_tailor_made_name", "Tailor Made"),
                description: L.T("feat_eq_tailor_made_desc", "Own a smithy for {TARGET} days."),
                target: 30,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_ironmen",
                name: L.T("feat_eq_ironmen_name", "Ironmen"),
                description: L.T(
                    "feat_eq_ironmen_desc",
                    "Win a battle fielding only troops wearing full metal armor."
                ),
                target: 1,
                repeatable: false
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Royal Patronage                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterRoyalPatronage()
        {
            RegisterFeat(
                id: "feat_eq_royal_stewardship",
                name: L.T("feat_eq_royal_stewardship_name", "Royal Stewardship"),
                description: L.T(
                    "feat_eq_royal_stewardship_desc",
                    "Have a companion of the same culture as your kingdom govern a kingdom fief for {TARGET} days."
                ),
                target: 30,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_royal_host",
                name: L.T("feat_eq_royal_host_name", "Royal Host"),
                description: L.T(
                    "feat_eq_royal_host_desc",
                    "Get {TARGET} kills with custom kingdom troops."
                ),
                target: 1000,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_royal_levy",
                name: L.T("feat_eq_royal_levy_name", "Royal Levy"),
                description: L.T(
                    "feat_eq_royal_levy_desc",
                    "Recruit {TARGET} custom kingdom troops."
                ),
                target: 100,
                repeatable: true
            );
        }
    }
}
