
using System.Linq;
using System.Collections.Generic;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Game
{
    public static class Setup
    {
        public static void SetupFaction(WFaction faction)
        {
            // Clear existing troops, if any
            faction.EliteTroops.Clear();
            faction.BasicTroops.Clear();

            // Use the faction culture
            var culture = faction.Culture;

            // Mapping from original to clone
            var map = new Dictionary<WCharacter, WCharacter>();

            // Clone the elite tree from the elite root
            var eliteClones = CloneTroopTreeRecursive(culture.RootElite, faction, null, map).ToList();
            faction.EliteTroops.AddRange(eliteClones);

            Log.Debug($"Cloned {faction.EliteTroops.Count} elite troops from {culture.Name} to {faction.Name}");

            // Clone the basic tree from the basic root
            var basicClones = CloneTroopTreeRecursive(culture.RootBasic, faction, null, map).ToList();
            faction.BasicTroops.AddRange(basicClones);

            Log.Debug($"Cloned {faction.BasicTroops.Count} basic troops from {culture.Name} to {faction.Name}");

            // Fix upgrade targets to point to clones
            foreach (var pair in map)
            {
                var orig = pair.Key;
                var clone = pair.Value;
                if (orig.UpgradeTargets != null)
                {
                    clone.UpgradeTargets = [.. orig.UpgradeTargets
                        .Select(t => map.TryGetValue(t, out var c) ? c : null)
                        .Where(c => c != null)];
                }
            }

            // Unlock items from the added clones
            foreach (var troop in Enumerable.Concat(faction.EliteTroops, faction.BasicTroops))
                foreach (var equipment in troop.Equipments)
                    foreach (var item in equipment.Items)
                        item.Unlock();

            Log.Debug($"Unlocked {WItem.UnlockedItems.Count()} items from {faction.EliteTroops.Count + faction.BasicTroops.Count} troops");
        }

        private static IEnumerable<WCharacter> CloneTroopTreeRecursive(WCharacter original, WFaction faction, WCharacter parent, Dictionary<WCharacter, WCharacter> map)
        {
            var clone = original.Clone(faction: faction, parent: parent);
            clone.Name = $"{faction.Name} {original.Name}";
            map[original] = clone;
            yield return clone;
            if (original.UpgradeTargets != null)
            {
                foreach (var child in original.UpgradeTargets)
                {
                    foreach (var descendant in CloneTroopTreeRecursive(child, faction, clone, map))
                        yield return descendant;
                }
            }
        }
    }
}
