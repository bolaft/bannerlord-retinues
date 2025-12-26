using System;
using System.Reflection;
using HarmonyLib;
using Retinues.Utils;

namespace OldRetinues.Mods.NavalDLC
{
    public static class NavalDlcShipTradePatcher
    {
        public static void TryPatch(Harmony harmony)
        {
            try
            {
                var t = AccessTools.TypeByName(
                    "NavalDLC.CampaignBehaviors.ShipTradeCampaignBehavior"
                );
                var m = AccessTools.Method(
                    t,
                    "OnShipOwnerChanged",
                    [
                        AccessTools.TypeByName("TaleWorlds.CampaignSystem.Naval.Ship"),
                        typeof(TaleWorlds.CampaignSystem.Party.PartyBase),
                        AccessTools.TypeByName(
                            "TaleWorlds.CampaignSystem.Actions.ChangeShipOwnerAction+ShipOwnerChangeDetail"
                        ),
                    ]
                );

                if (t == null || m == null)
                {
                    Log.Warn(
                        "[NavalDLCCompat] ShipTradeCampaignBehavior.OnShipOwnerChanged not found; no patch applied."
                    );
                    return;
                }

                var fin = typeof(NavalDlcShipTradePatcher).GetMethod(
                    nameof(OnShipOwnerChanged_Finalizer),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                harmony.Patch(m, finalizer: new HarmonyMethod(fin));
                Log.Info(
                    "[NavalDLCCompat] Patched ShipTradeCampaignBehavior.OnShipOwnerChanged (finalizer)."
                );
            }
            catch (Exception ex)
            {
                Log.Exception(
                    ex,
                    "[NavalDLCCompat] Failed to patch ShipTradeCampaignBehavior.OnShipOwnerChanged."
                );
            }
        }

        // Harmony finalizer: return null to suppress the exception.
        private static Exception OnShipOwnerChanged_Finalizer(Exception __exception)
        {
            if (__exception == null)
                return null;

            Log.Error(
                $"[NavalDLCCompat] Suppressed crash in OnShipOwnerChanged: {__exception.GetType().Name}: {__exception.Message}"
            );
            return null;
        }
    }
}
