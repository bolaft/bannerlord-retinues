using System;
using System.Collections.Generic;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.Library;

namespace Retinues.Core.Features.Xp
{
    public static class Cheats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("troop_xp_add", "retinues")]
        public static string TroopXpAdd(List<string> args)
        {
            if (args.Count == 0 || args.Count > 2)
                return "Usage: retinues.troop_xp_add [id] [amount]";

            // Find the troop
            WCharacter troop;

            try
            {
                troop = new(args[0]);
            }
            catch
            {
                return "Invalid troop ID.";
            }

            // Parse the amount
            int amount;

            if (args.Count == 1)
            {
                amount = 1000;
            }
            else
            {
                try
                {
                    amount = int.Parse(args[1]);
                }
                catch
                {
                    return "Amount must be an integer.";
                }
            }

            // Add the XP
            try
            {
                TroopXpBehavior.Add(troop, amount);
            }
            catch (Exception e)
            {
                return $"Failed to add XP: {e.Message}";
            }

            return $"Added {amount} XP to {troop.Name} ({troop.StringId}).";
        }
    }
}
