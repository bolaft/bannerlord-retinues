using System;
using HarmonyLib;
using Retinues.Utilities;
using SandBox.GauntletUI;

namespace Retinues.GUI.Editor.Integration.Barber.Patches
{
    /// <summary>
    /// Patches GauntletBarberScreen to integrate barber session lifecycle with Retinues.
    /// </summary>
    [HarmonyPatch(typeof(GauntletBarberScreen))]
    internal static class GauntletBarberScreenPatches
    {
        /// <summary>
        /// Runs after barber finalization to notify Retinues the barber closed.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("OnFinalize")]
        private static void OnFinalizePostfix()
        {
            try
            {
                BarberHelper.OnBarberClosed();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        /// <summary>
        /// Runs each frame to update face generation while a barber session is active.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("OnFrameTick")]
        private static void OnFrameTickPostfix(GauntletBarberScreen __instance, float dt)
        {
            try
            {
                if (!BarberHelper.HasActiveSession)
                    return;

                var handler = __instance.Handler;
                BarberHelper.PrimeFaceGen(handler);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
