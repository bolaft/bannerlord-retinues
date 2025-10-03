using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Localization;

namespace Retinues.Core.Features
{
    [SafeClass(SwallowByDefault = false)]
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

        private static void CreateRetinueTroop(WFaction faction, bool isElite, string retinueName)
        {
            var root = isElite ? faction.Culture.RootElite : faction.Culture.RootBasic;

            if (root == null)
            {
                Log.Error(
                    $"Cannot create retinue troop for faction {faction.Name} because its culture {faction.Culture.Name} has no {(isElite ? "elite" : "basic")} root troop."
                );
                return;
            }

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

            // Force recalculation of formation class based on equipment
            retinue.ResetFormationClass();

            // Activate
            retinue.Activate();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Militias                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void CreateMilitiaTroop(WFaction faction, bool isElite, bool isMelee)
        {
            WCharacter root;

            if (isMelee)
                root = isElite ? faction.Culture.MilitiaMeleeElite : faction.Culture.MilitiaMelee;
            else
                root = isElite ? faction.Culture.MilitiaRangedElite : faction.Culture.MilitiaRanged;

            if (root == null)
            {
                Log.Error(
                    $"Cannot create militia troop for faction {faction.Name} because its culture {faction.Culture.Name} has no {(isElite ? "elite" : "basic")} {(isMelee ? "melee" : "ranged")} militia troop."
                );
                return;
            }

            var militia = new WCharacter(
                faction == Player.Kingdom,
                isElite,
                false,
                isMelee,
                !isMelee
            );

            militia.FillFrom(root, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            Log.Info(
                $"Created militia troop {militia.StringId} for {faction.Name} (from {root.StringId})"
            );
            Log.Info($"troop vanilla id set to {militia.VanillaStringId}");

            // Rename it
            if (militia.Name.Contains(faction.Culture?.Name) == true)
                militia.Name = militia.Name.Replace(faction.Culture.Name, faction.Name);
            else
                militia.Name = $"{faction.Name} {root.Name}";

            // Non-transferable
            militia.IsNotTransferableInPartyScreen = true;

            // Unlock items
            foreach (var equipment in root.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

            // Force recalculation of formation class based on equipment
            militia.ResetFormationClass();

            // Activate
            militia.Activate();
        }

        public static void SetupFactionMilitia(WFaction faction)
        {
            Log.Info($"Setting up militia troops for faction {faction.Name}.");

            CreateMilitiaTroop(faction, false, true);
            CreateMilitiaTroop(faction, true, true);
            CreateMilitiaTroop(faction, false, false);
            CreateMilitiaTroop(faction, true, false);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void SetupFactionTroops(WFaction faction)
        {
            Log.Info($"Setting up troops for faction {faction.Name}.");

            // Use the faction culture
            var culture = faction.Culture;

            var eliteClones = new List<WCharacter>();

            if (culture?.RootElite != null)
            {
                // Clone the elite tree from the elite root
                eliteClones = [.. CloneTroopTreeRecursive(culture.RootElite, true, faction, null)];

                Log.Debug(
                    $"Cloned {eliteClones.Count} elite troops from {culture.Name} to {faction.Name}"
                );
            }
            else
            {
                Log.Warn(
                    $"Cannot clone elite troops for faction {faction.Name} because its culture {culture?.Name ?? "null"} has no elite root troop."
                );
            }

            var basicClones = new List<WCharacter>();

            if (culture?.RootBasic != null)
            {
                // Clone the basic tree from the basic root
                basicClones = [.. CloneTroopTreeRecursive(culture.RootBasic, false, faction, null)];

                Log.Debug(
                    $"Cloned {basicClones.Count} basic troops from {culture.Name} to {faction.Name}"
                );
            }
            else
            {
                Log.Warn(
                    $"Cannot clone basic troops for faction {faction.Name} because its culture {culture?.Name ?? "null"} has no basic root troop."
                );
            }

            // Count unlocks
            int unlocks = 0;

            // Unlock items from the added clones
            foreach (var troop in Enumerable.Concat(eliteClones, basicClones))
            foreach (var equipment in troop.Equipments)
            foreach (var item in equipment.Items)
            {
                item.Unlock();
                unlocks++;
            }

            Log.Debug(
                $"Unlocked {unlocks} items from {eliteClones.Count + basicClones.Count} troops"
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
            var troop = new WCharacter(faction == Player.Kingdom, isElite, false, path: path);

            // Copy from the original troop
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            // Rename it
            if (vanilla.Name.Contains(faction.Culture?.Name) == true)
                troop.Name = vanilla.Name.Replace(faction.Culture.Name, faction.Name);
            else
                troop.Name = $"{faction.Name} {vanilla.Name}";

            // Add to upgrade targets of the parent, if any
            parent?.AddUpgradeTarget(troop);

            // Unlock items
            foreach (var equipment in vanilla.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

            // Force recalculation of formation class based on equipment
            troop.ResetFormationClass();

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
