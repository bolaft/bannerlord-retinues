using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
#if !BL12
using InventoryMode = Helpers.InventoryScreenHelper.InventoryMode;
#endif

namespace Retinues.Behaviors.Unlocks.Patches
{
    [HarmonyPatch]
    internal static class CheatInventoryDiscardPatch
    {
        // Patch only the common initializer, not the MobileParty overload that calls it.
        private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(InventoryLogic))
            .Single(m => m.Name == "Initialize" && m.GetParameters()[1].ParameterType == typeof(ItemRoster));

        [HarmonyPrefix]
        private static void Prefix(InventoryLogic __instance, ItemRoster leftItemRoster, bool isTrading
#if !BL12
            , InventoryMode inventoryMode
#endif
        )
        {
            // Capture at opening, even if unlocking is currently disabled. Settings and
            // cheat mode may change before closing. Never treat loot/trade/stash as cheats.
            bool isCheatInventory = Game.Current?.CheatMode == true && !isTrading
                && __instance.OtherParty == null
#if BL12
                && InventoryManager.Instance?.CurrentMode == InventoryMode.Default;
#else
                && inventoryMode == InventoryMode.Default;
#endif
            CheatDiscardFilter.Capture(leftItemRoster, isCheatInventory);
        }
    }
}
