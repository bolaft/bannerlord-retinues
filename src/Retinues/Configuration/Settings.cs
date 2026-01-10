using Retinues.Editor.Events;
using Retinues.Framework.Runtime;
using Retinues.Modules;
using Retinues.UI.Services;
using static Retinues.Configuration.SettingsManager;

namespace Retinues.Configuration
{
    /// <summary>
    /// Definitions for all configuration options (no boilerplate here).
    /// </summary>
    [SafeClass]
    public static class Settings
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     User Interface                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section UserInterface = CreateSection(
            name: L.F("mcm_section_user_interface", "User Interface")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EditorHotkey = CreateOption(
            section: UserInterface,
            name: L.F("mcm_option_editor_hotkey", "Editor Hotkey"),
            hint: L.F(
                "mcm_option_editor_hotkey_hint",
                "Enables the [R] hotkey to open the editor."
            ),
            @default: true
        );

        public enum ItemUnlockNotificationStyle
        {
            Popup,
            Message,
        }

        public static readonly MultiChoiceOption<ItemUnlockNotificationStyle> ItemUnlockNotification =
            CreateMultiChoiceOption(
                section: UserInterface,
                name: L.F("mcm_option_item_unlock_notification", "Item Unlock Notification"),
                hint: L.F(
                    "mcm_option_item_unlock_notification_hint",
                    "Determines how the notification style when when new items are unlocked."
                ),
                @default: ItemUnlockNotificationStyle.Popup,
                choices: [ItemUnlockNotificationStyle.Popup, ItemUnlockNotificationStyle.Message],
                choiceFormatter: v =>
                    v switch
                    {
                        ItemUnlockNotificationStyle.Popup => L.S(
                            "item_unlock_notification_popup",
                            "Popup"
                        ),
                        ItemUnlockNotificationStyle.Message => L.S(
                            "item_unlock_notification_message",
                            "Log Message"
                        ),
                        _ => v.ToString(),
                    },
                dependsOn: EquipmentNeedsUnlocking
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Restrictions                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Restrictions = CreateSection(
            name: L.F("mcm_section_restrictions", "Restrictions")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public enum EditingRestrictionMode
        {
            None,
            InSettlement,
            InFief,
        }

        public static readonly MultiChoiceOption<EditingRestrictionMode> EditingRestriction =
            CreateMultiChoiceOption(
                section: Restrictions,
                name: L.F("mcm_option_editing_restriction", "Location Restrictions"),
                hint: L.F(
                    "mcm_option_editing_restriction_hint",
                    "Restricts where custom troops can be edited. Note: retinues are unaffected by this setting."
                ),
                @default: EditingRestrictionMode.None,
                @realistic: EditingRestrictionMode.InFief,
                choices:
                [
                    EditingRestrictionMode.None,
                    EditingRestrictionMode.InSettlement,
                    EditingRestrictionMode.InFief,
                ],
                choiceFormatter: v =>
                    v switch
                    {
                        EditingRestrictionMode.None => L.S("editing_restriction_none", "Anywhere"),
                        EditingRestrictionMode.InSettlement => L.S(
                            "editing_restriction_in_settlement",
                            "Any Settlement"
                        ),
                        EditingRestrictionMode.InFief => L.S(
                            "editing_restriction_in_fief",
                            "Faction Settlement"
                        ),
                        _ => v.ToString(),
                    },
                fires: [UIEvent.Character]
            );

        public static readonly Option<int> MinTierForMounts = CreateOption(
            section: Restrictions,
            name: L.F("mcm_option_min_tier_for_mounts", "Minimum Tier For Mounts"),
            hint: L.F(
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
            hint: L.F(
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
            hint: L.F(
                "mcm_option_min_tier_for_noble_mounts_hint",
                "Troops below the specified tier will not be allowed to use noble mounts."
            ),
            minValue: 0,
            maxValue: 6,
            @default: 5
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Doctrines                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Doctrines = CreateSection(
            name: L.F("mcm_section_doctrines", "Doctrines")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EnableDoctrines = CreateOption(
            section: Doctrines,
            name: L.F("mcm_option_enable_doctrines", "Enable Doctrines"),
            hint: L.F(
                "mcm_option_enable_doctrines_hint",
                "Toggles the Doctrines feature on or off."
            ),
#if !DEBUG
            requiresRestart: true,
#endif
            @default: true
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Retinues = CreateSection(
            name: L.F("mcm_section_retinues", "Retinues")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EnableRetinues = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_enable_retinues", "Enable Retinues"),
            hint: L.F("mcm_option_enable_retinues_hint", "Toggles the Retinues feature on or off."),
            @default: true,
            fires: [UIEvent.Faction]
        );

        public static readonly Option<float> MaxRetinueRatio = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_max_retinue_ratio", "Max Retinue Ratio"),
            hint: L.F(
                "mcm_option_max_retinue_ratio_hint",
                "The maximum number of retinues as a ratio of the party size limit."
            ),
            minValue: 0,
            maxValue: 1,
            @default: 0.15f,
            @freeform: 1f,
            @realistic: 0.1f,
#if !DEBUG
            requiresRestart: true,
#endif
            dependsOn: EnableRetinues
        );

        public static readonly Option<int> RetinueHealthBonus = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_retinue_health_bonus", "Retinue Health Bonus"),
            hint: L.F(
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
            hint: L.F(
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
            hint: L.F(
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
            name: L.F("mcm_section_troops", "Troops")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public enum ClanTroopsUnlockMode
        {
            AlwaysUnlocked,
            UnlockedWithFirstFief,
            Disabled,
        }

        public static readonly MultiChoiceOption<ClanTroopsUnlockMode> ClanTroopsUnlock =
            CreateMultiChoiceOption(
                section: Troops,
                name: L.F("mcm_option_clan_troops_unlock", "Clan Troops"),
                hint: L.F(
                    "mcm_option_clan_troops_unlock_hint",
                    "Determines when clan troops become available."
                ),
                @default: ClanTroopsUnlockMode.UnlockedWithFirstFief,
                choices:
                [
                    ClanTroopsUnlockMode.AlwaysUnlocked,
                    ClanTroopsUnlockMode.UnlockedWithFirstFief,
                    ClanTroopsUnlockMode.Disabled,
                ],
#if !DEBUG
                requiresRestart: true,
#endif
                choiceFormatter: v =>
                    v switch
                    {
                        ClanTroopsUnlockMode.AlwaysUnlocked => L.S(
                            "clan_troops_unlock_always_unlocked",
                            "Always Unlocked"
                        ),
                        ClanTroopsUnlockMode.UnlockedWithFirstFief => L.S(
                            "clan_troops_unlock_with_first_fief",
                            "Unlocked With First Fief"
                        ),
                        ClanTroopsUnlockMode.Disabled => L.S(
                            "clan_troops_unlock_disabled",
                            "Disabled"
                        ),
                        _ => v.ToString(),
                    }
            );

        public enum KingdomTroopsUnlockMode
        {
            AlwaysUnlocked,
            UnlockedUponBecomingRuler,
            Disabled,
        }

        public static readonly MultiChoiceOption<KingdomTroopsUnlockMode> KingdomTroopsUnlock =
            CreateMultiChoiceOption(
                section: Troops,
                name: L.F("mcm_option_kingdom_troops_unlock", "Kingdom Troops"),
                hint: L.F(
                    "mcm_option_kingdom_troops_unlock_hint",
                    "Determines when kingdom troops become available."
                ),
                @default: KingdomTroopsUnlockMode.UnlockedUponBecomingRuler,
                choices:
                [
                    KingdomTroopsUnlockMode.AlwaysUnlocked,
                    KingdomTroopsUnlockMode.UnlockedUponBecomingRuler,
                    KingdomTroopsUnlockMode.Disabled,
                ],
#if !DEBUG
                requiresRestart: true,
#endif
                choiceFormatter: v =>
                    v switch
                    {
                        KingdomTroopsUnlockMode.AlwaysUnlocked => L.S(
                            "kingdom_troops_unlock_always_unlocked",
                            "Always Unlocked"
                        ),
                        KingdomTroopsUnlockMode.UnlockedUponBecomingRuler => L.S(
                            "kingdom_troops_unlock_upon_becoming_ruler",
                            "Unlocked Upon Becoming Ruler"
                        ),
                        KingdomTroopsUnlockMode.Disabled => L.S(
                            "kingdom_troops_unlock_disabled",
                            "Disabled"
                        ),
                        _ => v.ToString(),
                    }
            );

        public enum TroopsMode
        {
            RootsOnly,
            LeanTrees,
            FullTrees,
        }

        public static readonly MultiChoiceOption<TroopsMode> StarterTroops =
            CreateMultiChoiceOption(
                section: Troops,
                name: L.F("mcm_option_starter_troops", "Starter Troops"),
                hint: L.F(
                    "mcm_option_starter_troops_hint",
                    "Determines clan and kingdom starter troops."
                ),
                @default: TroopsMode.LeanTrees,
                choices: [TroopsMode.RootsOnly, TroopsMode.LeanTrees, TroopsMode.FullTrees],
                choiceFormatter: v =>
                    v switch
                    {
                        TroopsMode.RootsOnly => L.S(
                            "starter_troops_clone_culture_roots",
                            "Root Troops Only"
                        ),
                        TroopsMode.LeanTrees => L.S(
                            "starter_troops_clone_culture_lean_trees",
                            "Lean Troop Trees"
                        ),
                        TroopsMode.FullTrees => L.S(
                            "starter_troops_clone_culture_full_trees",
                            "Full Troop Trees"
                        ),
                        _ => v.ToString(),
                    }
            );

        public enum EquipmentMode
        {
            RandomSet,
            SingleSet,
            AllSets,
            EmptySet,
        }

        public static readonly MultiChoiceOption<EquipmentMode> StarterEquipment =
            CreateMultiChoiceOption(
                section: Troops,
                name: L.F("mcm_option_starter_equipment", "Starter Equipment"),
                hint: L.F(
                    "mcm_option_starter_equipment_hint",
                    "Sets the starter equipment for newly unlocked troops."
                ),
                @default: EquipmentMode.RandomSet,
                choices:
                [
                    EquipmentMode.RandomSet,
                    EquipmentMode.SingleSet,
                    EquipmentMode.AllSets,
                    EquipmentMode.EmptySet,
                ],
                choiceFormatter: v =>
                    v switch
                    {
                        EquipmentMode.RandomSet => L.S("starter_equipment_random_set", "Random"),
                        EquipmentMode.SingleSet => L.S(
                            "starter_equipment_single_set",
                            "Copy One Set"
                        ),
                        EquipmentMode.AllSets => L.S("starter_equipment_all_sets", "Copy All Sets"),
                        EquipmentMode.EmptySet => L.S("starter_equipment_empty_set", "Empty"),
                        _ => v.ToString(),
                    }
            );

        public static readonly Option<float> MixedGenderRatio = CreateOption(
            section: Troops,
            name: L.F("mcm_option_mixed_gender_ratio", "Mixed Gender Ratio"),
            hint: L.F(
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
            name: L.F("mcm_section_recruitment", "Recruitment")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public enum RecruitmentMode
        {
            Anywhere,
            FactionFiefs,
            ClanOrKingdomFiefs,
            Nowhere,
        }

        public static readonly MultiChoiceOption<RecruitmentMode> ClanTroopsAvailability =
            CreateMultiChoiceOption(
                section: Recruitment,
                name: L.F("mcm_option_clan_troops_availability", "Clan Troops Availability"),
                hint: L.F(
                    "mcm_option_clan_troops_availability_hint",
                    "Determines where clan troops can be recruited from."
                ),
                @default: RecruitmentMode.FactionFiefs,
                choices:
                [
                    RecruitmentMode.Anywhere,
                    RecruitmentMode.FactionFiefs,
                    RecruitmentMode.ClanOrKingdomFiefs,
                    RecruitmentMode.Nowhere,
                ],
                choiceFormatter: v =>
                    v switch
                    {
                        RecruitmentMode.Anywhere => L.S("troops_availability_anywhere", "Anywhere"),
                        RecruitmentMode.FactionFiefs => L.S(
                            "troops_availability_clan_fiefs",
                            "Clan Fiefs"
                        ),
                        RecruitmentMode.ClanOrKingdomFiefs => L.S(
                            "troops_availability_clan_or_kingdom_fiefs",
                            "Clan or Kingdom Fiefs"
                        ),
                        RecruitmentMode.Nowhere => L.S("troops_availability_nowhere", "Nowhere"),
                        _ => v.ToString(),
                    },
                dependsOn: ClanTroopsUnlock,
                dependsOnValue: new[]
                {
                    ClanTroopsUnlockMode.AlwaysUnlocked,
                    ClanTroopsUnlockMode.UnlockedWithFirstFief,
                }
            );

        public static readonly MultiChoiceOption<RecruitmentMode> KingdomTroopsAvailability =
            CreateMultiChoiceOption(
                section: Recruitment,
                name: L.F("mcm_option_kingdom_troops_availability", "Kingdom Troops Availability"),
                hint: L.F(
                    "mcm_option_kingdom_troops_availability_hint",
                    "Determines where kingdom troops can be recruited from."
                ),
                @default: RecruitmentMode.FactionFiefs,
                choices:
                [
                    RecruitmentMode.Anywhere,
                    RecruitmentMode.FactionFiefs,
                    RecruitmentMode.ClanOrKingdomFiefs,
                    RecruitmentMode.Nowhere,
                ],
                choiceFormatter: v =>
                    v switch
                    {
                        RecruitmentMode.Anywhere => L.S("troops_availability_anywhere", "Anywhere"),
                        RecruitmentMode.FactionFiefs => L.S(
                            "troops_availability_kingdom_fiefs",
                            "Kingdom Fiefs"
                        ),
                        RecruitmentMode.ClanOrKingdomFiefs => L.S(
                            "troops_availability_clan_or_kingdom_fiefs",
                            "Clan or Kingdom Fiefs"
                        ),
                        RecruitmentMode.Nowhere => L.S("troops_availability_nowhere", "Nowhere"),
                        _ => v.ToString(),
                    },
                dependsOn: KingdomTroopsUnlock,
                dependsOnValue: new[]
                {
                    KingdomTroopsUnlockMode.AlwaysUnlocked,
                    KingdomTroopsUnlockMode.UnlockedUponBecomingRuler,
                }
            );

        public static readonly Option<bool> MixWithVanillaTroops = CreateOption(
            section: Recruitment,
            name: L.F("mcm_option_mix_with_vanilla_troops", "Mix With Vanilla Troops"),
            hint: L.F(
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

        public static readonly MultiChoiceOption<AllowedRecruitersMode> AllowedRecruiters =
            CreateMultiChoiceOption(
                section: Recruitment,
                name: L.F("mcm_option_allowed_recruiters", "Allowed Recruiters"),
                hint: L.F(
                    "mcm_option_allowed_recruiters_hint",
                    "Determines who can recruit custom troops. 'Everyone' is recommended for compatibility with other recruitement mods."
                ),
                @default: AllowedRecruitersMode.Everyone,
                choices:
                [
                    AllowedRecruitersMode.Everyone,
                    AllowedRecruitersMode.FactionOnly,
                    AllowedRecruitersMode.PlayerOnly,
                ],
                choiceFormatter: v =>
                    v switch
                    {
                        AllowedRecruitersMode.Everyone => L.S(
                            "allowed_recruiters_everyone",
                            "Everyone"
                        ),
                        AllowedRecruitersMode.FactionOnly => L.S(
                            "allowed_recruiters_faction_only",
                            "Faction Only"
                        ),
                        AllowedRecruitersMode.PlayerOnly => L.S(
                            "allowed_recruiters_player_only",
                            "Player Only"
                        ),
                        _ => v.ToString(),
                    }
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Equipment = CreateSection(
            name: L.F("mcm_section_equipment", "Equipment")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EquipmentCostsMoney = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_cost_money", "Equipment Costs Money"),
            hint: L.F(
                "mcm_option_enable_equipment_costs_money_hint",
                "If enabled, equipping troops will cost money. Also enables the stocks system."
            ),
            @default: true,
            @freeform: false,
            fires: [UIEvent.Page]
        );

        public static readonly Option<float> EquipmentCostMultiplier = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_cost_multiplier", "Cost Multiplier"),
            hint: L.F(
                "mcm_option_equipment_cost_multiplier_hint",
                "Determines the cost to equip a new piece of equipment."
            ),
            minValue: 0.1f,
            maxValue: 10f,
            @default: 1f,
            @realistic: 2f,
            dependsOn: EquipmentCostsMoney,
            fires: [UIEvent.Page]
        );

        public static readonly Option<bool> EquippingTakesTime = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipping_takes_time", "Equipping Takes Time"),
            hint: L.F(
                "mcm_option_equipping_takes_time_hint",
                "If enabled, equipping troops will not be instant and will take time to be completed."
            ),
            @default: false,
            @realistic: true,
            fires: [UIEvent.Equipment]
        );

        public static readonly Option<float> EquipTimeMultiplier = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_time_multiplier", "Equip Time Multiplier"),
            hint: L.F(
                "mcm_option_time_multiplier_hint",
                "Determines how long it takes to equip a new piece of equipment."
            ),
            minValue: 0.5f,
            maxValue: 5f,
            @default: 1f,
            @realistic: 2f,
            dependsOn: EquippingTakesTime,
            fires: [UIEvent.Equipment]
        );

        public static readonly Option<bool> EquippingProgressesWhileTravelling = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipping_progresses_while_travelling", "Equip While Travelling"),
            hint: L.F(
                "mcm_option_equipping_progresses_while_travelling_hint",
                "Whether equipping progresses while the party is travelling or only while waiting in settlements."
            ),
            @default: true,
            dependsOn: EquippingTakesTime
        );

        public static readonly Option<bool> EquipmentWeightLimit = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_weight_limit", "Limit By Weight"),
            hint: L.F(
                "mcm_option_weight_limit_hint",
                "Whether to limit the total equipment weight (limits are tier-based)."
            ),
            @default: true,
            fires: [UIEvent.Equipment]
        );

        public static readonly Option<float> EquipmentWeightLimitMultiplier = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_weight_limit_multiplier", "Weight Limit Multiplier"),
            hint: L.F(
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
            hint: L.F(
                "mcm_option_equipment_value_limit_hint",
                "Whether to limit the total equipment value (limits are tier-based)."
            ),
            @default: true,
            fires: [UIEvent.Equipment]
        );

        public static readonly Option<float> EquipmentValueLimitMultiplier = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_value_limit_multiplier", "Value Limit Multiplier"),
            hint: L.F(
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
            name: L.F("mcm_section_equipment_unlocks", "Equipment Unlocks")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EquipmentNeedsUnlocking = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_equipment_needs_unlocking", "Equipment Needs Unlocking"),
            hint: L.F(
                "mcm_option_equipment_needs_unlocking_hint",
                "If enabled, troops will need to unlock equipment items before being able to equip them."
            ),
            @default: true,
            @freeform: false,
            fires: [UIEvent.Page]
        );

        public static readonly Option<bool> UnlockItemsThroughKills = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_unlock_items_through_kills", "Unlock Items Through Kills"),
            hint: L.F(
                "mcm_option_unlock_items_through_kills_hint",
                "Whether items are unlocked by defeating enemies in battles."
            ),
            @default: true,
            dependsOn: EquipmentNeedsUnlocking
        );

        public static readonly Option<int> RequiredKillsToUnlock = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_required_kills_to_unlock", "Required Kills To Unlock"),
            hint: L.F(
                "mcm_option_required_kills_to_unlock_hint",
                "The number of enemy troops wearing an item that must be defeated to unlock it."
            ),
            minValue: 1,
            maxValue: 1000,
            @default: 100,
            @realistic: 200,
            dependsOn: UnlockItemsThroughKills
        );

        public static readonly Option<bool> CountAllyKills = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_count_ally_kills", "Count Ally Kills"),
            hint: L.F(
                "mcm_option_count_ally_kills_hint",
                "Whether kills made by allied troops also count towards unlocking items."
            ),
            @default: false,
            dependsOn: UnlockItemsThroughKills
        );

        public static readonly Option<bool> CountAllyCasualties = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_count_ally_casualties", "Count Ally Casualties"),
            hint: L.F(
                "mcm_option_count_ally_casualties_hint",
                "Whether ally casualties also count towards unlocking items."
            ),
            @default: false,
            dependsOn: UnlockItemsThroughKills
        );

        public static readonly Option<bool> UnlockItemsThroughWorkshops = CreateOption(
            section: EquipmentUnlocks,
            name: L.F(
                "mcm_option_unlock_items_through_workshops",
                "Unlock Items Through Workshops"
            ),
            hint: L.F(
                "mcm_option_unlock_items_through_workshops_hint",
                "Whether owning workshops unlocks items over time. Items are selected based on workshop type and settlement culture."
            ),
            @default: true,
            dependsOn: EquipmentNeedsUnlocking
        );

        public static readonly Option<int> RequiredDaysToUnlock = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_required_days_to_unlock", "Required Days To Unlock"),
            hint: L.F(
                "mcm_option_required_days_to_unlock_hint",
                "The number of days it takes one workshop to unlock an item."
            ),
            minValue: 1,
            maxValue: 100,
            @default: 14,
            @realistic: 28,
            dependsOn: UnlockItemsThroughWorkshops
        );

        public static readonly Option<bool> UnlockItemsThroughDiscards = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_unlock_items_through_discards", "Unlock Items Through Discards"),
            hint: L.F(
                "mcm_option_unlock_items_through_discards_hint",
                "Whether items are unlocked by discarding items."
            ),
            @default: false,
            dependsOn: EquipmentNeedsUnlocking
        );

        public static readonly Option<int> RequiredDiscardsToUnlock = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_required_discards_to_unlock", "Required Discards To Unlock"),
            hint: L.F(
                "mcm_option_required_discards_to_unlock_hint",
                "The number of times an item must be discarded to unlock it."
            ),
            minValue: 1,
            maxValue: 100,
            @default: 10,
            @realistic: 20,
            dependsOn: UnlockItemsThroughDiscards
        );

        public static readonly Option<int> DefaultUnlockedAmountPerSlot = CreateOption(
            section: EquipmentUnlocks,
            name: L.F(
                "mcm_option_default_unlocked_amount_per_slot",
                "Pre-Unlocked Amount Per Slot"
            ),
            hint: L.F(
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
            hint: L.F(
                "mcm_option_default_unlocked_item_max_tier_hint",
                "The maximum tier of items unlocked on game start."
            ),
            dependsOn: EquipmentNeedsUnlocking,
            minValue: 1,
            maxValue: 6,
            @default: 2,
            @realistic: 1
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Skills = CreateSection(
            name: L.F("mcm_section_skills", "Skills")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EnableSkillGainSystem = CreateOption(
            section: Skills,
            name: L.F("mcm_option_enable_skill_gain_system", "Skill Points Must Be Earned"),
            hint: L.F(
                "mcm_option_enable_skill_gain_system_hint",
                "If enabled, troops will need to earn skill points in battle before leveling up skills."
            ),
            @default: true,
            @freeform: false,
            fires: [UIEvent.Page]
        );

        public static readonly Option<float> SkillPointsGainRate = CreateOption(
            section: Skills,
            name: L.F("mcm_option_skill_points_gain_rate", "Gain Rate"),
            hint: L.F(
                "mcm_option_skill_points_gain_rate_hint",
                "Rate at which troops gain skill points in battle."
            ),
            minValue: 0.1f,
            maxValue: 5f,
            @default: 1f,
            @realistic: 0.5f,
            dependsOn: EnableSkillGainSystem,
            fires: [UIEvent.Character]
        );

        public static readonly Option<bool> TrainingTakesTime = CreateOption(
            section: Skills,
            name: L.F("mcm_option_training_takes_time", "Training Takes Time"),
            hint: L.F(
                "mcm_option_training_takes_time_hint",
                "If enabled, increasing skills will not be instant and will take time to be improved."
            ),
            @default: false,
            @realistic: true,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<float> SkillProgressPerDay = CreateOption(
            section: Skills,
            name: L.F("mcm_option_skill_progress_per_day", "Trained Points Per Day"),
            hint: L.F(
                "mcm_option_skill_progress_per_day_hint",
                "Determines training speed in points per day."
            ),
            minValue: 0.1f,
            maxValue: 4f,
            @default: 1f,
            @realistic: 2f,
            dependsOn: TrainingTakesTime,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<bool> TrainingProgressesWhileTravelling = CreateOption(
            section: Skills,
            name: L.F("mcm_option_training_progresses_while_travelling", "Train While Travelling"),
            hint: L.F(
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
            name: L.F("mcm_section_skill_caps", "Skill Caps")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<int> SkillCapT0 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 0)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 0)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 20,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT1 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 1)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 1)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 20,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT2 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 2)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 2)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 60,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT3 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 3)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 3)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 80,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT4 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 4)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 4)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 120,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT5 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 5)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 5)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 160,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT6 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 6)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 6)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 260,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillCapT7 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER}", ("TIER", 7)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 7)
            ),
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
            name: L.F("mcm_section_skill_totals", "Skill Totals")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<int> SkillTotalT0 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 0)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Maximum total skill points for Tier {TIER} troops.",
                ("TIER", 0)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 90,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT1 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 1)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Maximum total skill points for Tier {TIER} troops.",
                ("TIER", 1)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 90,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT2 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 2)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Maximum total skill points for Tier {TIER} troops.",
                ("TIER", 2)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 210,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT3 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 3)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Maximum total skill points for Tier {TIER} troops.",
                ("TIER", 3)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 360,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT4 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 4)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Maximum total skill points for Tier {TIER} troops.",
                ("TIER", 4)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 555,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT5 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 5)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Maximum total skill points for Tier {TIER} troops.",
                ("TIER", 5)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 780,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT6 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 6)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Maximum total skill points for Tier {TIER} troops.",
                ("TIER", 6)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 1015,
            fires: [UIEvent.Skill]
        );

        public static readonly Option<int> SkillTotalT7 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER}", ("TIER", 7)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Maximum total skill points for Tier {TIER} troops.",
                ("TIER", 7)
            ),
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
            name: L.F("mcm_section_debug", "Debug")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> DebugMode = CreateOption(
            section: Debug,
            name: L.F("mcm_option_debug_mode", "Debug Mode"),
            hint: L.F("mcm_option_debug_mode_hint", "Enables debug logging and additional checks."),
            @default: false
        );
    }
}
