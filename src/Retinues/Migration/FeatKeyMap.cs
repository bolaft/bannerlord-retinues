using System;
using System.Collections.Generic;

namespace Retinues.Migration
{
    /// <summary>
    /// Maps a v1 feat key (<c>Type.FullName</c> with <c>+</c> separator) to one or
    /// more v2 feat IDs together with the original and new completion targets.
    /// <para/>
    /// Used by <see cref="LegacyMigrationCoordinator"/> to proportionally scale raw
    /// v1 feat progress into the v2 feat system.
    /// </summary>
    internal static class FeatKeyMap
    {
        internal readonly struct Mapping
        {
            internal readonly string V2FeatId;

            /// <summary>v1 feat completion threshold (as declared in RetinuesLegacy).</summary>
            internal readonly int V1Target;

            /// <summary>v2 feat <c>Target</c> property value.</summary>
            internal readonly int V2Target;

            internal Mapping(string v2FeatId, int v1Target, int v2Target)
            {
                V2FeatId = v2FeatId;
                V1Target = v1Target;
                V2Target = v2Target;
            }
        }

        /// <summary>
        /// Returns the v2 feat mapping(s) for the given v1 feat key, or
        /// <c>null</c> if no equivalent exists.
        /// </summary>
        internal static Mapping[] GetMappings(string v1Key) =>
            _map.TryGetValue(v1Key, out var m) ? m : null;

        // ─── helpers ──────────────────────────────────────────────────────────

        private static Mapping[] One(string id, int v1t, int v2t) =>
            new[] { new Mapping(id, v1t, v2t) };

        private static Mapping[] Two(
            string id1,
            int v1t1,
            int v2t1,
            string id2,
            int v1t2,
            int v2t2
        ) => new[] { new Mapping(id1, v1t1, v2t1), new Mapping(id2, v1t2, v2t2) };

        // ─── map ──────────────────────────────────────────────────────────────

