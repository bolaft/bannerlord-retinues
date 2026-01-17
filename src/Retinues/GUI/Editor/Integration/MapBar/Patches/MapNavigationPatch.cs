using System.Linq;
using HarmonyLib;
using Retinues.Utilities;
using SandBox.View.Map.Navigation;

namespace Retinues.GUI.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Injects the troop editor navigation element into the map bar.
    /// </summary>
    [HarmonyPatch(typeof(MapNavigationHandler))]
    internal static class MapNavigationHandlerCtorPatch
    {
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

            if (elements.Any(e => e?.StringId == TroopsNavigationElement.TroopsId))
                return;

            var idx = 2; // after "character"
            elements.Insert(idx, new TroopsNavigationElement(handler));

            // IMPORTANT: update the private backing field so the handler "owns" it.
            Reflection.SetFieldValue(handler, "_elements", elements.ToArray());
        }
    }
}
