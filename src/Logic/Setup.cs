
using System.Linq;
using System.Collections.Generic;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Logic
{
    public static class Setup
    {

        public static void Initialize()
        {
            var culture = Player.Culture;
            var clan = Player.Clan;

            clan.EliteTroops.Clear();
            clan.BasicTroops.Clear();

            // Mapping from original to clone
            var map = new Dictionary<WCharacter, WCharacter>();

            // Clone the elite tree from the elite root
            foreach (var clone in CloneTroopTreeRecursive(culture.RootElite, clan, null, map))
                clan.EliteTroops.Add(clone);

            Log.Debug($"Cloned {clan.EliteTroops.Count} elite troops from {culture.Name} to {clan.Name}");

            // Clone the basic tree from the basic root
            foreach (var clone in CloneTroopTreeRecursive(culture.RootBasic, clan, null, map))
                clan.BasicTroops.Add(clone);

            Log.Debug($"Cloned {clan.BasicTroops.Count} basic troops from {culture.Name} to {clan.Name}");

            // Fix upgrade targets to point to clones
            foreach (var pair in map)
            {
                var orig = pair.Key;
                var clone = pair.Value;
                if (orig.UpgradeTargets != null)
                {
                    clone.UpgradeTargets = orig.UpgradeTargets
                        .Select(t => map.TryGetValue(t, out var c) ? c : null)
                        .Where(c => c != null)
                        .ToArray();
                }
            }

            // Unlock items from the added clones
            foreach (var troop in Enumerable.Concat(clan.EliteTroops, clan.BasicTroops))
                foreach (var equipment in troop.Equipments)
                    foreach (var item in equipment.Items)
                        item.Unlock();

            Log.Debug($"Unlocked {WItem.UnlockedItems.Count()} items from {clan.EliteTroops.Count + clan.BasicTroops.Count} troops");
        }

        private static IEnumerable<WCharacter> CloneTroopTreeRecursive(WCharacter original, WClan clan, WCharacter parent, Dictionary<WCharacter, WCharacter> map)
        {
            var clone = original.Clone(clan: clan, parent: parent);
            clone.Name = $"{clan.Name} {original.Name}";
            map[original] = clone;
            yield return clone;
            if (original.UpgradeTargets != null)
            {
                foreach (var child in original.UpgradeTargets)
                {
                    foreach (var descendant in CloneTroopTreeRecursive(child, clan, clone, map))
                        yield return descendant;
                }
            }
        }
    }
}
