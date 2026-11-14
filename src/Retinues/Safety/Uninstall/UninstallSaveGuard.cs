using System;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Safety.Uninstall
{
    [HarmonyPatch(typeof(SaveHandler), "SetSaveArgs")]
    internal static class UninstallSaveGuard
    {
        internal static void EnsureInstalled()
        {
            var target = AccessTools.Method(typeof(SaveHandler), "SetSaveArgs");
            if (target == null || Harmony.GetPatchInfo(target)?.Prefixes.Any(p => p.PatchMethod.DeclaringType == typeof(UninstallSaveGuard)) != true)
                throw new InvalidOperationException("The uninstall save guard is unavailable on this game version.");
            var write = AccessTools.Method(typeof(TaleWorlds.Core.Game), "Save");
            if (write == null || Harmony.GetPatchInfo(write)?.Prefixes.Any(p => p.PatchMethod.DeclaringType == typeof(UninstallWriteGuard)) != true)
                throw new InvalidOperationException("The uninstall final save guard is unavailable on this game version.");
        }

        // All normal manual, quick and auto saves enter the queue through this method.
        [HarmonyPrefix]
        private static bool Prefix() => CanSave(includeBehaviorSnapshots: false);

        internal static bool CanSave(bool includeBehaviorSnapshots)
        {
            if (!UninstallBehavior.IsPrepared && !UninstallBehavior.HasFailed) return true;
            try
            {
                if (UninstallBehavior.HasFailed)
                    throw new InvalidOperationException("The previous cleanup was incomplete. Reload the pre-uninstall backup.");
                UninstallCleanup.VerifyPreparedSave(includeBehaviorSnapshots);
                return true;
            }
            catch (Exception error)
            {
                UninstallHooks.Log("[Uninstall] Save blocked: " + error);
                InformationManager.DisplayMessage(new InformationMessage(
                    "Retinues: uninstall verification failed; this save was cancelled. Reload your pre-uninstall backup and report debug.log."));
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(TaleWorlds.Core.Game), "Save")]
    internal static class UninstallWriteGuard
    {
        // Recheck after OnBeforeSave/SyncData, immediately before the engine writes data.
        // Report failure through the native completion callback so its queue is released.
        [HarmonyPrefix, HarmonyPriority(Priority.Last)]
        private static bool Prefix(Action<SaveResult> __3)
        {
            if (UninstallSaveGuard.CanSave(includeBehaviorSnapshots: true)) return true;
            __3?.Invoke(SaveResult.GeneralFailure);
            return false;
        }
    }
}
