using System.Collections.Generic;

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
                new("feat_eq_cultural_pride_defeat_ruler_foreign_culture_1", worth: 40),
                new("feat_eq_cultural_pride_kills_no_foreign_gear_100", worth: 30),
                new("feat_eq_cultural_pride_win_tournament_clan_culture_1", worth: 15),
            ];

        public static IReadOnlyList<DoctrineFeatLink> HonorGuard =>
            [
                new("feat_eq_honor_guard_field_unit_value_over_100k_1", worth: 50),
                new("feat_eq_honor_guard_loot_25000_battlefield_25000", worth: 20),
                new("feat_eq_honor_guard_pay_wages_50000_50000", worth: 10),
            ];

        public static IReadOnlyList<DoctrineFeatLink> Ironclad =>
            [
                new("feat_eq_ironclad_field_unit_weight_over_60kg_1", worth: 40),
                new("feat_eq_ironclad_own_smithy_days_30", worth: 30),
                new("feat_eq_ironclad_win_battle_full_steel_only_1", worth: 30),
            ];

        public static IReadOnlyList<DoctrineFeatLink> RoyalPatronage =>
            [
                new("feat_eq_royal_patronage_governor_same_culture_days_30", worth: 40),
                new("feat_eq_royal_patronage_kills_custom_kingdom_troops_1000", worth: 30),
                new("feat_eq_royal_patronage_recruit_custom_kingdom_troops_100", worth: 15),
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Cultural Pride                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterCulturalPride()
        {
            RegisterFeat(
                id: "feat_eq_cultural_pride_defeat_ruler_foreign_culture_1",
                nameId: "feat_eq_cultural_pride_defeat_ruler_foreign_culture_1",
                nameFallback: "King Slayer",
                descId: "feat_eq_cultural_pride_defeat_ruler_foreign_culture_1_desc",
                descFallback: "Defeat a ruler of a different culture in battle.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_cultural_pride_kills_no_foreign_gear_100",
                nameId: "feat_eq_cultural_pride_kills_no_foreign_gear_100",
                nameFallback: "Proud & Strong",
                descId: "feat_eq_cultural_pride_kills_no_foreign_gear_100_desc",
                descFallback: "Get 100 kills in battle with troops wearing no foreign gear.",
                target: 100,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_cultural_pride_win_tournament_clan_culture_1",
                nameId: "feat_eq_cultural_pride_win_tournament_clan_culture_1",
                nameFallback: "Hometown Tournament",
                descId: "feat_eq_cultural_pride_win_tournament_clan_culture_1_desc",
                descFallback: "Win a tournament in a town of your clan's culture.",
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
                id: "feat_eq_honor_guard_field_unit_value_over_100k_1",
                nameId: "feat_eq_honor_guard_field_unit_value_over_100k_1",
                nameFallback: "Golden Legion",
                descId: "feat_eq_honor_guard_field_unit_value_over_100k_1_desc",
                descFallback: "Field a unit wearing equipment worth over 100,000 denars.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_honor_guard_loot_25000_battlefield_25000",
                nameId: "feat_eq_honor_guard_loot_25000_battlefield_25000",
                nameFallback: "Blood Money",
                descId: "feat_eq_honor_guard_loot_25000_battlefield_25000_desc",
                descFallback: "Loot 25,000 denars on the battlefield.",
                target: 25000,
                repeatable: true
            );

            RegisterFeat(
                id: "feat_eq_honor_guard_pay_wages_50000_50000",
                nameId: "feat_eq_honor_guard_pay_wages_50000_50000",
                nameFallback: "Paid in Full",
                descId: "feat_eq_honor_guard_pay_wages_50000_50000_desc",
                descFallback: "Pay 50,000 denars in troop wages.",
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
                id: "feat_eq_ironclad_field_unit_weight_over_60kg_1",
                nameId: "feat_eq_ironclad_field_unit_weight_over_60kg_1",
                nameFallback: "Heavy Kit",
                descId: "feat_eq_ironclad_field_unit_weight_over_60kg_1_desc",
                descFallback: "Field a unit wearing equipment weighing over 60 kg.",
                target: 1,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_ironclad_own_smithy_days_30",
                nameId: "feat_eq_ironclad_own_smithy_days_30",
                nameFallback: "Tailor Made",
                descId: "feat_eq_ironclad_own_smithy_days_30_desc",
                descFallback: "Own a smithy for 30 days.",
                target: 30,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_ironclad_win_battle_full_steel_only_1",
                nameId: "feat_eq_ironclad_win_battle_full_steel_only_1",
                nameFallback: "Steel Wave",
                descId: "feat_eq_ironclad_win_battle_full_steel_only_1_desc",
                descFallback: "Win a battle fielding only troops wearing full steel armor.",
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
                id: "feat_eq_royal_patronage_governor_same_culture_days_30",
                nameId: "feat_eq_royal_patronage_governor_same_culture_days_30",
                nameFallback: "Royal Stewardship",
                descId: "feat_eq_royal_patronage_governor_same_culture_days_30_desc",
                descFallback: "Have a companion of the same culture as your kingdom govern a kingdom fief for 30 days.",
                target: 30,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_royal_patronage_kills_custom_kingdom_troops_1000",
                nameId: "feat_eq_royal_patronage_kills_custom_kingdom_troops_1000",
                nameFallback: "Royal Host",
                descId: "feat_eq_royal_patronage_kills_custom_kingdom_troops_1000_desc",
                descFallback: "Get 1,000 kills with custom kingdom troops.",
                target: 1000,
                repeatable: false
            );

            RegisterFeat(
                id: "feat_eq_royal_patronage_recruit_custom_kingdom_troops_100",
                nameId: "feat_eq_royal_patronage_recruit_custom_kingdom_troops_100",
                nameFallback: "Royal Levy",
                descId: "feat_eq_royal_patronage_recruit_custom_kingdom_troops_100_desc",
                descFallback: "Recruit 100 custom kingdom troops.",
                target: 100,
                repeatable: true
            );
        }
    }
}
