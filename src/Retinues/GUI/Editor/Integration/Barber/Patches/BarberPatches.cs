
using System;
using HarmonyLib;
using Retinues.Utilities;
using SandBox.GauntletUI;

namespace Retinues.UI.Screens
{
    [HarmonyPatch(typeof(GauntletBarberScreen))]
    internal static class GauntletBarberScreenPatches
    {
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