        private static readonly Dictionary<string, Mapping[]> _map = new(StringComparer.Ordinal)
        {
            // ── Loot ─────────────────────────────────────────────────────────

            // LionsShare → doc_loot_lions_share
            ["RetinuesLegacy.Doctrines.Catalog.LionsShare+LS_25PersonalKills"] = One(
                "feat_sp_blood_price",
                25,
                25
            ),
            ["RetinuesLegacy.Doctrines.Catalog.LionsShare+LS_5Tier5Plus"] = One(
                "feat_sp_high_value_targets",
                5,
                5
            ),
            ["RetinuesLegacy.Doctrines.Catalog.LionsShare+LS_KillEnemyLord"] = One(
                "feat_sp_cut_the_head",
                1,
                1
            ),

            // BattlefieldTithes → doc_loot_battlefield_tithes
            // BT_QuestForAlliedLord (5 quests) → feat_sp_allies_favor (1 quest, repeatable): treat progress/5 as v2 progress/1
            ["RetinuesLegacy.Doctrines.Catalog.BattlefieldTithes+BT_QuestForAlliedLord"] = One(
                "feat_sp_allies_favor",
                5,
                1
            ),
            // BT_LeadArmyVictory (lead allied army to victory) ≈ feat_sp_second_in_command (win not as main commander)
            ["RetinuesLegacy.Doctrines.Catalog.BattlefieldTithes+BT_LeadArmyVictory"] = One(
                "feat_sp_second_in_command",
                1,
                1
            ),
            ["RetinuesLegacy.Doctrines.Catalog.BattlefieldTithes+BT_TurnTideAlliedArmyBattle"] =
                One("feat_sp_turn_the_tide", 1, 1),

            // PragmaticScavengers → doc_loot_pragmatic_scavengers
            // PS_Allies100Casualties: v1 high-water mark up to 100 → v2 binary (completed = done once)
            ["RetinuesLegacy.Doctrines.Catalog.PragmaticScavengers+PS_Allies100Casualties"] = One(
                "feat_sp_costly_victory",
                100,
                1
            ),
            ["RetinuesLegacy.Doctrines.Catalog.PragmaticScavengers+PS_AllyArmyWin3"] = One(
                "feat_sp_march_together",
                3,
                1
            ),
            ["RetinuesLegacy.Doctrines.Catalog.PragmaticScavengers+PS_RescueLord"] = One(
                "feat_sp_rescue_mission",
                1,
                1
            ),

            // AncestralHeritage → doc_loot_ancestral_heritage
            ["RetinuesLegacy.Doctrines.Catalog.AncestralHeritage+AH_150OutnumberedOwnCulture"] =
                One("feat_sp_cultural_triumph", 1, 1),
            ["RetinuesLegacy.Doctrines.Catalog.AncestralHeritage+AH_CaptureOwnCultureFief"] = One(
                "feat_sp_homecoming",
                1,
                1
            ),
            // AH_TournamentOwnCultureTown: no equivalent feat in doc_loot_ancestral_heritage

            // ── Armory ───────────────────────────────────────────────────────

            // CulturalPride → doc_armory_cultural_pride
            ["RetinuesLegacy.Doctrines.Catalog.CulturalPride+CP_TournamentOwnCultureGear"] = One(
                "feat_eq_hometown_tournament",
                1,
                1
            ),
            ["RetinuesLegacy.Doctrines.Catalog.CulturalPride+CP_FullSet100Kills"] = One(
                "feat_eq_proud_and_strong",
                100,
                100
            ),
            ["RetinuesLegacy.Doctrines.Catalog.CulturalPride+CP_DefeatForeignRuler"] = One(
                "feat_eq_kingslayer",
                1,
                1
            ),

            // ClanicTraditions → doc_armory_honor_guard: feat conditions completely changed; no mapping

            // RoyalPatronage → doc_armory_royal_patronage
            ["RetinuesLegacy.Doctrines.Catalog.RoyalPatronage+RP_Recruit100CustomKingdom"] = One(
                "feat_eq_royal_levy",
                100,
                100
            ),
            ["RetinuesLegacy.Doctrines.Catalog.RoyalPatronage+RP_CompanionGovernor30Days"] = One(
                "feat_eq_royal_stewardship",
                30,
                30
            ),
            ["RetinuesLegacy.Doctrines.Catalog.RoyalPatronage+RP_1000KillsCustomKingdom"] = One(
                "feat_eq_royal_host",
                1000,
                1000
            ),

            // Ironclad → doc_armory_ironclad
            // IC_FullSetT6100Kills (100 kills with T6 armor+helmet) ≈ feat_eq_ironmen (win battle, only full metal armor)
            ["RetinuesLegacy.Doctrines.Catalog.Ironclad+IC_FullSetT6100Kills"] = One(
                "feat_eq_ironmen",
                100,
                1
            ),
            // IC_12TroopsAthletics90, IC_100BattleOutnumberedLowTier: no equivalent

            // ── Troops ───────────────────────────────────────────────────────

            // StalwartMilitia → doc_troops_stalwart_militia
            ["RetinuesLegacy.Doctrines.Catalog.StalwartMilitia+SM_DefendCityFromSiege"] = One(
                "feat_trp_defender_of_the_city",
                1,
                1
            ),
            ["RetinuesLegacy.Doctrines.Catalog.StalwartMilitia+SM_PlayerKillsInSiegeDefense"] = One(
                "feat_trp_they_shall_not_pass",
                50,
                50
            ),
            ["RetinuesLegacy.Doctrines.Catalog.StalwartMilitia+SM_RaiseMilitiaTo400"] = One(
                "feat_trp_watchers_on_the_walls",
                400,
                400
            ),

            // RoadWardens → doc_troops_road_wardens
            ["RetinuesLegacy.Doctrines.Catalog.RoadWardens+RW_OwnThreeCaravans"] = One(
                "feat_trp_trade_network",
                3,
                3
            ),
            // RW_MerchantTownQuests: 3 quests for one merchant → feat_trp_merchants_favor: 10 total (repeatable)
            ["RetinuesLegacy.Doctrines.Catalog.RoadWardens+RW_MerchantTownQuests"] = One(
                "feat_trp_merchants_favor",
                3,
                10
            ),
            ["RetinuesLegacy.Doctrines.Catalog.RoadWardens+RW_ClearHideout"] = One(
                "feat_trp_bandit_scourge",
                1,
                1
            ),

            // ArmedPeasantry → doc_troops_armed_peasantry
            ["RetinuesLegacy.Doctrines.Catalog.ArmedPeasantry+AP_DefendVillageOnlyCustom"] = One(
                "feat_trp_shield_of_the_people",
                1,
                1
            ),
            // AP_HeadmanVillageQuests / AP_LandownerVillageQuests: 3 cumulative → v2 repeatable Target=1 each
            ["RetinuesLegacy.Doctrines.Catalog.ArmedPeasantry+AP_HeadmanVillageQuests"] = One(
                "feat_trp_headmans_help",
                3,
                1
            ),
            ["RetinuesLegacy.Doctrines.Catalog.ArmedPeasantry+AP_LandownerVillageQuests"] = One(
                "feat_trp_landowners_request",
                3,
                1
            ),

            // Captains → doc_troops_captains
            // CP_MaxOutEliteAndBasic (T6 elite AND T5 basic both maxed) → warrior_class (T6) + veterans (T5)
            ["RetinuesLegacy.Doctrines.Catalog.Captains+CP_MaxOutEliteAndBasic"] = Two(
                "feat_trp_warrior_class",
                1,
                1,
                "feat_trp_veterans",
                1,
                1
            ),
            // CP_Earn200InfluenceInADay, CP_Have4000Renown: no equivalent

            // ── Training ─────────────────────────────────────────────────────

            // IronDiscipline → doc_training_iron_discipline
            ["RetinuesLegacy.Doctrines.Catalog.IronDiscipline+ID_Upgrade100BasicToMax"] = One(
                "feat_tr_forged_in_battle",
                100,
                100
            ),
            ["RetinuesLegacy.Doctrines.Catalog.IronDiscipline+ID_LeadArmy10Days"] = One(
                "feat_tr_general",
                10,
                10
            ),
            ["RetinuesLegacy.Doctrines.Catalog.IronDiscipline+ID_DefeatTwiceSizeOnlyCustom"] = One(
                "feat_tr_disciplined_victory",
                1,
                1
            ),

            // SteadfastSoldiers → doc_training_steadfast_soldiers
            ["RetinuesLegacy.Doctrines.Catalog.SteadfastSoldiers+SS_TroopsMaxedSkills"] = One(
                "feat_tr_peak_performance",
                15,
                10
            ),
            ["RetinuesLegacy.Doctrines.Catalog.SteadfastSoldiers+SS_SiegeDefenseOnlyCustom"] = One(
                "feat_tr_hold_the_walls",
                1,
                1
            ),
            ["RetinuesLegacy.Doctrines.Catalog.SteadfastSoldiers+SS_RaiseSecurityTo60"] = One(
                "feat_tr_public_safety",
                60,
                60
            ),

            // MastersAtArms → doc_training_masters_at_arms
            ["RetinuesLegacy.Doctrines.Catalog.MastersAtArms+MAA_Upgrade100EliteToMax"] = One(
                "feat_tr_distinguished_service",
                100,
                100
            ),
            ["RetinuesLegacy.Doctrines.Catalog.MastersAtArms+MAA_KO50Opponents"] = One(
                "feat_tr_brawler",
                50,
                50
            ),
            ["RetinuesLegacy.Doctrines.Catalog.MastersAtArms+MAA_1000EliteKills"] = One(
                "feat_tr_battle_hardened",
                1000,
                1000
            ),

            // AdaptiveTraining → doc_training_advanced_tactics
            ["RetinuesLegacy.Doctrines.Catalog.AdaptiveTraining+AT_WinWithEvenSplit"] = One(
                "feat_tr_combined_arms",
                1,
                1
            ),
            // AT_5Weapons: high-water mark of weapon-class count in a battle → v2 binary completed
            ["RetinuesLegacy.Doctrines.Catalog.AdaptiveTraining+AT_5Weapons"] = One(
                "feat_tr_lethal_versatility",
                5,
                1
            ),
            // AT_150InEachSkill: no equivalent in doc_training_advanced_tactics

            // ── Retinues ─────────────────────────────────────────────────────

            // Indomitable → doc_retinues_indomitable
            ["RetinuesLegacy.Doctrines.Catalog.Indomitable+IND_25EquivNoCasualty"] = One(
                "feat_ret_flawless_execution",
                25,
                20
            ),
            // IND_JoinSiegeDefenderFullStrength: no close equivalent in indomitable
            ["RetinuesLegacy.Doctrines.Catalog.Indomitable+IND_RetinueOnly3DefWins"] = One(
                "feat_ret_hold_the_line",
                3,
                1
            ),

            // BoundByHonor → doc_retinues_bound_by_honor
            ["RetinuesLegacy.Doctrines.Catalog.BoundByHonor+BBH_ProtectVillagersOrCaravans"] = One(
                "feat_ret_safe_travels",
                3,
                1
            ),
            ["RetinuesLegacy.Doctrines.Catalog.BoundByHonor+BBH_RetinueOnlyMorale90For15Days"] =
                One("feat_ret_high_spirits", 15, 15),
            ["RetinuesLegacy.Doctrines.Catalog.BoundByHonor+BBH_Defeat10Bandits"] = One(
                "feat_ret_bounty_hunters",
                10,
                5
            ),

            // Vanguard → doc_retinues_vanguard
            // VG_ClearHideoutRetinueOnly: no equivalent within vanguard doctrine
            ["RetinuesLegacy.Doctrines.Catalog.Vanguard+VG_Win100RetinueOnly"] = One(
                "feat_ret_shock_assault",
                1,
                1
            ),
            ["RetinuesLegacy.Doctrines.Catalog.Vanguard+VG_FirstMeleeKillInSiege"] = One(
                "feat_ret_first_through_the_breach",
                1,
                1
            ),

            // Immortals → doc_retinues_immortals
            ["RetinuesLegacy.Doctrines.Catalog.Immortals+IM_100RetinueSurviveStruckDown"] = One(
                "feat_ret_still_standing",
                100,
                20
            ),
            ["RetinuesLegacy.Doctrines.Catalog.Immortals+IM_Win100NoDeaths"] = One(
                "feat_ret_perfect_victory",
                1,
                1
            ),
            // IM_Retinue200Enemies (200 enemies in one battle, high-water) ≈ feat_ret_defy_the_tide (binary overwhelming odds)
            ["RetinuesLegacy.Doctrines.Catalog.Immortals+IM_Retinue200Enemies"] = One(
                "feat_ret_defy_the_tide",
                200,
                1
            ),
        };
    }
}
