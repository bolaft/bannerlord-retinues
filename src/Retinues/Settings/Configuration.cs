using System;
using System.Collections.Generic;
using Retinues.Behaviors.Troops;
using Retinues.Compatibility;
using Retinues.Domain;
using Retinues.Editor.Events;
using Retinues.Framework.Runtime;
using Retinues.Interface.Services;
using static Retinues.Settings.ConfigurationManager;

namespace Retinues.Settings
{
    /// <summary>
    /// Definitions for all configuration options (no boilerplate here).
    /// </summary>
    [SafeClass]
    public static class Configuration
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     User Interface                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section UserInterface = CreateSection(
            name: L.F("mcm_section_user_interface", "User Interface"),
            description: L.F(
                "mcm_section_user_interface_desc",
                "Editor access and in-game notifications."
            )
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EditorHotkey = CreateOption(
            section: UserInterface,
            name: L.F("mcm_option_editor_hotkey", "Editor Hotkey"),
            description: L.F(
                "mcm_option_editor_hotkey_hint",
                "Enables the [R] hotkey to open the editor."
            ),
            @default: true
        );

        public enum NotificationStyle
        {
            Popup,
            Message,
        }

        public static readonly Option<NotificationStyle> FeatCompleteNotification = CreateOption(
            section: UserInterface,
            name: L.F("mcm_option_feat_complete_notification", "Feat Complete Notification"),
            description: L.F(
                "mcm_option_feat_complete_notification_hint",
                "Determines how feat completion notifications are shown."
            ),
            @default: NotificationStyle.Popup,
            choices:
            [
                (
                    NotificationStyle.Popup,
                    L.S("notification_popup", "Popup"),
                    L.S("notification_popup_hint", "Shows a popup notification")
                ),
                (
                    NotificationStyle.Message,
                    L.S("notification_message", "Log Message"),
                    L.S("notification_message_hint", "Writes a message to the in-game message log")
                ),
            ],
            dependsOn: EnableDoctrines
        );

        public static readonly Option<NotificationStyle> ItemUnlockNotification = CreateOption(
            section: UserInterface,
            name: L.F("mcm_option_item_unlock_notification", "Item Unlock Notification"),
            description: L.F(
                "mcm_option_item_unlock_notification_hint",
                "Determines how item unlock notifications are shown."
            ),
            @default: NotificationStyle.Popup,
            choices:
            [
                (
                    NotificationStyle.Popup,
                    L.S("notification_popup", "Popup"),
                    L.S("notification_popup_hint", "Shows a popup notification")
                ),
                (
                    NotificationStyle.Message,
                    L.S("notification_message", "Log Message"),
                    L.S("notification_message_hint", "Writes a message to the in-game message log")
                ),
            ],
            dependsOn: EquipmentNeedsUnlocking
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Universal Editor                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section UniversalEditor = CreateSection(
            name: L.F("mcm_section_universal_editor", "Universal Editor"),
            description: L.F(
                "mcm_section_universal_editor_desc",
                "Settings for editing vanilla troops and heroes."
            )
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EnableUniversalEditor = CreateOption(
            section: UniversalEditor,
            name: L.F("mcm_option_enable_universal_editor", "Enable Universal Editor"),
            description: L.F(
                "mcm_option_enable_universal_editor_hint",
                "Enables the Universal Editor, that can be used to edit vanilla troops and heroes."
            ),
            @default: true
        );

        public static readonly Option<bool> EnforceSkillLimitsInUniversalMode = CreateOption(
            section: UniversalEditor,
            name: L.F("mcm_option_enforce_skill_limits", "Enforce Skill Limits"),
            description: L.F(
                "mcm_option_enforce_skill_limits_hint",
                "If enabled, the Universal Editor will enforce normal skill limits when editing vanilla troops."
            ),
            @default: false,
            fires: [UIEvent.Character, UIEvent.Page],
            dependsOn: EnableUniversalEditor
        );

        public static readonly Option<bool> EnforceEquipmentLimitsInUniversalMode = CreateOption(
            section: UniversalEditor,
            name: L.F("mcm_option_enforce_equipment_limits", "Enforce Equipment Limits"),
            description: L.F(
                "mcm_option_enforce_equipment_limits_hint",
                "If enabled, the Universal Editor will enforce normal equipment limits when editing vanilla troops."
            ),
            @default: false,
            fires: [UIEvent.Equipment],
            dependsOn: EnableUniversalEditor
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Doctrines                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Doctrines = CreateSection(
            name: L.F("mcm_section_doctrines", "Doctrines"),
            description: L.F(
                "mcm_section_doctrines_desc",
                "Enable doctrines and configure acquisition costs."
            )
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EnableDoctrines = CreateOption(
            section: Doctrines,
            name: L.F("mcm_option_enable_doctrines", "Enable Doctrines"),
            description: L.F(
                "mcm_option_enable_doctrines_hint",
                "Toggles the Doctrines feature on or off."
            ),
            @default: true,
            fires: [UIEvent.Mode]
        );

        public static readonly Option<bool> EnableFeatRequirements = CreateOption(
            section: Doctrines,
            name: L.F("mcm_option_enable_feat_requirements", "Enable Feat Requirements"),
            description: L.F(
                "mcm_option_enable_feat_requirements_hint",
                "If enabled, doctrines require specific feats to be accomplished before they can be acquired."
            ),
            @default: true,
            dependsOn: EnableDoctrines,
            fires: [UIEvent.Doctrine]
        );

        public static readonly Option<bool> DoctrinesCostMoney = CreateOption(
            section: Doctrines,
            name: L.F("mcm_option_doctrines_cost_money", "Doctrines Cost Money"),
            description: L.F(
                "mcm_option_doctrines_cost_money_hint",
                "If enabled, acquiring doctrines will cost money."
            ),
            @default: true,
            dependsOn: EnableDoctrines,
            fires: [UIEvent.Doctrine]
        );

        public static readonly Option<float> DoctrineMoneyCostMultiplier = CreateOption(
            section: Doctrines,
            name: L.F(
                "mcm_option_doctrine_money_cost_multiplier",
                "Doctrine Money Cost Multiplier"
            ),
            description: L.F(
                "mcm_option_doctrine_money_cost_multiplier_hint",
                "Multiplier affecting the money cost of acquiring doctrines."
            ),
            minValue: 0.1f,
            maxValue: 5f,
            @default: 1f,
            dependsOn: DoctrinesCostMoney,
            fires: [UIEvent.Doctrine]
        );

        public static readonly Option<bool> DoctrinesCostInfluence = CreateOption(
            section: Doctrines,
            name: L.F("mcm_option_doctrines_cost_influence", "Doctrines Cost Influence"),
            description: L.F(
                "mcm_option_doctrines_cost_influence_hint",
                "If enabled, acquiring doctrines will cost influence."
            ),
            @default: true,
            dependsOn: EnableDoctrines,
            fires: [UIEvent.Doctrine]
        );

        public static readonly Option<float> DoctrineInfluenceCostMultiplier = CreateOption(
            section: Doctrines,
            name: L.F(
                "mcm_option_doctrine_influence_cost_multiplier",
                "Doctrine Influence Cost Multiplier"
            ),
            description: L.F(
                "mcm_option_doctrine_influence_cost_multiplier_hint",
                "Multiplier affecting the influence cost of acquiring doctrines."
            ),
            minValue: 0.1f,
            maxValue: 5f,
            @default: 1f,
            dependsOn: DoctrinesCostInfluence,
            fires: [UIEvent.Doctrine]
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Restrictions                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Restrictions = CreateSection(
            name: L.F("mcm_section_restrictions", "Restrictions"),
            description: L.F(
                "mcm_section_restrictions_desc",
                "Where editing is allowed and other limits."
            )
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public enum EditingRestrictionMode
        {
            None,
            InSettlement,
            InFief,
        }

        public static readonly Option<EditingRestrictionMode> EditingRestriction = CreateOption(
            section: Restrictions,
            name: L.F("mcm_option_editing_restriction", "Location Restrictions"),
            description: L.F(
                "mcm_option_editing_restriction_hint",
                "Restricts where faction troops can be edited."
            ),
            @default: EditingRestrictionMode.None,
            choices:
            [
                (
                    EditingRestrictionMode.None,
                    L.S("editing_restriction_none", "Anywhere"),
                    L.S("editing_restriction_none_hint", "No location restrictions")
                ),
                (
                    EditingRestrictionMode.InSettlement,
                    L.S("editing_restriction_in_settlement", "Any Settlement"),
                    L.S("editing_restriction_in_settlement_hint", "You must be in any settlement")
                ),
                (
                    EditingRestrictionMode.InFief,
                    L.S("editing_restriction_in_fief", "Faction Settlement"),
                    L.S(
                        "editing_restriction_in_fief_hint",
                        "You must be in an owned faction fief (retinues excepted)"
                    )
                ),
            ],
            fires: [UIEvent.Character]
        );

        public static readonly Option<int> MinTierForMounts = CreateOption(
            section: Restrictions,
            name: L.F("mcm_option_min_tier_for_mounts", "Minimum Tier For Mounts"),
            description: L.F(
                "mcm_option_min_tier_for_mounts_hint",
                "Troops below the specified tier will not be allowed to be mounted."
            ),
            minValue: 0,
            maxValue: 6,
            @default: 2
        );

        public static readonly Option<int> MinTierForWarMounts = CreateOption(
            section: Restrictions,
            name: L.F("mcm_option_min_tier_for_war_mounts", "Minimum Tier For War Mounts"),
            description: L.F(
                "mcm_option_min_tier_for_war_mounts_hint",
                "Troops below the specified tier will not be allowed to use war mounts."
            ),
            minValue: 0,
            maxValue: 6,
            @default: 3
        );

        public static readonly Option<int> MinTierForNobleMounts = CreateOption(
            section: Restrictions,
            name: L.F("mcm_option_min_tier_for_noble_mounts", "Minimum Tier For Noble Mounts"),
            description: L.F(
                "mcm_option_min_tier_for_noble_mounts_hint",
                "Troops below the specified tier will not be allowed to use noble mounts."
            ),
            minValue: 0,
            maxValue: 6,
            @default: 5
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Retinues = CreateSection(
            name: L.F("mcm_section_retinues", "Retinues"),
            description: L.F(
                "mcm_section_retinues_desc",
                "Core retinues feature, unlocking, and bonuses."
            )
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EnableRetinues = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_enable_retinues", "Enable Retinues"),
            description: L.F(
                "mcm_option_enable_retinues_hint",
                "Toggles the Retinues feature on or off."
            ),
            @default: true,
            fires: [UIEvent.Faction]
        );

        public static readonly Option<float> MaxRetinueRatio = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_max_retinue_ratio", "Max Retinue Ratio"),
            description: L.F(
                "mcm_option_max_retinue_ratio_hint",
                "The maximum number of retinues as a ratio of the party size limit."
            ),
            minValue: 0,
            maxValue: 1,
            @default: 0.15f,
            dependsOn: EnableRetinues
        );

        public static readonly Option<float> RetinueUnlockSpeed = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_retinue_unlock_speed", "Retinue Unlock Speed"),
            description: L.F(
                "mcm_option_retinue_unlock_speed_hint",
                "Multiplier affecting how quickly new retinues are unlocked."
            ),
            minValue: 0.1f,
            maxValue: 5f,
            @default: 1f,
            dependsOn: EnableRetinues
        );

        public static readonly Option<int> RetinueHealthBonus = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_retinue_health_bonus", "Retinue Health Bonus"),
            description: L.F(
                "mcm_option_retinue_health_bonus_hint",
                "The amount added to retinue troop health when Buff Retinue Health is enabled."
            ),
            minValue: 0,
            maxValue: 100,
            @default: 20,
            dependsOn: EnableRetinues
        );

        public static readonly Option<int> RetinueSkillCapBonus = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_retinue_skill_cap_bonus", "Retinue Skill Cap Bonus"),
            description: L.F(
                "mcm_option_retinue_skill_cap_bonus_hint",
                "The amount added to retinue troop skill caps when Buff Retinue Skill Cap is enabled."
            ),
            minValue: 0,
            maxValue: 50,
            @default: 5,
            dependsOn: EnableRetinues,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> RetinueSkillTotalBonus = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_retinue_skill_total_bonus", "Retinue Skill Total Bonus"),
            description: L.F(
                "mcm_option_retinue_skill_total_bonus_hint",
                "The amount added to retinue troop skill totals when Buff Retinue Skill Total is enabled."
            ),
            minValue: 0,
            maxValue: 200,
            @default: 20,
            dependsOn: EnableRetinues,
            fires: [UIEvent.Skill]
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Troops = CreateSection(
            name: L.F("mcm_section_troops", "Troops"),
            description: L.F(
                "mcm_section_troops_desc",
                "Troop availability and starter generation options."
            )
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public enum TroopsUnlockMode
        {
            AlwaysUnlocked,
            UnlockedWithFirstFief,
            UnlockedUponBecomingRuler,
            Disabled,
        }

        public static readonly Option<TroopsUnlockMode> ClanTroopsUnlock = CreateOption(
            section: Troops,
            name: L.F("mcm_option_clan_troops_unlock", "Clan Troops"),
            description: L.F(
                "mcm_option_clan_troops_unlock_hint",
                "Determines when clan troops become available."
            ),
            @default: TroopsUnlockMode.UnlockedWithFirstFief,
            choices:
            [
                (
                    TroopsUnlockMode.AlwaysUnlocked,
                    L.S("clan_troops_unlock_always_unlocked", "Always Unlocked"),
                    L.S(
                        "clan_troops_unlock_always_unlocked_hint",
                        "Clan troops are available from the start"
                    )
                ),
                (
                    TroopsUnlockMode.UnlockedWithFirstFief,
                    L.S("clan_troops_unlock_with_first_fief", "Unlocked With First Fief"),
                    L.S(
                        "clan_troops_unlock_with_first_fief_hint",
                        "Unlocks when the clan acquires its first fief"
                    )
                ),
                (
                    TroopsUnlockMode.Disabled,
                    L.S("clan_troops_unlock_disabled", "Disabled"),
                    L.S("clan_troops_unlock_disabled_hint", "Clan troops are never unlocked")
                ),
            ],
            requiresRestart: true
        );

        public static readonly Option<TroopsUnlockMode> KingdomTroopsUnlock = CreateOption(
            section: Troops,
            name: L.F("mcm_option_kingdom_troops_unlock", "Kingdom Troops"),
            description: L.F(
                "mcm_option_kingdom_troops_unlock_hint",
                "Determines when kingdom troops become available."
            ),
            @default: TroopsUnlockMode.UnlockedUponBecomingRuler,
            choices:
            [
                (
                    TroopsUnlockMode.UnlockedUponBecomingRuler,
                    L.S(
                        "kingdom_troops_unlock_upon_becoming_ruler",
                        "Unlocked Upon Becoming Ruler"
                    ),
                    L.S(
                        "kingdom_troops_unlock_upon_becoming_ruler_hint",
                        "Unlocks when the player founds a kingdom"
                    )
                ),
                (
                    TroopsUnlockMode.Disabled,
                    L.S("kingdom_troops_unlock_disabled", "Disabled"),
                    L.S("kingdom_troops_unlock_disabled_hint", "Kingdom troops are never unlocked")
                ),
            ],
            requiresRestart: true
        );

        public enum TroopsMode
        {
            RootsOnly,
            LeanTrees,
            FullTrees,
        }

        public static readonly Option<TroopsMode> StarterTroops = CreateOption(
            section: Troops,
            name: L.F("mcm_option_starter_troops", "Starter Troops"),
            description: L.F(
                "mcm_option_starter_troops_hint",
                "Determines clan and kingdom starter troops."
            ),
            @default: TroopsMode.LeanTrees,
            choices:
            [
                (
                    TroopsMode.RootsOnly,
                    L.S("starter_troops_clone_culture_roots", "Root Troops Only"),
                    L.S(
                        "starter_troops_clone_culture_roots_hint",
                        "Only one basic troop and one elite troop are created"
                    )
                ),
                (
                    TroopsMode.LeanTrees,
                    L.S("starter_troops_clone_culture_lean_trees", "Lean Troop Trees"),
                    L.S(
                        "starter_troops_clone_culture_lean_trees_hint",
                        "A lean selection of troops is created on game start"
                    )
                ),
                (
                    TroopsMode.FullTrees,
                    L.S("starter_troops_clone_culture_full_trees", "Full Troop Trees"),
                    L.S(
                        "starter_troops_clone_culture_full_trees_hint",
                        "The full range of troops is automatically created"
                    )
                ),
            ]
        );

        public enum EquipmentMode
        {
            RandomSet,
            SingleSet,
            AllSets,
            EmptySet,
        }

        public static readonly Option<EquipmentMode> StarterEquipment = CreateOption(
            section: Troops,
            name: L.F("mcm_option_starter_equipment", "Starter Equipment"),
            description: L.F(
                "mcm_option_starter_equipment_hint",
                "Sets the starter equipment for newly unlocked troops."
            ),
            @default: EquipmentMode.RandomSet,
            choices:
            [
                (
                    EquipmentMode.RandomSet,
                    L.S("starter_equipment_random_set", "Random"),
                    L.S("starter_equipment_random_set_hint", "A random equipment set is assigned")
                ),
                (
                    EquipmentMode.SingleSet,
                    L.S("starter_equipment_single_set", "Copy One Set"),
                    L.S(
                        "starter_equipment_single_set_hint",
                        "One equipment set is copied from the base troop"
                    )
                ),
                (
                    EquipmentMode.AllSets,
                    L.S("starter_equipment_all_sets", "Copy All Sets"),
                    L.S(
                        "starter_equipment_all_sets_hint",
                        "All equipment sets are copied from the base troop"
                    )
                ),
                (
                    EquipmentMode.EmptySet,
                    L.S("starter_equipment_empty_set", "Empty"),
                    L.S("starter_equipment_empty_set_hint", "No starter equipment is created")
                ),
            ]
        );

        public static readonly Option<int> RandomItemMaxTier = CreateOption(
            section: Troops,
            name: L.F("mcm_option_random_item_max_tier", "Random Item Max Tier"),
            description: L.F(
                "mcm_option_random_item_max_tier_hint",
                "The maximum tier for randomly assigned starter equipment. Item tier is also capped by troop tier."
            ),
            minValue: 1,
            maxValue: 6,
            @default: 4,
            dependsOn: StarterEquipment,
            dependsOnValue: new[] { EquipmentMode.RandomSet }
        );

        public static readonly Option<int> CaptainSpawnRate = CreateOption(
            section: Troops,
            name: L.F("mcm_option_captain_spawn_rate", "Captain Spawn Rate"),
            description: L.F(
                "mcm_option_captain_spawn_rate_hint",
                "Determines how many regular troops must be fielded before another captain can spawn."
            ),
            minValue: 10,
            maxValue: 50,
            @default: 15
        );

        public static readonly Option<float> MixedGenderRatio = CreateOption(
            section: Troops,
            name: L.F("mcm_option_mixed_gender_ratio", "Mixed Gender Ratio"),
            description: L.F(
                "mcm_option_mixed_gender_ratio_hint",
                "Sets the chance for mixed gender units to spawn as a member of the opposite sex."
            ),
            minValue: 0f,
            maxValue: 0.5f,
            @default: 0.5f
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Recruitement                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Recruitment = CreateSection(
            name: L.F("mcm_section_recruitment", "Recruitment"),
            description: L.F(
                "mcm_section_recruitment_desc",
                "Where and who can recruit custom troops."
            )
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public enum RecruitmentMode
        {
            Everywhere,
            FactionFiefs,
            ClanOrKingdomFiefs,
            Nowhere,
        }

        public static readonly Option<RecruitmentMode> ClanTroopsAvailability = CreateOption(
            section: Recruitment,
            name: L.F("mcm_option_clan_troops_availability", "Clan Troops Availability"),
            description: L.F(
                "mcm_option_clan_troops_availability_hint",
                "Determines where clan troops can be recruited from."
            ),
            @default: RecruitmentMode.FactionFiefs,
            choices:
            [
                (
                    RecruitmentMode.Everywhere,
                    L.S("troops_availability_everywhere", "Everywhere"),
                    L.S(
                        "troops_availability_everywhere_hint",
                        "Clan troops can be recruited from any settlement"
                    )
                ),
                (
                    RecruitmentMode.FactionFiefs,
                    L.S("troops_availability_clan_fiefs", "Clan Fiefs"),
                    L.S(
                        "troops_availability_clan_fiefs_hint",
                        "Clan troops can only be recruited from fiefs owned by their clan"
                    )
                ),
                (
                    RecruitmentMode.ClanOrKingdomFiefs,
                    L.S("troops_availability_clan_or_kingdom_fiefs", "Clan or Kingdom Fiefs"),
                    L.S(
                        "troops_availability_clan_or_kingdom_fiefs_hint",
                        "Clan troops can be recruited from owned clan or kingdom fiefs"
                    )
                ),
                (
                    RecruitmentMode.Nowhere,
                    L.S("troops_availability_nowhere", "Nowhere"),
                    L.S("troops_availability_nowhere_hint", "Clan troops cannot be recruited")
                ),
            ],
            dependsOn: ClanTroopsUnlock,
            dependsOnValue: new[]
            {
                TroopsUnlockMode.AlwaysUnlocked,
                TroopsUnlockMode.UnlockedWithFirstFief,
            }
        );

        public static readonly Option<RecruitmentMode> KingdomTroopsAvailability = CreateOption(
            section: Recruitment,
            name: L.F("mcm_option_kingdom_troops_availability", "Kingdom Troops Availability"),
            description: L.F(
                "mcm_option_kingdom_troops_availability_hint",
                "Determines where kingdom troops can be recruited from."
            ),
            @default: RecruitmentMode.FactionFiefs,
            choices:
            [
                (
                    RecruitmentMode.Everywhere,
                    L.S("troops_availability_everywhere", "Everywhere"),
                    L.S(
                        "troops_availability_everywhere_kingdom_hint",
                        "Kingdom troops can be recruited from any settlement"
                    )
                ),
                (
                    RecruitmentMode.FactionFiefs,
                    L.S("troops_availability_kingdom_fiefs", "Kingdom Fiefs"),
                    L.S(
                        "troops_availability_kingdom_fiefs_hint",
                        "Kingdom troops can only be recruited from fiefs owned by their kingdom"
                    )
                ),
                (
                    RecruitmentMode.ClanOrKingdomFiefs,
                    L.S("troops_availability_clan_or_kingdom_fiefs", "Clan or Kingdom Fiefs"),
                    L.S(
                        "troops_availability_clan_or_kingdom_fiefs_kingdom_hint",
                        "Kingdom troops can be recruited from owned clan or kingdom fiefs"
                    )
                ),
                (
                    RecruitmentMode.Nowhere,
                    L.S("troops_availability_nowhere", "Nowhere"),
                    L.S(
                        "troops_availability_nowhere_kingdom_hint",
                        "Kingdom troops cannot be recruited"
                    )
                ),
            ],
            dependsOn: KingdomTroopsUnlock,
            dependsOnValue: TroopsUnlockMode.UnlockedUponBecomingRuler
        );

        public static readonly Option<bool> SameCultureOnly = CreateOption(
            section: Recruitment,
            name: L.F("mcm_option_same_culture_only", "Same Culture Only"),
            description: L.F(
                "mcm_option_same_culture_only_hint",
                "If enabled, custom troops can only be recruited in settlements of the same culture."
            ),
            @default: false
        );

        public static readonly Option<bool> MixWithVanillaTroops = CreateOption(
            section: Recruitment,
            name: L.F("mcm_option_mix_with_vanilla_troops", "Mix With Vanilla Troops"),
            description: L.F(
                "mcm_option_mix_with_vanilla_troops_hint",
                "If enabled, custom troops will be mixed with vanilla troops in recruitment pools."
            ),
            @default: false
        );

        public enum AllowedRecruitersMode
        {
            Everyone,
            FactionOnly,
            PlayerOnly,
        }

        public static readonly Option<AllowedRecruitersMode> AllowedRecruiters = CreateOption(
            section: Recruitment,
            name: L.F("mcm_option_allowed_recruiters", "Allowed Recruiters"),
            description: L.F(
                "mcm_option_allowed_recruiters_hint",
                "Determines who can recruit custom troops."
            ),
            @default: AllowedRecruitersMode.Everyone,
            choices:
            [
                (
                    AllowedRecruitersMode.Everyone,
                    L.S("allowed_recruiters_everyone", "Everyone"),
                    L.S(
                        "allowed_recruiters_everyone_hint",
                        "All parties can recruit custom troops (recommended for compatibility with other recruitment mods)"
                    )
                ),
                (
                    AllowedRecruitersMode.FactionOnly,
                    L.S("allowed_recruiters_faction_only", "Faction Only"),
                    L.S(
                        "allowed_recruiters_faction_only_hint",
                        "Only faction parties can recruit custom troops"
                    )
                ),
                (
                    AllowedRecruitersMode.PlayerOnly,
                    L.S("allowed_recruiters_player_only", "Player Only"),
                    L.S(
                        "allowed_recruiters_player_only_hint",
                        "Only the player can recruit custom troops"
                    )
                ),
            ]
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Equipment = CreateSection(
            name: L.F("mcm_section_equipment", "Equipment"),
            description: L.F("mcm_section_equipment_desc", "Equipment costs, limits, and timing.")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EquipmentCostsMoney = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_cost_money", "Equipment Costs Money"),
            description: L.F(
                "mcm_option_enable_equipment_costs_money_hint",
                "If enabled, equipping troops will cost money. Also enables the stocks system."
            ),
            @default: true,
            fires: [UIEvent.Page]
        );

        public static readonly Option<float> EquipmentCostMultiplier = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_cost_multiplier", "Cost Multiplier"),
            description: L.F(
                "mcm_option_equipment_cost_multiplier_hint",
                "Determines the cost to equip a new piece of equipment."
            ),
            minValue: 0.1f,
            maxValue: 10f,
            @default: 1f,
            dependsOn: EquipmentCostsMoney,
            fires: [UIEvent.Page]
        );

        public static readonly Option<bool> EquippingTakesTime = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipping_takes_time", "Equipping Takes Time"),
            description: L.F(
                "mcm_option_equipping_takes_time_hint",
                "If enabled, equipping troops will not be instant and will take time to be completed."
            ),
            @default: false,
            fires: [UIEvent.Equipment]
        );

        public static readonly Option<float> EquipTimeMultiplier = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_time_multiplier", "Equip Time Multiplier"),
            description: L.F(
                "mcm_option_time_multiplier_hint",
                "Determines how long it takes to equip a new piece of equipment."
            ),
            minValue: 0.5f,
            maxValue: 5f,
            @default: 1f,
            dependsOn: EquippingTakesTime,
            fires: [UIEvent.Equipment]
        );

        public static readonly Option<bool> EquippingProgressesWhileTravelling = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipping_progresses_while_travelling", "Equip While Travelling"),
            description: L.F(
                "mcm_option_equipping_progresses_while_travelling_hint",
                "Whether equipping progresses while the party is travelling or only while waiting in settlements."
            ),
            @default: true,
            dependsOn: EquippingTakesTime
        );

        public static readonly Option<bool> EquipmentWeightLimit = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_weight_limit", "Limit By Weight"),
            description: L.F(
                "mcm_option_weight_limit_hint",
                "Whether to limit the total equipment weight (limits are tier-based)."
            ),
            @default: true,
            fires: [UIEvent.Equipment]
        );

        public static readonly Option<float> EquipmentWeightLimitMultiplier = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_weight_limit_multiplier", "Weight Limit Multiplier"),
            description: L.F(
                "mcm_option_equipment_weight_limit_multiplier_hint",
                "Affects the maximum total equipment weight limit."
            ),
            minValue: 0.5f,
            maxValue: 2f,
            @default: 1f,
            dependsOn: EquipmentWeightLimit,
            fires: [UIEvent.Equipment]
        );

        public static readonly Option<bool> EquipmentValueLimit = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_value_limit", "Limit By Value"),
            description: L.F(
                "mcm_option_equipment_value_limit_hint",
                "Whether to limit the total equipment value (limits are tier-based)."
            ),
            @default: true,
            fires: [UIEvent.Equipment]
        );

        public static readonly Option<float> EquipmentValueLimitMultiplier = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_value_limit_multiplier", "Value Limit Multiplier"),
            description: L.F(
                "mcm_option_equipment_value_limit_multiplier_hint",
                "Affects the maximum total equipment value limit."
            ),
            minValue: 0.5f,
            maxValue: 2f,
            @default: 1f,
            dependsOn: EquipmentValueLimit,
            fires: [UIEvent.Equipment]
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Equipment Unlocks                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section EquipmentUnlocks = CreateSection(
            name: L.F("mcm_section_equipment_unlocks", "Equipment Unlocks"),
            description: L.F(
                "mcm_section_equipment_unlocks_desc",
                "How equipment items are unlocked over time."
            )
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EquipmentNeedsUnlocking = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_equipment_needs_unlocking", "Equipment Needs Unlocking"),
            description: L.F(
                "mcm_option_equipment_needs_unlocking_hint",
                "If enabled, troops will need to unlock equipment items before being able to equip them."
            ),
            @default: true,
            fires: [UIEvent.Page]
        );

        public static readonly Option<bool> UnlockItemsThroughKills = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_unlock_items_through_kills", "Unlock Items Through Kills"),
            description: L.F(
                "mcm_option_unlock_items_through_kills_hint",
                "Whether items are unlocked by defeating enemies in battles."
            ),
            @default: true,
            dependsOn: EquipmentNeedsUnlocking,
            dependsOnDisabledOverride: false
        );

        public static readonly Option<int> RequiredKillsToUnlock = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_required_kills_to_unlock", "Required Kills To Unlock"),
            description: L.F(
                "mcm_option_required_kills_to_unlock_hint",
                "The number of enemy troops wearing an item that must be defeated to unlock it."
            ),
            minValue: 1,
            maxValue: 1000,
            @default: 100,
            dependsOn: UnlockItemsThroughKills
        );

