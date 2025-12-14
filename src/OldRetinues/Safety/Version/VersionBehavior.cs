using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace OldRetinues.Safety.Version
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

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                // Update to current version on save (string form, for display)
                var mod = ModuleChecker.GetModule("Retinues");
                _retinuesVersion = mod?.Version ?? ModuleChecker.UnknownVersionString;
            }

            dataStore.SyncData("Retinues_Version", ref _retinuesVersion);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        private void OnGameLoadFinished()
        {
            try
            {
                var currentModule = ModuleChecker.GetModule("Retinues");

                if (currentModule == null)
                {
                    Log.Warn("VersionBehavior: Retinues module not found in ModuleChecker.");
                    return;
                }

                string currentVersionRaw = currentModule.Version;
                ApplicationVersion currentAppVersion = currentModule.AppVersion;

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

                // If the stored version is literally "unknown", just go to mismatch popup logic.
                if (saveVersionRaw == ModuleChecker.UnknownVersionString)
                {
                    VersionMismatchPopup(currentVersionRaw, saveVersionRaw);
                    return;
                }

                // Try to parse both versions into ApplicationVersion.
                if (
                    !TryParseAppVersion(saveVersionRaw, out var saveAppVersion)
                    || currentAppVersion == ApplicationVersion.Empty
                )
                {
                    // If parsing fails for either, fall back to mismatch behavior.
                    VersionMismatchPopup(currentVersionRaw, saveVersionRaw);
                    return;
                }

                // Exact match (including change-set) => nothing to do.
                if (currentAppVersion.IsSame(saveAppVersion, checkChangeSet: true))
                {
                    Log.Info(
                        $"Retinues version in save matches current version {currentVersionRaw}."
                    );
                    return;
                }

                Log.Info($"Save File: Retinues {saveVersionRaw}");

                try
                {
                    if (ShouldShowUpdatePopup(saveAppVersion, currentAppVersion))
                    {
                        VersionUpdatePopup(currentVersionRaw, saveVersionRaw);
                    }
                    else
                    {
                        VersionMismatchPopup(currentVersionRaw, saveVersionRaw);
                    }
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Popups                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
        /// Robustly parse a stored Retinues version string into an ApplicationVersion.
        /// Accepts both "v1.2.3.4" and bare "1.2.3.4"; treats invalid as failure.
        /// </summary>
        private static bool TryParseAppVersion(string versionString, out ApplicationVersion version)
        {
            version = ApplicationVersion.Empty;

            if (string.IsNullOrWhiteSpace(versionString))
                return false;

            if (versionString == ModuleChecker.UnknownVersionString)
                return false;

            string s = versionString.Trim();

            // If someone ever stored a bare "1.2.3.4", add default release prefix.
            if (!char.IsLetter(s[0]))
                s = "v" + s;

            try
            {
                version = ApplicationVersion.FromString(s, defaultChangeSet: 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether an update popup should be shown, based on the old and new ApplicationVersions.
        /// </summary>
        private static bool ShouldShowUpdatePopup(
            ApplicationVersion saveVersion,
            ApplicationVersion currentVersion
        )
        {
            // If current isn't actually newer, nothing to do.
            if (!currentVersion.IsNewerThan(saveVersion))
                return false;

            // Changing Bannerlord series (major/minor) is treated as mismatch.
            if (
                saveVersion.Major != currentVersion.Major
                || saveVersion.Minor != currentVersion.Minor
            )
                return false;

            // Major   = BL major
            // Minor   = BL minor
            // Revision = mod major
            // ChangeSet = mod minor
            int saveModMajor = saveVersion.Revision;
            int saveModMinor = saveVersion.ChangeSet;
            int curModMajor = currentVersion.Revision;
            int curModMinor = currentVersion.ChangeSet;

            // Legacy -> 13.0 bridge (old 1.2.12.x / 1.3.3.x / 1.3.1.x -> 1.2.13.0 / 1.3.13.0).
            if (
                curModMajor == 13
                && curModMinor == 0
                && IsLegacyToNewBridge(saveVersion, currentVersion)
            )
            {
                return true;
            }

            bool isMinorStep = curModMajor == saveModMajor && curModMinor == saveModMinor + 1;
            bool isMajorStep = curModMajor == saveModMajor + 1 && curModMinor == saveModMinor;

            return isMinorStep || isMajorStep;
        }

        /// <summary>
        /// Special-case bridge from early schemes to the new "13.0" scheme.
        /// - New: 1.2.13.0 or 1.3.13.0
        /// - Old: 1.3.3.9, 1.3.3.10, 1.3.1.9, 1.3.1.10, 1.2.12.9, 1.2.12.10
        /// </summary>
        private static bool IsLegacyToNewBridge(
            ApplicationVersion saveVersion,
            ApplicationVersion currentVersion
        )
        {
            // Same BL series guaranteed by caller.
            // New is always ModMajor=13, ModMinor=0 here.

            // 1.2.*: 1.2.12.9 / 1.2.12.10
            if (
                currentVersion.Major == 1
                && currentVersion.Minor == 2
                && saveVersion.Revision == 12
                && (saveVersion.ChangeSet == 9 || saveVersion.ChangeSet == 10)
            )
            {
                return true;
            }

            // 1.3.*: 1.3.3.9 / 1.3.3.10 / 1.3.1.9 / 1.3.1.10
            if (currentVersion.Major == 1 && currentVersion.Minor == 3)
            {
                bool oldRevOk = saveVersion.Revision == 3 || saveVersion.Revision == 1;
                bool oldMinorOk = saveVersion.ChangeSet == 9 || saveVersion.ChangeSet == 10;

                if (oldRevOk && oldMinorOk)
                    return true;
            }

            return false;
        }
    }
}
