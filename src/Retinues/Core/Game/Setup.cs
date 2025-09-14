using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Game
{
    public static class Setup
    {
        public static void SetupFactionRetinue(WFaction faction)
        {
            string eliteName;
            string basicName;

            if (faction.StringId == Player.Kingdom?.StringId)
            {
                if (Player.IsFemale)
                    eliteName = L.T(
                        "retinue_female_kingdom", "{FACTION} Queen's Champion"
                    ).SetTextVariable("FACTION", faction.Name).ToString();
                else
                    eliteName = L.T(
                        "retinue_male_kingdom", "{FACTION} King's Champion"
                    ).SetTextVariable("FACTION", faction.Name).ToString();

                basicName = L.T(
                    "retinue_royal_guard", "{FACTION} Royal Guard"
                ).SetTextVariable("FACTION", faction.Name).ToString();
            }
            else
            {
                eliteName = L.T(
                    "retinue_house_champion", "{FACTION} House Champion"
                ).SetTextVariable("FACTION", faction.Name).ToString();
                basicName = L.T(
                    "retinue_house_guard", "{FACTION} House Guard"
                ).SetTextVariable("FACTION", faction.Name).ToString();
            }

            faction.RetinueElite = CreateRetinueTroop(
                faction,
                faction.Culture.RootElite,
                eliteName
            );

            faction.RetinueBasic = CreateRetinueTroop(
                faction,
                faction.Culture.RootBasic,
                basicName
            );
        }

        private static WCharacter CreateRetinueTroop(
            WFaction faction,
            WCharacter rootTroop,
            string retinueName
        )
        {
            // Clone it for the player retinue
            var retinueTroop = rootTroop.Clone(faction, keepUpgrades: false);

            // Rename it
            retinueTroop.Name = retinueName;

            // Non-transferable
            retinueTroop.IsNotTransferableInPartyScreen = true;

            // Unlock items
            foreach (var equipment in rootTroop.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

            return retinueTroop;
        }

        public static void SetupFactionTroops(WFaction faction)
        {
            // Clear existing troops, if any
            faction.EliteTroops.Clear();
            faction.BasicTroops.Clear();

            // Use the faction culture
            var culture = faction.Culture;

            // Clone the elite tree from the elite root
            var eliteClones = CloneTroopTreeRecursive(culture.RootElite, faction, null).ToList();
            faction.EliteTroops.AddRange(eliteClones);

            Log.Debug(
                $"Cloned {faction.EliteTroops.Count} elite troops from {culture.Name} to {faction.Name}"
            );

            // Clone the basic tree from the basic root
            var basicClones = CloneTroopTreeRecursive(culture.RootBasic, faction, null).ToList();
            faction.BasicTroops.AddRange(basicClones);

            Log.Debug(
                $"Cloned {faction.BasicTroops.Count} basic troops from {culture.Name} to {faction.Name}"
            );

            // Unlock items from the added clones
            foreach (var troop in Enumerable.Concat(faction.EliteTroops, faction.BasicTroops))
            foreach (var equipment in troop.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

            Log.Debug(
                $"Unlocked {WItem.UnlockedItems.Count()} items from {faction.EliteTroops.Count + faction.BasicTroops.Count} troops"
            );
        }

        private static IEnumerable<WCharacter> CloneTroopTreeRecursive(
            WCharacter original,
            WFaction faction,
            WCharacter parent
        )
        {
            var clone = original.Clone(faction: faction, parent: parent, keepUpgrades: false);
            clone.Name = $"{faction.Name} {original.Name}";

            yield return clone;

            if (original.UpgradeTargets != null)
                foreach (var child in original.UpgradeTargets)
                    foreach (var descendant in CloneTroopTreeRecursive(child, faction, clone))
                        yield return descendant;
        }
    }
}
