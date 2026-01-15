using System.Collections.Generic;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Runtime;
using TaleWorlds.Library;

namespace Retinues.Game.Unlocks
{
    [SafeClass]
    public static class UnlockCheats
    {
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
