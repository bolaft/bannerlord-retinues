using System;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace OldRetinues.GUI.Helpers
{
    /// <summary>
    /// Shared guard around appearance-changing operations (culture/race/gender).
    /// Uses WCharacter.GetModel as a probe for FaceGen failures and reverts on error.
    /// </summary>
    [SafeClass]
    public static class AppearanceGuard
    {
        public readonly struct Snapshot
        {
            public readonly CultureObject Culture;
            public readonly int Race;
            public readonly bool IsFemale;

            public Snapshot(WCharacter troop)
            {
                if (troop == null)
                {
                    Culture = null;
                    Race = -1;
                    IsFemale = false;
                    return;
                }

                Culture = troop.Culture?.Base;
                Race = troop.Race;
                IsFemale = troop.IsFemale;
            }

            public void Restore(WCharacter troop)
            {
                if (troop == null)
                    return;

                try
                {
                    if (Culture != null)
                    {
                        troop.Culture = new WCulture(Culture);
                        BodyHelper.ApplyPropertiesFromCulture(troop, Culture);
                    }

                    if (Race >= 0)
                        troop.Race = Race;

                    troop.IsFemale = IsFemale;
                    troop.Body.EnsureOwnBodyRange();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        public static bool TryApply(
            WCharacter troop,
            int equipmentIndex,
            Func<bool> applyChange,
            Action onSuccess = null,
            bool retryOnFailure = true
        )
        {
            if (troop == null)
                return false;
            if (applyChange == null)
                return false;

            var snapshot = new Snapshot(troop);

            try
            {
                // applyChange should return false for "no-op" cases.
                if (!applyChange())
                    return false;

                if (!troop.TryGetModel(equipmentIndex, out var model, out var error))
                {
                    // We only treat AccessViolation as "invalid combo".
                    if (error is AccessViolationException)
                    {
                        Log.Info("AccessViolation detected when applying appearance change.");
                        snapshot.Restore(troop);
                        if (retryOnFailure)
                        {
                            Log.Info("Retrying appearance change.");
                            return TryApply(troop, equipmentIndex, applyChange, onSuccess, false);
                        }
                        ShowFailurePopup();
                    }
                    else
                    {
                        // Some other editor bug: revert, but use a generic message.
                        snapshot.Restore(troop);
                        Notifications.Popup(
                            L.T("appearance_error_title", "Appearance Error"),
                            L.T(
                                "appearance_error_body",
                                "An error occurred while updating this troop's appearance. The previous appearance has been restored. See log for details."
                            )
                        );
                    }

                    onSuccess?.Invoke();
                    return false;
                }

                if (model == null)
                {
                    Log.Info("Null model detected when applying appearance change.");
                    if (retryOnFailure)
                    {
                        Log.Info("Retrying appearance change.");
                        return TryApply(troop, equipmentIndex, applyChange, onSuccess, false);
                    }
                    // Should not normally happen if TryGetModel sets error on failure,
                    // but keep a defensive path.
                    snapshot.Restore(troop);
                    ShowFailurePopup();
                    onSuccess?.Invoke();
                    return false;
                }

                onSuccess?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                snapshot.Restore(troop);
                Notifications.Popup(
                    L.T("appearance_error_title", "Appearance Error"),
                    L.T(
                        "appearance_error_body",
                        "An error occurred while updating this troop's appearance. The previous appearance has been restored. See log for details."
                    )
                );
                onSuccess?.Invoke();
                return false;
            }
        }

        private static void ShowFailurePopup()
        {
            bool hasAltSpecies = HasAlternateSpecies();

            TextObject title;
            TextObject body;

            if (hasAltSpecies)
            {
                title = L.T("no_valid_model_title_species", "No Valid Model");
                body = L.T(
                    "no_valid_model_body_species",
                    "This combination of gender, culture and species does not have a valid model.\n\nThe previous appearance has been restored."
                );
            }
            else
            {
                title = L.T("no_valid_model_title", "No Valid Model");
                body = L.T(
                    "no_valid_model_body",
                    "This combination of gender and culture does not have a valid model.\n\nThe previous appearance has been restored."
                );
            }

            Notifications.Popup(title, body);
        }

        private static bool HasAlternateSpecies()
        {
            try
            {
                var names = TaleWorlds.Core.FaceGen.GetRaceNames();
                return names != null && names.Length > 1;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return false;
            }
        }
    }
}
