using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Unlocks
{
    /// <summary>
    /// The cheat inventory starts with free items which Bannerlord includes in its discard
    /// event. Only positive additions to that catalogue count towards Retinues unlocks.
    /// </summary>
    internal static class CheatDiscardFilter
    {
        // Per-screen, runtime-only state. Weak keys let closed inventories be collected;
        // values must not retain the roster itself. Never serialize or edit earned unlocks.
        private static readonly ConditionalWeakTable<ItemRoster, Dictionary<EquipmentElement, int>>
            InitialCounts = new();

        internal static void Capture(ItemRoster roster, bool isCheatInventory)
        {
            if (roster == null)
                return;

            InitialCounts.Remove(roster);
            if (!isCheatInventory)
                return;

            var counts = new Dictionary<EquipmentElement, int>();
            foreach (var entry in roster)
                if (entry.EquipmentElement.Item != null && entry.Amount > 0)
                    counts[entry.EquipmentElement] = entry.Amount;
            InitialCounts.Add(roster, counts);
        }

        internal static ItemRoster ForUnlocks(ItemRoster roster)
        {
            if (roster == null || !InitialCounts.TryGetValue(roster, out var initial))
                return roster; // Ordinary discards and unclaimed battle loot are unchanged.

            var discarded = new ItemRoster();
            foreach (var entry in roster)
            {
                if (entry.EquipmentElement.Item == null || entry.Amount <= 0)
                    continue;
                initial.TryGetValue(entry.EquipmentElement, out int supplied);
                int added = entry.Amount - supplied;
                if (added > 0)
                    discarded.AddToCounts(entry.EquipmentElement, added);
            }

            // Keep the snapshot: a repeated event must never expose the full catalogue.
            // Do not modify the event roster, which the game and other mods also consume.
            Log.Debug($"[Unlocks] Cheat inventory filtered: catalogueStacks={initial.Count}, discardedStacks={discarded.Count}.");
            return discarded;
        }
    }
}
