using System.Collections.Generic;
using System.Linq;
using Helpers;
using Retinues.Features.Recruits.Behaviors;
using Retinues.Game;
using Retinues.Game.Helpers.Character;
using Retinues.Game.Wrappers;
using Retinues.Mods.WarlordsBattlefield;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Troops
{
    /// <summary>
    /// Static helpers for creating, cloning, and initializing custom troops for a faction.
    /// Handles retinues, militias, and troop trees, including item unlocks and naming.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
    public static class TroopBuilder
    {
        public static bool WarlordsBattlefield =
            ModuleChecker.GetModule("WarlordsBattlefield") != null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensures all required custom troops exist for the given faction, creating defaults if needed.
        /// </summary>
        public static void EnsureTroopsExist(WFaction faction)
        {
            if (faction == null)
            {
                Log.Warn("Cannot ensure troops exist for null faction.");
                return;
            }

            Log.Debug($"Ensuring troops exist for faction: {faction?.Name ?? "null"}");

            if (!faction.RetinueElite.IsActive || !faction.RetinueBasic.IsActive)
            {
                Log.Info("No retinue troops found, initializing default retinue troops.");
                BuildRetinue(faction);
            }
            else
            {
                Log.Debug("Retinue troops found, no need to initialize.");
            }

            if (faction.BasicTroops.Count == 0 && faction.EliteTroops.Count == 0)
            {
                Log.Debug("No custom troops found for faction.");

                // Always have clan troops if clan has fiefs, if player leads a kingdom or if can recruit anywhere is enabled
                if (faction.HasFiefs || Player.Kingdom != null
                // || Config.GetOption<bool>("RecruitAnywhere")
                )
                {
                    Log.Info("Initializing default troops.");

                    BuildTroops(faction);
                }
            }
            else
            {
                Log.Debug("Custom troops found for faction, no need to initialize.");
            }

            if (!faction.MilitiaMelee.IsActive || !faction.MilitiaRanged.IsActive)
            {
                // Always have militia troops if clan has fiefs or if player leads a kingdom
                if (faction.HasFiefs || Player.Kingdom != null)
                {
                    Log.Info("Initializing militia troops.");

                    BuildMilitia(faction);

                    // Update all existing militias for this faction
                    foreach (var s in faction.Settlements)
                        s.MilitiaParty?.MemberRoster?.SwapTroops(faction);
                }
            }
            else
            {
                Log.Debug("Militia troops found, no need to initialize.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Builds retinue troops for the given faction.
        /// </summary>
        public static void BuildRetinue(WFaction faction)
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

            // Custom fix for Warlords Battlefield
            if (WarlordsBattlefield)
                root = WarlordsBattlefieldRootFixer.FixRoot(root, isElite);

            if (root == null)
            {
                Log.Error(
                    $"Cannot create retinue troop for faction {faction.Name} because its culture {faction.Culture.Name} has no {(isElite ? "elite" : "basic")} root troop."
                );
                return;
            }

            Log.Info($"Creating retinue troop from root {root.Name} ({root})");
            Log.Info($"Tier: {root.Tier}");

            var retinue = new WCharacter(faction == Player.Kingdom, isElite, true);

            retinue.FillFrom(root, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            Log.Info($"Created retinue troop {retinue.Name} for {faction.Name} (from {root})");

            // Rename it
            retinue.Name = retinueName;

            // Non-transferable
            retinue.IsNotTransferableInPartyScreen = true;

            // Unlock items
            foreach (var equipment in root.Loadout.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

            // Activate
            retinue.Activate();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Militias                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Builds militia troops for the given faction.
        /// </summary>
        public static void BuildMilitia(WFaction faction)
        {
            Log.Info($"Setting up militia troops for faction {faction.Name}.");

            CreateMilitiaTroop(faction, false, true);
            CreateMilitiaTroop(faction, true, true);
            CreateMilitiaTroop(faction, false, false);
            CreateMilitiaTroop(faction, true, false);
        }

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

            Log.Info($"Created militia troop {militia.Name} for {faction.Name} (from {root})");
            Log.Info($"troop vanilla id set to {militia.VanillaStringId}");

            // Rename it
            militia.Name = BuildTroopName(root, faction);

            // Rank up so skill caps/totals match vanilla militia
            militia.Level += 5;

            // Non-transferable
            militia.IsNotTransferableInPartyScreen = true;

            // Unlock items
            foreach (var equipment in root.Loadout.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

            // Activate
            militia.Activate();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Builds regular troop trees for the given faction, cloning from culture roots.
        /// </summary>
        public static void BuildTroops(WFaction faction)
        {
            Log.Info($"Setting up troops for faction {faction.Name}.");

            // Use the faction culture
            var culture = faction.Culture;
            Log.Debug($"Faction culture: {culture?.Name ?? "null"}");

            var eliteClones = new List<WCharacter>();
            Log.Debug($"Initial eliteClones count: {eliteClones.Count}");

            if (culture?.RootElite != null)
            {
                Log.Debug($"Cloning elite tree from root: {culture.RootElite.Name}");
                eliteClones = [.. CloneTroopTreeRecursive(culture.RootElite, true, faction, null)];
                Log.Debug($"eliteClones after clone: {eliteClones.Count}");
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
            Log.Debug($"Initial basicClones count: {basicClones.Count}");

            if (culture?.RootBasic != null)
            {
                Log.Debug($"Cloning basic tree from root: {culture.RootBasic.Name}");
                basicClones = [.. CloneTroopTreeRecursive(culture.RootBasic, false, faction, null)];
                Log.Debug($"basicClones after clone: {basicClones.Count}");
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
            Log.Debug($"Starting unlocks count: {unlocks}");

            // Unlock items from the added clones
            foreach (var troop in Enumerable.Concat(eliteClones, basicClones))
            {
                Log.Debug($"Processing troop: {troop?.Name ?? "null"}");
                foreach (var equipment in troop.Loadout.Equipments)
                {
                    Log.Debug(
                        $"Processing equipment: {equipment?.ToString() ?? "null"}, Items count: {equipment.Items.Count}"
                    );
                    foreach (var item in equipment.Items)
                    {
                        Log.Debug($"Unlocking item: {item?.ToString() ?? "null"}");
                        item.Unlock();
                        unlocks++;
                        Log.Debug($"Unlocks incremented: {unlocks}");
                    }
                }
            }

            Log.Debug(
                $"Unlocked {unlocks} items from {eliteClones.Count + basicClones.Count} troops"
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         HELPERS                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Recursively clones a vanilla troop tree for a faction, setting up upgrades and unlocking items.
        /// </summary>
        private static IEnumerable<WCharacter> CloneTroopTreeRecursive(
            WCharacter vanilla,
            bool isElite,
            WFaction faction,
            WCharacter parent
        )
        {
            // Custom fix for Warlords Battlefield
            if (parent == null && WarlordsBattlefield)
                vanilla = WarlordsBattlefieldRootFixer.FixRoot(vanilla, isElite);

            // Determine the position in the tree
            List<int> path = null;
            if (parent != null)
                path = [.. parent.PositionInTree, parent.UpgradeTargets.Length];

            // CharacterObject
            var co = new CustomCharacterHelper().GetCharacterObject(
                faction == Player.Kingdom,
                isElite,
                false,
                false,
                false,
                path: path
            );

            if (co == null)
                yield return null; // Should not happen, but other mods might interfere
            else
            {
                // Wrap the custom troop
                var troop = new WCharacter(co);

                // Copy from the original troop
                troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: true, keepSkills: true);

                // Rename it
                troop.Name = BuildTroopName(vanilla, faction);

                // Add to upgrade targets of the parent, if any
                parent?.AddUpgradeTarget(troop);

                // Unlock items
                foreach (var equipment in vanilla.Loadout.Equipments)
                foreach (var item in equipment.Items)
                    item.Unlock();

                // Activate
                troop.Activate();

                yield return troop;

                if (vanilla.UpgradeTargets != null)
                {
                    foreach (var child in vanilla.UpgradeTargets)
                    foreach (
                        var descendant in CloneTroopTreeRecursive(child, isElite, faction, troop)
                    )
                    {
                        if (descendant != null)
                            continue;
                        yield return descendant;
                    }
                }
            }
        }

        /// <summary>
        /// Builds a troop name for a custom troop, replacing culture name with faction name if possible.
        /// </summary>
        private static string BuildTroopName(WCharacter vanilla, WFaction faction)
        {
            var cultureName = faction.Culture?.Name;
            if (
                string.IsNullOrEmpty(cultureName)
                || string.IsNullOrEmpty(faction.Name)
                || string.IsNullOrEmpty(vanilla.Name)
            )
                return vanilla.Name;

            // Try to find a word containing the culture name
            var words = vanilla.Name.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (word.Contains(cultureName))
                {
                    // If the word is mostly the culture name (e.g., 'Battanian' contains 'Battania'), replace the whole word
                    int overlap = cultureName.Length * 100 / word.Length;
                    if (overlap >= 80) // 80% or more of the word is the culture name
                    {
                        words[i] = faction.Name;
                        return string.Join(" ", words);
                    }
                }
            }
            // Fallback: prepend faction name
            return $"{faction.Name} {vanilla.Name}";
        }
    }
}
