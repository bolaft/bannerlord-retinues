using System;
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
    public class DependenciesBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void SyncData(IDataStore dataStore) { }

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

        enum DependencyStatus
        {
            OK,
            Missing,
            Error,
        }

        /// <summary>
        /// Checks whether all required subsystems initialized correctly.
        /// If not, logs errors and shows a popup notification.
        /// </summary>
        private void OnGameLoadFinished()
        {
            static DependencyStatus CheckDependency(bool enabled, string moduleId)
            {
                if (enabled)
                    return DependencyStatus.OK;
                else if (ModuleChecker.IsLoaded(moduleId))
                    return DependencyStatus.Error;
                else
                    return DependencyStatus.Missing;
            }

            try
            {
                DependencyStatus uiExtenderStatus = CheckDependency(
                    SubModule.UIExtenderExEnabled,
                    "Bannerlord.UIExtenderEx"
                );
                DependencyStatus harmonyStatus = CheckDependency(
                    SubModule.HarmonyPatchesApplied,
                    "Bannerlord.Harmony"
                );

                // If both dependencies are OK, nothing to do
                if (uiExtenderStatus == DependencyStatus.OK && harmonyStatus == DependencyStatus.OK)
                {
                    Log.Debug("All dependencies initialized correctly.");
                    return;
                }

                // Build detailed status parts for logging and the popup
                var parts = new System.Collections.Generic.List<string>();
                if (uiExtenderStatus != DependencyStatus.OK)
                    parts.Add($"UIExtenderEx: {uiExtenderStatus}");
                if (harmonyStatus != DependencyStatus.OK)
                    parts.Add($"Harmony: {harmonyStatus}");

                var details = parts.Count > 0 ? string.Join("; ", parts) : "none";

                Log.Error(
                    $"Module started without full initialization. UIExtenderExEnabled={SubModule.UIExtenderExEnabled}, HarmonyPatchesApplied={SubModule.HarmonyPatchesApplied}. Details: {details}."
                );

                Notifications.Popup(
                    L.T("retinues_init_error_title", "Retinues Dependency Error"),
                    L.T(
                            "retinues_init_error_body_detailed",
                            "Retinues mod dependencies failed to initialize properly:\n\n{DETAILS}.\n\nThe mod will not function correctly until the dependencies are correct."
                        )
                        .SetTextVariable("DETAILS", details)
                );
            }
            catch (Exception e)
            {
                Log.Exception(e, "Error during dependency check.");
                return;
            }
        }
    }
}
