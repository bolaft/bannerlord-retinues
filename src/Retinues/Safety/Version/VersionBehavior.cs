using System.Linq;
using System.Reflection;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Safety.Version
{
    /// <summary>
    /// Campaign behavior for tracking and displaying the Retinues mod version in save files.
    /// Updates version info on save and logs it after game load.
    /// </summary>
    [SafeClass]
    public class VersionBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string _retinuesVersion;

        /// <summary>
        /// Syncs the Retinues mod version to and from the campaign save file.
        /// Updates to current version on save.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                // Update to current version on save
                _retinuesVersion = ModuleChecker.GetModule("Retinues").Version;
            }

            dataStore.SyncData("Retinues_Version", ref _retinuesVersion);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers event listener for game load finished to log the mod version.
        /// </summary>
        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this,
                OnGameLoadFinished
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Logs the Retinues mod version after game load.
        /// </summary>
        private void OnGameLoadFinished()
        {
            try
            {
                string currentVersionString = ModuleChecker.GetModule("Retinues").Version;

                // Old saves or missing data: treat as "no stored version yet".
                if (string.IsNullOrWhiteSpace(_retinuesVersion))
                {
                    _retinuesVersion = currentVersionString;
                    Log.Info(
                        $"No Retinues version stored in save; assuming current version {_retinuesVersion}."
                    );
                    return;
                }

                if (_retinuesVersion == null)
                {
                    Log.Info(
                        $"Retinues version in save matches current version {_retinuesVersion}."
                    );
                    return;
                }

                string saveVersionString = _retinuesVersion;

                try
                {
                    if (saveVersionString != currentVersionString)
                    {
                        Log.Info($"Save File: Retinues {saveVersionString}");

                        try
                        {
                            int? saveVersionInt = GetPatchNumber(saveVersionString);
                            int? currentVersionInt = GetPatchNumber(currentVersionString);

                            if (saveVersionInt + 1 == currentVersionInt)
                            {
                                VersionUpdatePopup(currentVersionString, saveVersionString);
                                return;
                            }
                        }
                        catch
                        {
                            // ignore
                        }

                        // special case for first time with new versioning
                        if (saveVersionString == "unknown" && currentVersionString.EndsWith("10"))
                        {
                            VersionUpdatePopup(currentVersionString, saveVersionString);
                            return;
                        }

                        // Otherwise, show mismatch popup
                        VersionMismatchPopup(currentVersionString, saveVersionString);
                    }
                }
                catch (System.Exception ex)
                {
                    VersionMismatchPopup(currentVersionString, saveVersionString);
                    Log.Exception(ex);
                }
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// Displays a popup notification for a Retinues version update in the save file.
        /// </summary>
        private void VersionUpdatePopup(string currentVersionString, string saveVersionString)
        {
            if (saveVersionString == ModuleChecker.UnknownVersionString)
            {
                Notifications.Popup(
                    L.T("retinues_update_title", "Retinues Update"),
                    L.T(
                            "retinues_version_update_text_no_current",
                            "The Retinues mod version in this save file has been updated to the next version ({CURRENT_VERSION}). Your save data has been automatically migrated to the new version.\n\nAs a safety precaution, check that everything is as it should before overwriting your save.\n\nIf you notice any issues and wish to go back to the previous version, do not save and download the version you were previously using from Nexus Mods."
                        )
                        .SetTextVariable("CURRENT_VERSION", currentVersionString)
                );
            }
            else
            {
                Notifications.Popup(
                    L.T("retinues_update_title", "Retinues Update"),
                    L.T(
                            "retinues_version_update_text",
                            "The Retinues mod version in this save file has been updated to the next version ({CURRENT_VERSION}). Your save data has been automatically migrated to the new version.\n\nAs a safety precaution, check that everything is as it should before overwriting your save.\n\nIf you notice any issues and wish to go back to the previous version, do not save and download the {SAVE_VERSION} file from Nexus Mods."
                        )
                        .SetTextVariable("SAVE_VERSION", saveVersionString)
                        .SetTextVariable("CURRENT_VERSION", currentVersionString)
                );
            }
        }

        /// <summary>
        /// Displays a popup notification for a Retinues version mismatch in the save file.
        /// </summary>
        private void VersionMismatchPopup(string currentVersionString, string saveVersionString)
        {
            if (saveVersionString == ModuleChecker.UnknownVersionString)
            {
                Notifications.Popup(
                    L.T("retinues_version_change_title", "Retinues Version Change"),
                    L.T(
                            "retinues_version_change_text_no_current",
                            "The Retinues mod version in this save file does not match the current mod version ({CURRENT_VERSION}).\n\nAs a safety precaution, check that everything is as it should before overwriting your save.\n\nIf you notice any issues and wish to go back to the previous version, do not save and download the version you were previously using from Nexus Mods."
                        )
                        .SetTextVariable("CURRENT_VERSION", currentVersionString)
                );
            }
            else
            {
                Notifications.Popup(
                    L.T("retinues_version_change_title", "Retinues Version Change"),
                    L.T(
                            "retinues_version_change_text",
                            "The Retinues mod version in this save file does not match the current mod version ({CURRENT_VERSION}).\n\nAs a safety precaution, check that everything is as it should before overwriting your save.\n\nIf you notice any issues and wish to go back to the previous version, do not save and download the {SAVE_VERSION} file from Nexus Mods."
                        )
                        .SetTextVariable("SAVE_VERSION", saveVersionString)
                        .SetTextVariable("CURRENT_VERSION", currentVersionString)
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Helpers                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Extracts the trailing numeric patch component from a version string.
        /// </summary>
        private static int? GetPatchNumber(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return null;

            // Take last dotted segment, then keep only leading digits.
            string lastSegment = versionString.Split('.').Last();
            string digitPrefix = new([.. lastSegment.TakeWhile(char.IsDigit)]);

            if (string.IsNullOrEmpty(digitPrefix))
                return null;

            return int.TryParse(digitPrefix, out int patch) ? patch : null;
        }
    }
}
