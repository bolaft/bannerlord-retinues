using System.Text.RegularExpressions;
using HarmonyLib;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu;

namespace Retinues.Core.Game.Menu.Patches
{
    /// <summary>Show whole hours in wait menus (e.g., "3 hours" instead of "3.0 hours").</summary>
    [HarmonyPatch(typeof(GameMenuItemProgressVM), "Refresh")]
    internal static class GameMenuItemProgressVM_Refresh_Patch
    {
        private static readonly Regex TrailingPointZero = new(
            @"(\d+)([.,])0(\D|$)",
            RegexOptions.Compiled
        );

        [SafeMethod]
        static void Postfix(GameMenuItemProgressVM __instance)
        {
            // Replace any X.0 or X,0 with X inside the ProgressText the VM just computed.
            var textProp = __instance.GetType().GetProperty("ProgressText");
            if (textProp?.GetValue(__instance) is string s && !string.IsNullOrEmpty(s))
            {
                var fixedText = TrailingPointZero.Replace(s, "$1$3");
                if (!ReferenceEquals(fixedText, s))
                    textProp.SetValue(__instance, fixedText);
            }
        }
    }
}
