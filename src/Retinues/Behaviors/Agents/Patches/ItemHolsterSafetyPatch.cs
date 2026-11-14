using HarmonyLib;
using Retinues.Domain.Equipments.Helpers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Behaviors.Agents.Patches
{
    // Covers existing gear on both armies and heroes as well as custom troops. Do not mutate
    // ItemObject or remove equipment: other mods and saved loadouts share these definitions.
    [HarmonyPatch(typeof(ItemCollectionElementMissionExtensions), "GetItemHolsterIndices")]
    internal static class ItemHolsterSafetyPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ItemObject item, ref StackArray.StackArray4Int __result)
        {
            if (!ItemHolsterSafety.TryGetFallbackIndices(item, MBItem.GetItemHolsterIndex, out var indices))
                return true;
            __result = indices;
            return false;
        }
    }
}
