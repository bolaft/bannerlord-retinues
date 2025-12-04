using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.GUI.Editor;
using Retinues.Utils;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

namespace Retinues.GUI.Menus
{
    [HarmonyPatch(typeof(EscapeMenuVM))]
    internal static class EscapeMenuPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(
            MethodType.Constructor,
            [typeof(IEnumerable<EscapeMenuItemVM>), typeof(TextObject)]
        )]
        private static void Postfix(EscapeMenuVM __instance)
        {
            try
            {
                if (Config.EnableGlobalEditor == false)
                    return; // Feature disabled

                var title = L.T("troop_editor_button", "Troop Editor");
                static Tuple<bool, TextObject> notDisabled() =>
                    new(false, new TextObject(string.Empty));

                void OnExecute(object _)
                {
                    try
                    {
                        // Find the vanilla "Resume/Continue" item and execute it.
                        // Prefer the first menu item that is NOT our own button.
                        var resume = __instance.MenuItems.FirstOrDefault(mi =>
                            !string.Equals(
                                mi.ActionText,
                                title.ToString(),
                                StringComparison.Ordinal
                            )
                        );

                        // Fallback: just take the first item
                        resume ??= __instance.MenuItems.FirstOrDefault();

                        // Trigger the exact same path vanilla uses to close the ESC menu
                        resume?.ExecuteAction();

                        // Launch Studio right after resuming
                        ClanScreen.LaunchEditor();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to open Troop Editor screen: {e}");
                    }
                }

                var item = new EscapeMenuItemVM(
                    title,
                    onExecute: OnExecute,
                    identifier: "ret_troop_editor",
                    getIsDisabledAndReason: notDisabled,
                    isPositiveBehaviored: false
                );

                int insertIndex = Math.Min(3, __instance.MenuItems.Count);
                __instance.MenuItems.Insert(insertIndex, item);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to inject Troop Editor into EscapeMenuVM: {e}");
            }
        }
    }
}
