using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Features.Setup
{
    /// <summary>
    /// First-run setup wizard for Retinues, shown once per save.
    /// </summary>
    [SafeClass]
    public sealed class SetupWizardBehavior : CampaignBehaviorBase
    {
        private bool _hasRun;

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this,
                OnGameLoadFinished
            );
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_SetupWizardHasRun", ref _hasRun);
        }

        private void OnGameLoadFinished()
        {
            try
            {
                if (_hasRun)
                    return;

                if (Campaign.Current == null || Hero.MainHero == null)
                    return;

                ShowWelcomePopup();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ──────────────────────────────
        //  Popup 0: Welcome / skip
        // ──────────────────────────────

        private void ShowWelcomePopup()
        {
            var title = L.T("wizard_welcome_title", "Welcome to Retinues");
            var body = L.T("wizard_welcome_body",
                "Retinues lets you raise personal troops, unlock special units and equip them over time.\n\n"
                + "Do you want to go through a quick setup now, or keep the current settings?");

            var options = new List<WizardPopupOption>
            {
                new WizardPopupOption(
                    id: "wizard_start",
                    title: L.T("wizard_welcome_begin", "Begin quick setup"),
                    subtitle: L.T("wizard_welcome_begin_sub",
                        "Answer a few questions to shape how Retinues fits into this playthrough."),
                    onSelected: ShowGlobalPresetPopup
                ),
                new WizardPopupOption(
                    id: "wizard_skip",
                    title: L.T("wizard_welcome_skip", "Keep current settings"),
                    subtitle: L.T("wizard_welcome_skip_sub",
                        "Skip the wizard and continue as things are now. You can still fine-tune everything later in the configuration menu."),
                    onSelected: CompleteWithoutChanges
                )
            };

            WizardPopup.Show(
                title,
                body,
                options,
                confirmText: L.T("wizard_continue", "Continue"),
                cancelText: L.T("wizard_cancel", "Skip"),
                onCancel: CompleteWithoutChanges
            );
        }

        // ──────────────────────────────
        //  Popup 1: Global preset
        // ──────────────────────────────

        private void ShowGlobalPresetPopup()
        {
            var title = L.T("wizard_preset_title", "Overall style");
            var body = L.T("wizard_preset_body",
                "How open do you want Retinues to feel in this campaign?");

            var options = new List<WizardPopupOption>
            {
                new WizardPopupOption(
                    id: "preset_freeform",
                    title: L.T("wizard_preset_freeform", "Freeform"),
                    subtitle: L.T("wizard_preset_freeform_sub",
                        "Sandbox style. All systems are wide open from the start with minimal requirements."),
                    onSelected: () =>
                    {
                        Config.ApplyPresetsToAll(ConfigPreset.Freeform);
                        ShowSkillsPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "preset_balanced",
                    title: L.T("wizard_preset_balanced", "Balanced"),
                    subtitle: L.T("wizard_preset_balanced_sub",
                        "A mix of freedom and challenge. Unlocks, costs and limits follow the recommended settings."),
                    onSelected: () =>
                    {
                        Config.ApplyPresetsToAll(ConfigPreset.Default);
                        ShowSkillsPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "preset_realistic",
                    title: L.T("wizard_preset_realistic", "Realistic"),
                    subtitle: L.T("wizard_preset_realistic_sub",
                        "Slow, earned progression. Unlocks are gated and gold and time matter a lot."),
                    onSelected: () =>
                    {
                        Config.ApplyPresetsToAll(ConfigPreset.Realistic);
                        ShowSkillsPopup();
                    }
                )
            };

            WizardPopup.Show(
                title,
                body,
                options,
                confirmText: L.T("wizard_continue", "Continue"),
                cancelText: L.T("wizard_cancel", "Skip"),
                onCancel: CompleteWithoutChanges
            );
        }

        // ──────────────────────────────
        //  Popup 2: Skill progression
        // ──────────────────────────────

        private void ApplySkillPreset(ConfigPreset preset)
        {
            // XP cost
            Config.BaseSkillXpCost.ApplyPreset(preset);
            Config.SkillXpCostPerPoint.ApplyPreset(preset);

            // Caps
            Config.RetinueSkillCapBonus.ApplyPreset(preset);
            Config.SkillCapTier0.ApplyPreset(preset);
            Config.SkillCapTier1.ApplyPreset(preset);
            Config.SkillCapTier2.ApplyPreset(preset);
            Config.SkillCapTier3.ApplyPreset(preset);
            Config.SkillCapTier4.ApplyPreset(preset);
            Config.SkillCapTier5.ApplyPreset(preset);
            Config.SkillCapTier6.ApplyPreset(preset);
            Config.SkillCapTier7Plus.ApplyPreset(preset);
            Config.SkillCapHeroes.ApplyPreset(preset);

            // Totals
            Config.RetinueSkillTotalBonus.ApplyPreset(preset);
            Config.SkillTotalTier0.ApplyPreset(preset);
            Config.SkillTotalTier1.ApplyPreset(preset);
            Config.SkillTotalTier2.ApplyPreset(preset);
            Config.SkillTotalTier3.ApplyPreset(preset);
            Config.SkillTotalTier4.ApplyPreset(preset);
            Config.SkillTotalTier5.ApplyPreset(preset);
            Config.SkillTotalTier6.ApplyPreset(preset);
            Config.SkillTotalTier7Plus.ApplyPreset(preset);

            // Training time
            Config.TrainingTroopsTakesTime.ApplyPreset(preset);
            Config.TrainingTimeMultiplier.ApplyPreset(preset);
        }

        private void ShowSkillsPopup()
        {
            var title = L.T("wizard_skills_title", "Troop skills");
            var body = L.T("wizard_skills_body",
                "How quickly should your troops improve, and how demanding should training be?");

            var options = new List<WizardPopupOption>
            {
                new WizardPopupOption(
                    id: "skills_freeform",
                    title: L.T("wizard_skills_freeform", "Loose and forgiving"),
                    subtitle: L.T("wizard_skills_freeform_sub",
                        "High caps, generous totals and short training times."),
                    onSelected: () =>
                    {
                        ApplySkillPreset(ConfigPreset.Freeform);
                        ShowEquipmentUnlocksPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "skills_balanced",
                    title: L.T("wizard_skills_balanced", "Steady progress"),
                    subtitle: L.T("wizard_skills_balanced_sub",
                        "Skills grow at a comfortable pace with reasonable limits."),
                    onSelected: () =>
                    {
                        ApplySkillPreset(ConfigPreset.Default);
                        ShowEquipmentUnlocksPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "skills_realistic",
                    title: L.T("wizard_skills_realistic", "Tight and specialized"),
                    subtitle: L.T("wizard_skills_realistic_sub",
                        "Lower caps, stricter totals and longer training times."),
                    onSelected: () =>
                    {
                        ApplySkillPreset(ConfigPreset.Realistic);
                        ShowEquipmentUnlocksPopup();
                    }
                )
            };

            WizardPopup.Show(
                title,
                body,
                options,
                confirmText: L.T("wizard_continue", "Continue"),
                cancelText: L.T("wizard_cancel", "Skip"),
                onCancel: CompleteWithoutChanges
            );
        }

        // ──────────────────────────────
        //  Popup 3: Equipment unlocks
        // ──────────────────────────────

        private void ApplyEquipmentUnlockPreset(ConfigPreset preset)
        {
            Config.CopyAllSetsOnUnlock.ApplyPreset(preset);
            Config.AllEquipmentUnlocked.ApplyPreset(preset);
            Config.AllCultureEquipmentUnlocked.ApplyPreset(preset);
            Config.UnlockItemsFromKills.ApplyPreset(preset);
            Config.UnlockItemsFromDiscards.ApplyPreset(preset);
            Config.PlayerCultureUnlockBonus.ApplyPreset(preset);
        }

        private void ShowEquipmentUnlocksPopup()
        {
            var title = L.T("wizard_unlocks_title", "Equipment progression");
            var body = L.T("wizard_unlocks_body",
                "How should new gear become available for your troops?");

            var options = new List<WizardPopupOption>
            {
                new WizardPopupOption(
                    id: "unlocks_freeform",
                    title: L.T("wizard_unlocks_freeform", "Everything from day one"),
                    subtitle: L.T("wizard_unlocks_freeform_sub",
                        "Almost all gear is unlocked right away."),
                    onSelected: () =>
                    {
                        ApplyEquipmentUnlockPreset(ConfigPreset.Freeform);
                        ShowEquipmentCostsPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "unlocks_balanced",
                    title: L.T("wizard_unlocks_balanced", "Discover as you go"),
                    subtitle: L.T("wizard_unlocks_balanced_sub",
                        "Many items are available early, but the best gear appears gradually."),
                    onSelected: () =>
                    {
                        ApplyEquipmentUnlockPreset(ConfigPreset.Default);
                        ShowEquipmentCostsPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "unlocks_realistic",
                    title: L.T("wizard_unlocks_realistic", "Earn your gear"),
                    subtitle: L.T("wizard_unlocks_realistic_sub",
                        "Rare and powerful equipment must be earned through many battles."),
                    onSelected: () =>
                    {
                        ApplyEquipmentUnlockPreset(ConfigPreset.Realistic);
                        ShowEquipmentCostsPopup();
                    }
                )
            };

            WizardPopup.Show(
                title,
                body,
                options,
                confirmText: L.T("wizard_continue", "Continue"),
                cancelText: L.T("wizard_cancel", "Skip"),
                onCancel: CompleteWithoutChanges
            );
        }

        // ──────────────────────────────
        //  Popup 4: Equipment costs & time
        // ──────────────────────────────

        private void ApplyEquipmentCostPreset(ConfigPreset preset)
        {
            Config.EquippingTroopsCostsGold.ApplyPreset(preset);
            Config.EquipmentCostMultiplier.ApplyPreset(preset);
            Config.EquipmentCostReductionPerPurchase.ApplyPreset(preset);
            Config.EquippingTroopsTakesTime.ApplyPreset(preset);
            Config.EquipmentTimeMultiplier.ApplyPreset(preset);
        }

        private void ShowEquipmentCostsPopup()
        {
            var title = L.T("wizard_equipment_title", "Cost and time to equip");
            var body = L.T("wizard_equipment_body",
                "When you change your troops’ equipment, how much should gold and time matter?");

            var options = new List<WizardPopupOption>
            {
                new WizardPopupOption(
                    id: "equip_freeform",
                    title: L.T("wizard_equipment_freeform", "Instant and free"),
                    subtitle: L.T("wizard_equipment_freeform_sub",
                        "Changing gear costs little or nothing and takes almost no time."),
                    onSelected: () =>
                    {
                        ApplyEquipmentCostPreset(ConfigPreset.Freeform);
                        ShowKingdomTreePopup();
                    }
                ),
                new WizardPopupOption(
                    id: "equip_balanced",
                    title: L.T("wizard_equipment_balanced", "Costs gold, quick to apply"),
                    subtitle: L.T("wizard_equipment_balanced_sub",
                        "You’ll feel the price of gear, but orders stay reasonably quick."),
                    onSelected: () =>
                    {
                        ApplyEquipmentCostPreset(ConfigPreset.Default);
                        ShowKingdomTreePopup();
                    }
                ),
                new WizardPopupOption(
                    id: "equip_realistic",
                    title: L.T("wizard_equipment_realistic", "Expensive and takes time"),
                    subtitle: L.T("wizard_equipment_realistic_sub",
                        "Outfitting an army is a serious investment of gold and days."),
                    onSelected: () =>
                    {
                        ApplyEquipmentCostPreset(ConfigPreset.Realistic);
                        ShowKingdomTreePopup();
                    }
                )
            };

            WizardPopup.Show(
                title,
                body,
                options,
                confirmText: L.T("wizard_continue", "Continue"),
                cancelText: L.T("wizard_cancel", "Skip"),
                onCancel: CompleteWithoutChanges
            );
        }

        // ──────────────────────────────
        //  Popup 5: Kingdom troop tree
        // ──────────────────────────────

        private void ShowKingdomTreePopup()
        {
            var title =
                L.T("wizard_kingdom_tree_title", "Kingdom troop tree");
            var body = L.T("wizard_kingdom_tree_body",
                "Do you want your kingdom to have its own troop tree, or should everyone in your realm use your clan’s custom troops?");

            var options = new List<WizardPopupOption>
            {
                new WizardPopupOption(
                    id: "kingdom_separate",
                    title: L.T("wizard_kingdom_tree_separate", "Separate kingdom troops"),
                    subtitle: L.T("wizard_kingdom_tree_separate_sub",
                        "Your realm has its own troop line distinct from your clan’s."),
                    onSelected: () =>
                    {
                        Config.DisableKingdomTroops.Value = false;
                        ShowWhereVolunteersPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "kingdom_shared",
                    title: L.T("wizard_kingdom_tree_shared", "Share clan troops"),
                    subtitle: L.T("wizard_kingdom_tree_shared_sub",
                        "Your clan’s troops are used everywhere in your realm."),
                    onSelected: () =>
                    {
                        Config.DisableKingdomTroops.Value = true;
                        ShowWhereVolunteersPopup();
                    }
                )
            };

            WizardPopup.Show(
                title,
                body,
                options,
                confirmText: L.T("wizard_continue", "Continue"),
                cancelText: L.T("wizard_cancel", "Skip"),
                onCancel: CompleteWithoutChanges
            );
        }

        // ──────────────────────────────
        //  Popup 6: Where volunteers appear
        // ──────────────────────────────

        private void ShowWhereVolunteersPopup()
        {
            var title =
                L.T("wizard_where_title", "Where custom volunteers appear");
            var body = L.T("wizard_where_body",
                "How present do you want custom volunteers to be in the world?");

            var options = new List<WizardPopupOption>
            {
                new WizardPopupOption(
                    id: "where_your_fiefs",
                    title: L.T("wizard_where_your_fiefs", "Only in your fiefs"),
                    subtitle: L.T("wizard_where_your_fiefs_sub",
                        "Custom volunteers focus on your own settlements."),
                    onSelected: () =>
                    {
                        Config.RestrictToOwnedSettlements.Value = true;
                        Config.RestrictToSameCultureSettlements.Value = true;
                        Config.CustomVolunteersProportion.Value = 1.0f;
                        ShowWhoFieldsRetinuesPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "where_everywhere_mixed",
                    title: L.T("wizard_where_mixed", "Everywhere, mixed with locals"),
                    subtitle: L.T("wizard_where_mixed_sub",
                        "Custom volunteers can appear widely but share space with local troops."),
                    onSelected: () =>
                    {
                        Config.RestrictToOwnedSettlements.Value = false;
                        Config.RestrictToSameCultureSettlements.Value = false;
                        Config.CustomVolunteersProportion.Value = 0.5f;
                        ShowWhoFieldsRetinuesPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "where_everywhere",
                    title: L.T("wizard_where_everywhere", "Everywhere, all-in"),
                    subtitle: L.T("wizard_where_everywhere_sub",
                        "Custom volunteers can replace most local troops where they are allowed."),
                    onSelected: () =>
                    {
                        Config.RestrictToOwnedSettlements.Value = false;
                        Config.RestrictToSameCultureSettlements.Value = false;
                        Config.CustomVolunteersProportion.Value = 1.0f;
                        ShowWhichLordsRecruitPopup();
                    }
                )
            };

            WizardPopup.Show(
                title,
                body,
                options,
                confirmText: L.T("wizard_continue", "Continue"),
                cancelText: L.T("wizard_cancel", "Skip"),
                onCancel: CompleteWithoutChanges
            );
        }

        // ──────────────────────────────
        //  Popup 7: Who fields retinues
        // ──────────────────────────────

        private void ShowWhoFieldsRetinuesPopup()
        {
            var title =
                L.T("wizard_field_title", "Who fields custom troops");
            var body = L.T("wizard_field_body",
                "Who should be able to lead custom troops in their parties?");

            var options = new List<WizardPopupOption>
            {
                new WizardPopupOption(
                    id: "field_you_only",
                    title: L.T("wizard_field_you_only", "Only you"),
                    subtitle: L.T("wizard_field_you_only_sub",
                        "Retinues are your personal specialty. Other lords use more traditional forces."),
                    onSelected: () =>
                    {
                        // TODO: hook into your internal logic for which parties can have retinues.
                        ShowWhichLordsRecruitPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "field_you_and_realm",
                    title: L.T("wizard_field_realm", "You and your realm"),
                    subtitle: L.T("wizard_field_realm_sub",
                        "Lords who serve your realm can also lead custom troops."),
                    onSelected: () =>
                    {
                        // TODO: hook into your internal logic for which parties can have retinues.
                        ShowWhichLordsRecruitPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "field_everyone",
                    title: L.T("wizard_field_everyone", "Many lords"),
                    subtitle: L.T("wizard_field_everyone_sub",
                        "Custom troops can eventually spread into the forces of many rulers."),
                    onSelected: () =>
                    {
                        // TODO: hook into your internal logic for which parties can have retinues.
                        ShowWhichLordsRecruitPopup();
                    }
                )
            };

            WizardPopup.Show(
                title,
                body,
                options,
                confirmText: L.T("wizard_continue", "Continue"),
                cancelText: L.T("wizard_cancel", "Skip"),
                onCancel: CompleteWithoutChanges
            );
        }

        // ──────────────────────────────
        //  Popup 8: Which lords can recruit
        // ──────────────────────────────

        private void ShowWhichLordsRecruitPopup()
        {
            var title =
                L.T("wizard_recruit_title", "Who can recruit custom troops");
            var body = L.T("wizard_recruit_body",
                "Who should be able to recruit your custom troops from settlements?");

            var options = new List<WizardPopupOption>
            {
                new WizardPopupOption(
                    id: "recruit_none",
                    title: L.T("wizard_recruit_none", "Only you"),
                    subtitle: L.T("wizard_recruit_none_sub",
                        "Custom recruitment stays exclusive to you."),
                    onSelected: () =>
                    {
                        Config.AllLordsCanRecruitCustomTroops.Value = false;
                        Config.VassalLordsCanRecruitCustomTroops.Value = false;
                        ShowFinalPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "recruit_vassals",
                    title: L.T("wizard_recruit_vassals", "You and your realm"),
                    subtitle: L.T("wizard_recruit_vassals_sub",
                        "Lords in your realm can also recruit your custom troops."),
                    onSelected: () =>
                    {
                        Config.AllLordsCanRecruitCustomTroops.Value = false;
                        Config.VassalLordsCanRecruitCustomTroops.Value = true;
                        ShowFinalPopup();
                    }
                ),
                new WizardPopupOption(
                    id: "recruit_all",
                    title: L.T("wizard_recruit_all", "Any lord in your lands"),
                    subtitle: L.T("wizard_recruit_all_sub",
                        "Any lord visiting your fiefs may recruit your custom troops there."),
                    onSelected: () =>
                    {
                        Config.AllLordsCanRecruitCustomTroops.Value = true;
                        Config.VassalLordsCanRecruitCustomTroops.Value = true;
                        ShowFinalPopup();
                    }
                )
            };

            WizardPopup.Show(
                title,
                body,
                options,
                confirmText: L.T("wizard_finish", "Finish"),
                cancelText: L.T("wizard_cancel", "Skip"),
                onCancel: CompleteWithoutChanges
            );
        }

        // ──────────────────────────────
        //  Final / skipped
        // ──────────────────────────────

        private void ShowFinalPopup()
        {
            _hasRun = true;
            Config.SaveSettings();

            var data = new TextInquiryData(
                L.T("wizard_final_title", "Retinues setup complete").ToString(),
                L.T("wizard_final_body",
                    "Your starting settings for Retinues are ready.\n\n"
                    + "You can change any of these choices later from the configuration menu.")
                    .ToString(),
                true,
                false,
                L.T("wizard_ok", "OK").ToString(),
                string.Empty,
                null,
                null
            );

            InformationManager.ShowTextInquiry(data, false);
        }

        private void CompleteWithoutChanges()
        {
            _hasRun = true;

            var data = new TextInquiryData(
                L.T("wizard_skipped_title", "Retinues setup skipped").ToString(),
                L.T("wizard_skipped_body",
                    "No changes were made.\n\n"
                    + "You can open the Retinues configuration menu at any time to adjust things in detail.")
                    .ToString(),
                true,
                false,
                L.T("wizard_ok", "OK").ToString(),
                string.Empty,
                null,
                null
            );

            InformationManager.ShowTextInquiry(data, false);
        }
    }
}
