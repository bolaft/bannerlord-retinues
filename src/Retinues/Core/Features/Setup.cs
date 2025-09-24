using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Localization;

namespace Retinues.Core.Features
{
    public static class Setup
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void SetupFactionRetinue(WFaction faction)
        {
            Log.Info($"Setting up retinue troops for faction {faction.Name}.");

            CreateRetinueTroop(faction, true, MakeRetinueName(faction, isElite: true));

            CreateRetinueTroop(faction, false, MakeRetinueName(faction, isElite: false));
        }

        /* ━━━━━━━━ Helpers ━━━━━━━ */

        private static string MakeRetinueName(WFaction faction, bool isElite)
        {
            TextObject to;

            if (faction == Player.Kingdom)
                if (isElite)
                    if (Player.IsFemale)
                        to = L.T("retinue_female_kingdom", "{FACTION} Queen's Champion");
                    else
                        to = L.T("retinue_male_kingdom", "{FACTION} King's Champion");
                else
                    to = L.T("retinue_royal_guard", "{FACTION} Royal Guard");
            else if (isElite)
                to = L.T("retinue_house_champion", "{FACTION} House Champion");
            else
                to = L.T("retinue_house_guard", "{FACTION} House Guard");

            return to.SetTextVariable("FACTION", faction.Name).ToString();
        }

        private static WCharacter CreateRetinueTroop(
            WFaction faction,
            bool isElite,
            string retinueName
        )
        {
            var root = isElite ? faction.Culture.RootElite : faction.Culture.RootBasic;
            var retinue = new WCharacter(faction == Player.Kingdom, isElite, true);

            retinue.FillFrom(root, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            Log.Info(
                $"Created retinue troop {retinue.StringId} for {faction.Name} (from {root.StringId})"
            );
            Log.Info($"troop vanilla id set to {retinue.VanillaStringId}");

            // Rename it
            retinue.Name = retinueName;

            // Non-transferable
            retinue.IsNotTransferableInPartyScreen = true;

            // Unlock items
            foreach (var equipment in root.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

            // Activate
            retinue.Activate();

            return retinue;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void SetupFactionTroops(WFaction faction)
        {
            Log.Info($"Setting up troops for faction {faction.Name}.");

            // Use the faction culture
            var culture = faction.Culture;

            // Clone the elite tree from the elite root
            var eliteClones = CloneTroopTreeRecursive(
                    faction.Culture.RootElite,
                    true,
                    faction,
                    null
                )
                .ToList();

            Log.Debug(
                $"Cloned {eliteClones.Count} elite troops from {culture.Name} to {faction.Name}"
            );

            // Clone the basic tree from the basic root
            var basicClones = CloneTroopTreeRecursive(
                    faction.Culture.RootBasic,
                    false,
                    faction,
                    null
                )
                .ToList();

            Log.Debug(
                $"Cloned {basicClones.Count} basic troops from {culture.Name} to {faction.Name}"
            );

            // Unlock items from the added clones
            foreach (var troop in Enumerable.Concat(eliteClones, basicClones))
            foreach (var equipment in troop.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

            Log.Debug(
                $"Unlocked {WItem.UnlockedItems.Count()} items from {eliteClones.Count + basicClones.Count} troops"
            );
        }

        /* ━━━━━━━━ Helpers ━━━━━━━ */

        private static IEnumerable<WCharacter> CloneTroopTreeRecursive(
            WCharacter vanilla,
            bool isElite,
            WFaction faction,
            WCharacter parent
        )
        {
            // Determine the position in the tree
            List<int> path = null;
            if (parent != null)
                path = [.. parent.PositionInTree, parent.UpgradeTargets.Length];

            // Wrap the custom troop
            var troop = new WCharacter(faction == Player.Kingdom, isElite, false, path);

            // Copy from the original troop
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            // Rename it
            troop.Name = $"{faction.Name} {vanilla.Name}";

            // Add to upgrade targets of the parent, if any
            parent?.AddUpgradeTarget(troop);

            // Unlock items
            foreach (var equipment in vanilla.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

            // Activate
            troop.Activate();

            yield return troop;

            if (vanilla.UpgradeTargets != null)
                foreach (var child in vanilla.UpgradeTargets)
                foreach (var descendant in CloneTroopTreeRecursive(child, isElite, faction, troop))
                    yield return descendant;
        }
    }
}
