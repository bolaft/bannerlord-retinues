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
            Log.Info($"Save File: Retinues {_retinuesVersion ?? "unknown"}");
        }
    }
}
