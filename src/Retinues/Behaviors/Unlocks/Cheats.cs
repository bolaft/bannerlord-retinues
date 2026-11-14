using System.Collections.Generic;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Unlocks
{
    [SafeClass]
    public static class Cheats
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("reset_unlocks", "retinues")]
        public static string ResetUnlocks(List<string> args)
        {
            if (args != null && args.Count > 0)
                return "Usage: retinues.reset_unlocks";

            if (Campaign.Current == null)
                return "No campaign loaded.";

            // Use the full registry, including items hidden from the equipment editor.
            int count = WItem.ResetUnlockProgress(WItem.All);
            string result = $"Reset equipment unlock progress for {count} items. Save the game to persist it.";

            if (!Configuration.EquipmentNeedsUnlocking)
                result += " Equipment Needs Unlocking is disabled: enable it in Settings to restrict equipment access.";
            else if (DoctrineCatalog.AncestralHeritage?.IsAcquired == true)
                result += " Ancestral Heritage still grants access to your culture's equipment.";

            return result;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("unlock_item", "retinues")]
        public static string CreateRetinueCommand(List<string> args)
        {
            if (args.Count < 1)
                return "Usage: unlock_item <item_id>";

            var itemId = args[0];
            var item = WItem.Get(itemId);
            if (item == null)
                return $"Item with id '{itemId}' not found.";

            item.Unlock();

            return $"Unlocked item with id '{itemId}'.";
        }
    }
}
