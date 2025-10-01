using System;
using System.Collections.Generic;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Features.Unlocks
{
    public static class Cheats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("unlock_item", "retinues")]
        public static string UnlockItem(List<string> args)
        {
            if (args.Count != 1)
                return "Usage: retinues.unlock_item [id]";

            // Find the item
            WItem item;

            try
            {
                item = new(MBObjectManager.Instance.GetObject<ItemObject>(args[0]));
            }
            catch
            {
                return "Invalid item ID.";
            }

            // Unlock the item
            try
            {
                item.Unlock();
            }
            catch (Exception e)
            {
                return $"Failed to unlock item: {e.Message}";
            }

            return $"Unlocked item {item.Name} ({item.StringId}).";
        }
    }
}
