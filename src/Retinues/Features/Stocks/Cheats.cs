using System.Collections.Generic;
using Retinues.Features.Stocks.Behaviors;
using TaleWorlds.Library;

namespace Retinues.Features.Stocks
{
    /// <summary>
    /// Console cheats for item stocks. Allows setting stock counts via command.
    /// </summary>
    public static class Cheats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Sets the stock count for an item by ID. Usage: retinues.set_stock [id] [count]
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("set_stock", "retinues")]
        public static string SetStock(List<string> args)
        {
            if (args.Count != 2)
                return "Usage: retinues.set_stock [id] [count]";

            var itemId = args[0];
            if (!int.TryParse(args[1], out var count))
                return "Invalid count.";

            StocksBehavior.Set(itemId, count);
            return $"Set stock for {itemId} to {count}.";
        }
    }
}
