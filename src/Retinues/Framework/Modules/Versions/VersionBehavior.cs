using System;
using System.IO;
using System.Text;
using Retinues.Framework.Behaviors;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Framework.Modules.Versions
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

        /// <summary>
        /// Synchronizes the saved version to save/load.
        /// </summary>
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
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called when game load finishes to compare saved vs current version and
        /// create a filesystem backup when the version has changed.
        /// </summary>
        protected override void OnGameLoadFinished()
        {
            try
            {
                var currentModule = ModuleManager.GetModule("Retinues");
                if (currentModule == null)
                {
                    Log.Warning("VersionBehavior: Retinues module not found by ModuleManager.");
                    return;
                }

                var currentVersionString =
                    currentModule.Version ?? ModuleManager.UnknownVersionString;
                var currentAppVersion = currentModule.AppVersion;

                Log.Debug($"Retinues version (current): {currentVersionString}");
                Log.Debug($"Retinues version (in save): {_savedVersion}");

                // Retinues was absent from this save: first-time installation.
                if (
                    string.IsNullOrWhiteSpace(_savedVersion)
                    || _savedVersion == ModuleManager.UnknownVersionString
                )
                {
                    var backupName = TryCreateFilesystemBackup(
                        MBSaveLoad.ActiveSaveSlotName,
                        previousVersion: null
                    );
                    if (backupName != null)
                        Notifications.Message(
                            $"[Retinues] Backup saved as '{backupName}' before first activation.",
                            "#a0c4ffff"
                        );

                    _savedVersion = currentVersionString;
                    Log.Debug(
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
                    Log.Debug(
                        $"Retinues version in save matches current version {currentVersionString}."
                    );
                    return;
                }

                // Version changed: back up the save file on disk before showing any popup.
                var versionBackupName = TryCreateFilesystemBackup(
                    MBSaveLoad.ActiveSaveSlotName,
                    previousVersion: saveVersionString
                );
                if (versionBackupName != null)
                    Notifications.Message(
                        $"[Retinues] Backup saved as '{versionBackupName}' before update.",
                        "#a0c4ffff"
                    );

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
                    ShowUpgradePopup(currentVersionString, saveVersionString);
                else
                    ShowDiscrepancyPopup(currentVersionString, saveVersionString);
            }
            catch (Exception e)
            {
                Log.Exception(e, "VersionBehavior.OnGameLoadFinished failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Popups                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shows the "version upgraded" popup.
        /// </summary>
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

        /// <summary>
        /// Shows the "version discrepancy" popup.
        /// </summary>
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
        //                         Helpers                        //
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Filesystem Backup                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Copies the loaded save file to a backup path inside the same Game Saves folder.
        /// Returns the backup slot name on success, or <c>null</c> if no copy was made
        /// (file not found, backup already exists, or an error occurred).
        /// </summary>
        private static string TryCreateFilesystemBackup(string slotName, string previousVersion)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                Log.Warning("VersionBehavior: Cannot create backup — active save slot is null.");
                return null;
            }

            // Never back up a file that is itself already a backup.
            if (slotName.Contains("_backup"))
            {
                Log.Info($"VersionBehavior: Skipping backup of '{slotName}' — already a backup.");
                return null;
            }

            var saveDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord",
                "Game Saves"
            );

            var sourcePath = Path.Combine(saveDir, slotName + ".sav");
            if (!File.Exists(sourcePath))
            {
                Log.Warning(
                    $"VersionBehavior: Cannot create backup — source not found: {sourcePath}"
                );
                return null;
            }

            var backupSlot = BuildBackupSlotName(slotName, previousVersion);
            var backupPath = Path.Combine(saveDir, backupSlot + ".sav");

            if (File.Exists(backupPath))
            {
                Log.Info($"VersionBehavior: Backup already exists at '{backupPath}', skipping.");
                return null;
            }

            try
            {
                File.Copy(sourcePath, backupPath);
                Log.Info($"VersionBehavior: Created backup '{backupPath}'.");
                return backupSlot;
            }
            catch (Exception e)
            {
                Log.Exception(e, $"VersionBehavior: Failed to copy save to '{backupPath}'.");
                return null;
            }
        }

        /// <summary>
        /// Builds the backup slot name from the original slot name and the previous Retinues version.
        /// Format: <c>{slot}_backup_vX.Y.Z</c> when a previous version is known,
        ///         <c>{slot}_backup</c> when Retinues was not present before.
        /// </summary>
        private static string BuildBackupSlotName(string slotName, string previousVersion)
        {
            if (
                string.IsNullOrWhiteSpace(previousVersion)
                || previousVersion == ModuleManager.UnknownVersionString
            )
                return slotName + "_backup";

            var v = previousVersion.TrimStart('v', 'V');
            return slotName + "_backup_v" + SanitizeVersionForFilename(v);
        }

        /// <summary>
        /// Strips characters that are invalid in Windows filenames from a version string.
        /// Keeps letters, digits, dots, hyphens and underscores; replaces everything else with '_'.
        /// </summary>
        private static string SanitizeVersionForFilename(string version)
        {
            if (string.IsNullOrEmpty(version))
                return version;

            var sb = new StringBuilder(version.Length);
            foreach (var c in version)
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            return sb.ToString();
        }
    }
}
