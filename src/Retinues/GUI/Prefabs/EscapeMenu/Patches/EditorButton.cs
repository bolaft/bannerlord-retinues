using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Retinues.GUI.Prefabs.ClanScreen;
using Retinues.Utilities;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

namespace Retinues.GUI.Prefabs.EscapeMenu.Patches
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
                var title = L.T("escape_menu_editor_button", "Troop Editor");
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

                        // Launch right after resuming
                        ClanScreenMixin.Launch();
                    }
                    catch (Exception e)
                    {
                        Log.Exception(e, "Failed to open Troop Editor screen.");
                    }
                }

                var item = new EscapeMenuItemVM(
                    title,
                    onExecute: OnExecute,
                    identifier: "Retinues_TroopEditorButton",
                    getIsDisabledAndReason: notDisabled,
                    isPositiveBehaviored: false
                );

                // Button index
                int insertIndex = Math.Min(3, __instance.MenuItems.Count);

                // Insert the button into the ESC menu
                __instance.MenuItems.Insert(insertIndex, item);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to inject button into EscapeMenuVM.");
            }
        }
    }
}
