using System.Linq;
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
                string currentVersionRaw = ModuleChecker.GetModule("Retinues").Version;

                Log.Info($"Current Retinues version: {currentVersionRaw}");
                Log.Info($"Retinues version in save: {_retinuesVersion}");

                // Old saves or missing data: treat as "no stored version yet".
                if (string.IsNullOrWhiteSpace(_retinuesVersion))
                {
                    _retinuesVersion = currentVersionRaw;
                    Log.Info(
                        $"No Retinues version stored in save; assuming current version {_retinuesVersion}."
                    );
                    return;
                }

                string saveVersionRaw = _retinuesVersion;

                // Normalized versions: strip leading 'v'/'V', trim, but keep "unknown" as-is.
                string currentVersion = NormalizeVersion(currentVersionRaw);
                string saveVersion = NormalizeVersion(saveVersionRaw);

                // Exact match (ignoring leading 'v'): nothing to do.
                if (
                    !string.IsNullOrEmpty(saveVersion)
                    && saveVersion != ModuleChecker.UnknownVersionString
                    && saveVersion == currentVersion
                )
                {
                    Log.Info(
                        $"Retinues version in save matches current version {currentVersionRaw}."
                    );
                    return;
                }

                Log.Info($"Save File: Retinues {saveVersionRaw}");

                try
                {
                    // New main rule:
                    // If either mod minor += 1 OR mod major += 1 (same BL series), show update popup.
                    // Anything else => mismatch popup.

                    if (ShouldShowUpdatePopup(saveVersion, currentVersion))
                    {
                        VersionUpdatePopup(currentVersionRaw, saveVersionRaw);
                        return;
                    }

                    // Otherwise, show mismatch popup
                    VersionMismatchPopup(currentVersionRaw, saveVersionRaw);
                }
                catch (System.Exception ex)
                {
                    VersionMismatchPopup(currentVersionRaw, saveVersionRaw);
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
        /// Normalize a version string for comparison:
        /// trims, strips leading 'v'/'V', leaves "unknown" unchanged.
        /// </summary>
        private static string NormalizeVersion(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return versionString;

            if (versionString == ModuleChecker.UnknownVersionString)
                return versionString;

            return versionString.Trim().TrimStart('v', 'V');
        }

        /// <summary>
        /// Determines whether an update popup should be shown, based on
        /// the old and new (normalized) version strings.
        /// </summary>
        private static bool ShouldShowUpdatePopup(
            string saveVersionString,
            string currentVersionString
        )
        {
            // From here on we apply the generic rule:
            // if either minor += 1 OR major += 1, show update.
            if (
                !TryParseVersion(
                    saveVersionString,
                    out int saveBlMajor,
                    out int saveBlMinor,
                    out int saveModMajor,
                    out int saveModMinor
                )
            )
                return false;

            if (
                !TryParseVersion(
                    currentVersionString,
                    out int curBlMajor,
                    out int curBlMinor,
                    out int curModMajor,
                    out int curModMinor
                )
            )
                return false;

            // Changing Bannerlord series is treated as a mismatch, not a normal update.
            if (saveBlMajor != curBlMajor || saveBlMinor != curBlMinor)
                return false;

            bool isMinorStep = curModMajor == saveModMajor && curModMinor == saveModMinor + 1;

            bool isMajorStep = curModMajor == saveModMajor + 1 && curModMinor == saveModMinor;

            return isMinorStep || isMajorStep;
        }

        /// <summary>
        /// Parses a normalized version string "A.B.X.Y" into BL + mod components.
        /// A/B = BL major/minor, X/Y = mod major/minor.
        /// </summary>
        private static bool TryParseVersion(
            string versionString,
            out int blMajor,
            out int blMinor,
            out int modMajor,
            out int modMinor
        )
        {
            blMajor = blMinor = modMajor = modMinor = 0;

            if (string.IsNullOrWhiteSpace(versionString))
                return false;

            if (versionString == ModuleChecker.UnknownVersionString)
                return false;

            string normalized = versionString.Trim().TrimStart('v', 'V');

            string[] parts = normalized.Split('.');
            if (parts.Length < 4)
                return false;

            return int.TryParse(parts[0], out blMajor)
                && int.TryParse(parts[1], out blMinor)
                && int.TryParse(parts[2], out modMajor)
                && int.TryParse(parts[3], out modMinor);
        }
    }
}