        public static readonly Option<bool> UnlockItemsThroughWorkshops = CreateOption(
            section: EquipmentUnlocks,
            name: L.F(
                "mcm_option_unlock_items_through_workshops",
                "Unlock Items Through Workshops"
            ),
            description: L.F(
                "mcm_option_unlock_items_through_workshops_hint",
                "Whether owning workshops unlocks items over time. Items are selected based on workshop type and settlement culture."
            ),
            @default: true,
            dependsOn: EquipmentNeedsUnlocking,
            dependsOnDisabledOverride: false
        );

        public static readonly Option<int> RequiredDaysToUnlock = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_required_days_to_unlock", "Required Days To Unlock"),
            description: L.F(
                "mcm_option_required_days_to_unlock_hint",
                "The number of days it takes one workshop to unlock an item."
            ),
            minValue: 1,
            maxValue: 100,
            @default: 14,
            dependsOn: UnlockItemsThroughWorkshops
        );

        public static readonly Option<bool> UnlockItemsThroughDiscards = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_unlock_items_through_discards", "Unlock Items Through Discards"),
            description: L.F(
                "mcm_option_unlock_items_through_discards_hint",
                "Whether items are unlocked by discarding items."
            ),
            @default: false,
            dependsOn: EquipmentNeedsUnlocking,
            dependsOnDisabledOverride: false
        );

        public static readonly Option<int> RequiredDiscardsToUnlock = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_required_discards_to_unlock", "Required Discards To Unlock"),
            description: L.F(
                "mcm_option_required_discards_to_unlock_hint",
                "The number of times an item must be discarded to unlock it."
            ),
            minValue: 1,
            maxValue: 100,
            @default: 10,
            dependsOn: UnlockItemsThroughDiscards
        );

        public static readonly Option<int> DefaultUnlockedAmountPerSlot = CreateOption(
            section: EquipmentUnlocks,
            name: L.F(
                "mcm_option_default_unlocked_amount_per_slot",
                "Pre-Unlocked Amount Per Slot"
            ),
            description: L.F(
                "mcm_option_default_unlocked_amount_per_slot_hint",
                "The number of items unlocked per equipment slot on game start."
            ),
            dependsOn: EquipmentNeedsUnlocking,
            minValue: 0,
            maxValue: 10,
            @default: 3
        );

        public static readonly Option<int> DefaultUnlockedItemMaxTier = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_default_unlocked_item_max_tier", "Pre-Unlocked Max Tier"),
            description: L.F(
                "mcm_option_default_unlocked_item_max_tier_hint",
                "The maximum tier of items unlocked on game start."
            ),
            dependsOn: EquipmentNeedsUnlocking,
            minValue: 1,
            maxValue: 6,
            @default: 2
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Skills = CreateSection(
            name: L.F("mcm_section_skills", "Skills"),
            description: L.F("mcm_section_skills_desc", "Skill gain rules and training behavior.")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> SkillPointsMustBeEarned = CreateOption(
            section: Skills,
            name: L.F("mcm_option_skill_points_must_be_earned", "Skill Points Must Be Earned"),
            description: L.F(
                "mcm_option_skill_points_must_be_earned_hint",
                "If enabled, troops will need to earn skill points in battle before leveling up skills."
            ),
            @default: true,
            fires: [UIEvent.Page]
        );

        public static readonly Option<float> SkillPointsGainRate = CreateOption(
            section: Skills,
            name: L.F("mcm_option_skill_points_gain_rate", "Gain Rate"),
            description: L.F(
                "mcm_option_skill_points_gain_rate_hint",
                "Rate at which troops gain skill points in battle."
            ),
            minValue: 0.1f,
            maxValue: 5f,
            @default: 1f,
            dependsOn: SkillPointsMustBeEarned,
            fires: [UIEvent.Character]
        );

        public static readonly Option<bool> TrainingTakesTime = CreateOption(
            section: Skills,
            name: L.F("mcm_option_training_takes_time", "Training Takes Time"),
            description: L.F(
                "mcm_option_training_takes_time_hint",
                "If enabled, increasing skills will not be instant and will take time to be improved."
            ),
            @default: false,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<float> SkillProgressPerDay = CreateOption(
            section: Skills,
            name: L.F("mcm_option_skill_progress_per_day", "Trained Points Per Day"),
            description: L.F(
                "mcm_option_skill_progress_per_day_hint",
                "Determines training speed in points per day."
            ),
            minValue: 0.1f,
            maxValue: 4f,
            @default: 1f,
            dependsOn: TrainingTakesTime,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<bool> TrainingProgressesWhileTravelling = CreateOption(
            section: Skills,
            name: L.F("mcm_option_training_progresses_while_travelling", "Train While Travelling"),
            description: L.F(
                "mcm_option_training_progresses_while_travelling_hint",
                "Whether training progress occurs while the party is travelling or only while waiting in settlements."
            ),
            @default: true,
            dependsOn: TrainingTakesTime
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Skill Caps                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section SkillCaps = CreateSection(
            name: L.F("mcm_section_skill_caps", "Skill Caps"),
            description: L.F("mcm_section_skill_caps_desc", "Maximum skill level per troop tier.")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<int> SkillCapT0 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 0)),
            minValue: 20,
            maxValue: 360,
            @default: 20,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT1 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 1)),
            minValue: 20,
            maxValue: 360,
            @default: 20,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT2 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 2)),
            minValue: 20,
            maxValue: 360,
            @default: 60,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT3 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 3)),
            minValue: 20,
            maxValue: 360,
            @default: 80,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT4 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 4)),
            minValue: 20,
            maxValue: 360,
            @default: 120,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT5 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 5)),
            minValue: 20,
            maxValue: 360,
            @default: 160,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT6 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 6)),
            minValue: 20,
            maxValue: 360,
            @default: 260,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT7 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 7)),
            minValue: 20,
            maxValue: 360,
            @default: 360,
            disabled: !Mods.T7TroopUnlocker.IsLoaded,
            fires: [UIEvent.Skill]
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Skill Totals                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section SkillTotals = CreateSection(
            name: L.F("mcm_section_skill_totals", "Skill Totals"),
            description: L.F(
                "mcm_section_skill_totals_desc",
                "Maximum total skill points per troop tier."
            )
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<int> SkillTotalT0 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 0)),
            minValue: 90,
            maxValue: 1600,
            @default: 90,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT1 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 1)),
            minValue: 90,
            maxValue: 1600,
            @default: 90,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT2 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 2)),
            minValue: 90,
            maxValue: 1600,
            @default: 210,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT3 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 3)),
            minValue: 90,
            maxValue: 1600,
            @default: 380,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT4 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 4)),
            minValue: 90,
            maxValue: 1600,
            @default: 560,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT5 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 5)),
            minValue: 90,
            maxValue: 1600,
            @default: 780,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT6 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 6)),
            minValue: 90,
            maxValue: 1600,
            @default: 1020,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT7 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 7)),
            minValue: 90,
            maxValue: 1600,
            @default: 1600,
            disabled: !Mods.T7TroopUnlocker.IsLoaded,
            fires: [UIEvent.Skill]
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Debug                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Debug = CreateSection(
            name: L.F("mcm_section_debug", "Debug"),
            description: L.F("mcm_section_debug_desc", "Debug logging and diagnostics.")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> DebugMode = CreateOption(
            section: Debug,
            name: L.F("mcm_option_debug_mode", "Debug Mode"),
            description: L.F(
                "mcm_option_debug_mode_hint",
                "Enables debug logging and additional checks."
            ),
            @default: false
        );
    }
}
