using System;
using Retinues.Behaviors;
using Retinues.Engine;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Module.Version
{
    /// <summary>
    /// Tracks the Retinues mod version in save files and compares it to the currently loaded version.
    /// - Persists the Retinues version into the save.
    /// - On load, compares saved vs current version.
    ///   * Same -> nothing.
    ///   * Direct upgrade -> "version upgraded" popup.
    ///   * Any other discrepancy -> "version discrepancy" popup.
    /// </summary>
    public class VersionBehavior : BaseCampaignBehavior
    {
        private string _savedVersion;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        const string DataStoreKey = "Retinues_Version";

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                if (dataStore.IsSaving)
                {
                    var mod = ModuleManager.GetModule("Retinues");
                    _savedVersion = mod?.Version ?? ModuleManager.UnknownVersionString;
                }

                dataStore.SyncData(DataStoreKey, ref _savedVersion);
            }
            catch (Exception e)
            {
                Log.Exception(e, "VersionBehavior.SyncData failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            // Once the game is fully loaded, check the saved vs current version.
            Hook(BehaviorEvent.GameLoadFinished, OnGameLoadFinished);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnGameLoadFinished()
        {
            try
            {
                var currentModule = ModuleManager.GetModule("Retinues");
                if (currentModule == null)
                {
                    Log.Warn("VersionBehavior: Retinues module not found by ModuleManager.");
                    return;
                }

                var currentVersionString =
                    currentModule.Version ?? ModuleManager.UnknownVersionString;
                var currentAppVersion = currentModule.AppVersion;

                Log.Info($"Retinues version (current): {currentVersionString}");
                Log.Info($"Retinues version (in save): {_savedVersion}");

                // Old saves or missing data: treat as "no stored version yet".
                if (
                    string.IsNullOrWhiteSpace(_savedVersion)
                    || _savedVersion == ModuleManager.UnknownVersionString
                )
                {
                    _savedVersion = currentVersionString;
                    Log.Info(
                        $"No Retinues version stored in save; assuming current version {_savedVersion}."
                    );
                    return;
                }

                var saveVersionString = _savedVersion;

                // Exact string match => nothing to do.
                if (
                    string.Equals(saveVersionString, currentVersionString, StringComparison.Ordinal)
                )
                {
                    Log.Info(
                        $"Retinues version in save matches current version {currentVersionString}."
                    );
                    return;
                }

                // Try to parse both versions into ApplicationVersion.
                if (
                    !TryParseAppVersion(saveVersionString, out var saveAppVersion)
                    || currentAppVersion == ApplicationVersion.Empty
                )
                {
                    // If parsing fails for either, treat as discrepancy.
                    ShowDiscrepancyPopup(currentVersionString, saveVersionString);
                    return;
                }

                // Decide whether this is a direct upgrade or a discrepancy.
                if (IsDirectUpgrade(saveAppVersion, currentAppVersion))
                {
                    ShowUpgradePopup(currentVersionString, saveVersionString);
                }
                else
                {
                    ShowDiscrepancyPopup(currentVersionString, saveVersionString);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "VersionBehavior.OnGameLoadFinished failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Popups                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ShowUpgradePopup(string currentVersionString, string saveVersionString)
        {
            var title = L.T("retinues_update_title", "Retinues Update");

            TextObject body;
            if (saveVersionString == ModuleManager.UnknownVersionString)
            {
                body = L.T(
                        "retinues_version_update_text_no_current",
                        "The Retinues mod version in this save file has been updated to a newer version ({CURRENT_VERSION}). Your save data has been automatically migrated.\n\nAs a safety precaution, check that everything is as it should before overwriting your save.\n\nIf for any reason you wish to go back to the previous version, download the version you were previously using from Nexus Mods."
                    )
                    .SetTextVariable("CURRENT_VERSION", currentVersionString);
            }
            else
            {
                body = L.T(
                        "retinues_version_update_text",
                        "The Retinues mod version in this save file has been updated to a newer version ({CURRENT_VERSION}). Your save data has been automatically migrated from {SAVE_VERSION}.\n\nAs a safety precaution, check that everything is as it should before overwriting your save.\n\nIf for any reason you wish to go back to the previous version, you can download the {SAVE_VERSION} file from Nexus Mods."
                    )
                    .SetTextVariable("SAVE_VERSION", saveVersionString)
                    .SetTextVariable("CURRENT_VERSION", currentVersionString);
            }

            Inquiries.Popup(title, body);
        }

        private static void ShowDiscrepancyPopup(
            string currentVersionString,
            string saveVersionString
        )
        {
            var title = L.T("retinues_version_change_title", "Retinues Version Change");

            TextObject body;
            if (saveVersionString == ModuleManager.UnknownVersionString)
            {
                body = L.T(
                        "retinues_version_change_text_no_current",
                        "The Retinues mod version in this save file does not match the current mod version ({CURRENT_VERSION}).\n\nAs a safety precaution, check that everything is as it should before overwriting your save.\n\nIf you notice any issues and wish to go back to the previous version, do not save and download the version you were previously using from Nexus Mods."
                    )
                    .SetTextVariable("CURRENT_VERSION", currentVersionString);
            }
            else
            {
                body = L.T(
                        "retinues_version_change_text",
                        "The Retinues mod version in this save file ({SAVE_VERSION}) does not match the current mod version ({CURRENT_VERSION}).\n\nAs a safety precaution, check that everything is as it should before overwriting your save.\n\nIf you notice any issues and wish to go back to the previous version, do not save and download the {SAVE_VERSION} file from Nexus Mods."
                    )
                    .SetTextVariable("SAVE_VERSION", saveVersionString)
                    .SetTextVariable("CURRENT_VERSION", currentVersionString);
            }

            Inquiries.Popup(title, body);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Helpers                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Parses a stored Retinues version string into an ApplicationVersion.
        /// Accepts both "v1.2.3.4" and bare "1.2.3.4"; treats invalid/unknown as failure.
        /// </summary>
        private static bool TryParseAppVersion(string versionString, out ApplicationVersion version)
        {
            version = ApplicationVersion.Empty;

            if (string.IsNullOrWhiteSpace(versionString))
                return false;

            if (versionString == ModuleManager.UnknownVersionString)
                return false;

            var s = versionString.Trim();

            // If it starts with a digit, prepend a letter so ApplicationVersion.FromString accepts it.
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
        /// Determines whether the current version is a "direct upgrade" from the save version.
        /// Rules:
        /// - Must be newer.
        /// - Direct minor step: same major, minor+1.
        /// - Or direct major step: major+1 and new minor == 0.
        /// The "major/minor" here are taken from ApplicationVersion.Revision / ChangeSet,
        /// which should encode the mod's own major/minor version.
        /// </summary>
        private static bool IsDirectUpgrade(
            ApplicationVersion saveVersion,
            ApplicationVersion currentVersion
        )
        {
            // If current isn't actually newer, treat as discrepancy.
            if (!currentVersion.IsNewerThan(saveVersion))
                return false;

            int saveMajor = saveVersion.Revision;
            int saveMinor = saveVersion.ChangeSet;
            int curMajor = currentVersion.Revision;
            int curMinor = currentVersion.ChangeSet;

            bool isMinorStep = curMajor == saveMajor && curMinor == saveMinor + 1;
            bool isMajorStep = curMajor == saveMajor + 1 && curMinor == 0;

            return isMinorStep || isMajorStep;
        }
    }
}
