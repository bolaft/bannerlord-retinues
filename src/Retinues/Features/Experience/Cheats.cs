using System;
using System.Collections.Generic;
using Retinues.Game;
using Retinues.Game.Wrappers;
using TaleWorlds.Library;

namespace Retinues.Features.Experience
{
    /// <summary>
    /// Console cheats for troop XP. Allows adding XP to custom troops via command.
    /// </summary>
    public static class Cheats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds XP to a troop by ID. Usage: retinues.troop_xp_add [id] [amount]
        /// </summary>
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
                BattleXpBehavior.Add(troop, amount);
            }
            catch (Exception e)
            {
                return $"Failed to add XP: {e.Message}";
            }

            return $"Added {amount} XP to {troop.Name} ({troop}).";
        }

        /// <summary>
        /// Lists the IDs of all IsActive custom troops. Usage: retinues.list_custom_troops
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("list_custom_troops", "retinues")]
        public static string ListCustomTroops(List<string> args)
        {
            var result = new List<string>();
            foreach (var troop in Player.Troops)
            {
                if (troop.IsActive && troop.IsCustom)
                {
                    var name = troop.Name;
                    var id = troop.StringId;
                    var xp = BattleXpBehavior.Get(troop);
                    result.Add($"{id}: {name} (XP: {xp})");
                }
            }
            if (result.Count == 0)
                return "No active custom troops found.";
            return string.Join("\n", result);
        }
    }
}
