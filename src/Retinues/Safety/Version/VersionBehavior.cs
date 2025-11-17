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
            string saveVersionString = _retinuesVersion ?? "unknown";
            string currentVersionString = ModuleChecker.GetModule("Retinues").Version;

            if (saveVersionString != currentVersionString)
            {
                int saveVersionInt = int.Parse(saveVersionString.Split('.').Last());
                int currentVersionInt = int.Parse(currentVersionString.Split('.').Last());

                Log.Info($"Save File: Retinues {_retinuesVersion ?? "unknown"}");

                if (saveVersionInt + 1 == currentVersionInt)
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
                    return;
                }
                else if (currentVersionInt != saveVersionInt)
                {
                    Notifications.Popup(
                        L.T("retinues_version_change_title", "Retinues Version Change"),
                        L.T(
                                "retinues_version_change_text",
                                "The Retinues mod version in this save file does not match the current mod version ({CURRENT_VERSION}). This may cause issues.\n\nAs a safety precaution, check that everything is as it should before overwriting your save.\n\nIf you notice any issues and wish to go back to the previous version, do not save and download the {SAVE_VERSION} file from Nexus Mods."
                            )
                            .SetTextVariable("SAVE_VERSION", saveVersionString)
                            .SetTextVariable("CURRENT_VERSION", currentVersionString)
                    );
                }
            }
        }
    }
}
