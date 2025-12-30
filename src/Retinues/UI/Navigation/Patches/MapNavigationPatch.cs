using System.Linq;
using HarmonyLib;
using Retinues.Utilities;
using SandBox.View.Map.Navigation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;

namespace Retinues.UI.Navigation.Patches;

[HarmonyPatch(typeof(MapNavigationHandler))]
internal static class MapNavigationHandlerCtorPatch
{
    private const string RetinuesId = "troops";

    [HarmonyPatch(typeof(MapNavigationItemVM))]
    internal static class MapNavigationItemVMItemIdPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(INavigationElement)])]
        private static void Postfix(
            MapNavigationItemVM __instance,
            INavigationElement navigationElement
        )
        {
            if (navigationElement?.StringId == RetinuesId)
            {
                // Reuse a known existing MapBar icon/background key.
                __instance.ItemId = "party"; // or "clan", "inventory", etc.
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor)]
    private static void Postfix(MapNavigationHandler __instance)
    {
        try
        {
            Inject(__instance);
        }
        catch (System.Exception e)
        {
            Log.Exception(e, "Failed to inject MapBar navigation element");
        }
    }

    private static void Inject(MapNavigationHandler handler)
    {
        var elements = handler.GetElements()?.ToList() ?? [];

        if (elements.Any(e => e?.StringId == RetinuesId))
            return;

        var idx = 2; // after "character"
        elements.Insert(idx, new RetinuesNavigationElement(handler));

        // IMPORTANT: update the private backing field so the handler "owns" it.
        Reflection.SetFieldValue(handler, "_elements", elements.ToArray());
    }
}
