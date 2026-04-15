using Retinues.Framework.Behaviors;
using Retinues.Framework.Model.Persistence;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Backup
{
    /// <summary>
    /// When Retinues is loaded onto a save that did not previously contain Retinues data,
    /// automatically creates a backup save as soon as the game finishes loading —
    /// before any retinue behavior has had a chance to modify game state.
    /// </summary>
    public sealed class BackupSaveBehavior : BaseCampaignBehavior<BackupSaveBehavior>
    {
        /// <summary>
        /// Fires after the campaign finishes loading from disk.
        /// If Retinues was absent from this save, queue a named backup save.
        /// </summary>
        protected override void OnGameLoadFinished()
        {
            if (!MPersistenceBehavior.WasAbsentFromSave)
                return;

            var originalSlot = MBSaveLoad.ActiveSaveSlotName;
            var backupName = BuildBackupName(originalSlot);

            Log.Info(
                $"BackupSaveBehavior: Retinues was absent from save '{originalSlot}'. Creating backup '{backupName}'."
            );

            try
            {
                Campaign.Current.SaveHandler.SaveAs(backupName);

                Notifications.Message(
                    $"[Retinues] Backup save created as '{backupName}' before Retinues first activation.",
                    "#a0c4ffff"
                );
            }
            catch (System.Exception e)
            {
                Log.Exception(e, "BackupSaveBehavior: Failed to queue backup save.");
            }
        }

        // ─────────────────────────────────────────────────────── //

        private static string BuildBackupName(string originalSlot)
        {
            const string suffix = "_pre_retinues";
            const int maxTotal = 50;

            var baseName = string.IsNullOrEmpty(originalSlot) ? "Save" : originalSlot;

            int maxBase = maxTotal - suffix.Length;
            if (baseName.Length > maxBase)
                baseName = baseName.Substring(0, maxBase);

            return baseName + suffix;
        }
    }
}
