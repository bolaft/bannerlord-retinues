using System;
using System.Collections.Generic;
using Retinues.Framework.Behaviors;
using Retinues.Interface.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Retinues.Behaviors.Presets
{
    /// <summary>
    /// Shows a one-time preset selection popup for campaigns that have never had
    /// Retinues settings applied — either on a fresh game start (after character
    /// creation) or when Retinues is first loaded into an existing save.
    /// </summary>
    public sealed class PresetSelectionBehavior : BaseCampaignBehavior<PresetSelectionBehavior>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string DataStoreKey = "Retinues_PresetSelected";

        private bool _presetSelected;

        /// <summary>
        /// True once the player has chosen a preset for this campaign.
        /// Used by other behaviors to defer work until the preset is known.
        /// </summary>
        public static bool IsPresetSelected { get; private set; }

        /// <summary>
        /// Persists whether the player has already been shown the preset prompt.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData(DataStoreKey, ref _presetSelected);
            IsPresetSelected = _presetSelected;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// New game: show prompt after character creation.
        /// </summary>
        protected override void OnCharacterCreationIsOver() => TryShowPrompt();

        /// <summary>
        /// Existing save without Retinues: show prompt after load.
        /// </summary>
        protected override void OnGameLoadFinished() => TryShowPrompt();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void TryShowPrompt()
        {
            if (_presetSelected)
                return;

            try
            {
                var choices = new List<(TextObject, Action)>
                {
                    (L.T("preset_selection_default", "Default"), () => Apply(SettingsPreset.Default)),
                    (
                        L.T("preset_selection_freeform", "Freeform"),
                        () => Apply(SettingsPreset.Freeform)
                    ),
                    (
                        L.T("preset_selection_realistic", "Realistic"),
                        () => Apply(SettingsPreset.Realistic)
                    ),
                };

                // Settings persist globally across campaigns. If the player has already configured
                // Retinues, offer a way to keep their current settings instead of forcing a preset
                // that would overwrite them.
                if (ConfigurationManager.HasConfiguredBefore)
                {
                    choices.Add(
                        (
                            L.T("preset_selection_keep", "Keep Current Settings"),
                            KeepCurrentSettings
                        )
                    );
                }

                Inquiries.MultiChoicePopup(
                    title: L.T("preset_selection_title", "Welcome to Retinues"),
                    description: L.T(
                        "preset_selection_description",
                        "Please select the settings that best match your playstyle.\n\nDEFAULT: A balanced first-playthrough experience. A full tree of clan troops are unlocked with your first fief, equipment has a cost, and skill points must be bought with experience earned in battle.\n\nFREEFORM: An unrestricted sandbox editor. No costs or unlock systems. Custom clan troops are available right from the start and can be recruited anywhere.\n\nREALISTIC: A grounded experience with harsher constraints. Troop editing is restricted to owned fiefs, equipments are limited in weight and value, equipping items and training skills take time, and troop trees must be built from scratch.\n\nYou can fine-tune any individual setting at any time from the Settings tab in the Troops screen."
                    ),
                    choices: choices
                );
            }
            catch (Exception e)
            {
                Log.Exception(e, "PresetSelectionBehavior.TryShowPrompt failed.");
            }
        }

        private void Apply(SettingsPreset preset)
        {
            try
            {
                ConfigurationManager.ApplyPreset(preset);
                _presetSelected = true;
                IsPresetSelected = true;
                Log.Info($"PresetSelectionBehavior: applied preset '{preset}'.");
            }
            catch (Exception e)
            {
                Log.Exception(e, "PresetSelectionBehavior.Apply failed.");
            }
        }

        /// <summary>
        /// Dismisses the prompt for this campaign without touching the (globally persisted) settings.
        /// </summary>
        private void KeepCurrentSettings()
        {
            _presetSelected = true;
            IsPresetSelected = true;
            Log.Info("PresetSelectionBehavior: kept current settings.");
        }
    }
}
