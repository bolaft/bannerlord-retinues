using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game;
using Retinues.Game.Helpers.Character;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Troops
{
    /// <summary>
    /// Static helpers for creating, cloning, and initializing custom troops for a faction.
    /// Handles retinues, militias, and troop trees, including item unlocks and naming.
    /// </summary>
    [SafeClass]
    public static class TroopBuilder
    {
        public static bool WarlordsBattlefield =
            ModuleChecker.GetModule("WarlordsBattlefield") != null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensures all required custom troops exist for the given faction, creating defaults if needed.
        /// Each troop family (retinue/regular/militia) is checked and initialized independently.
        /// </summary>
        public static void EnsureTroopsExist(WFaction faction)
        {
            if (faction == null)
            {
                Log.Warn("Cannot ensure troops exist for null faction.");
                return;
            }

            Log.Debug($"Ensuring troops exist for faction: {faction?.Name ?? "null"}");

            bool anyRetinueCreated = false;
            if (!faction.RetinueElite.IsActive)
            {
                Log.Info("Missing elite retinue. Initializing.");
                CreateRetinueTroop(faction, isElite: true, MakeRetinueName(faction, isElite: true));
                anyRetinueCreated = true;
            }
            if (!faction.RetinueBasic.IsActive)
            {
                Log.Info("Missing basic retinue. Initializing.");
                CreateRetinueTroop(
                    faction,
                    isElite: false,
                    MakeRetinueName(faction, isElite: false)
                );
                anyRetinueCreated = true;
            }
            if (!anyRetinueCreated)
                Log.Debug("Retinue troops found, no need to initialize.");

            var hasBasic = faction.BasicTroops.Count > 0;
            var hasElite = faction.EliteTroops.Count > 0;

            if (!hasBasic || !hasElite)
            {
                Log.Debug($"Custom troop presence: Basic:{hasBasic} Elite:{hasElite}.");

                bool canInitClanTroops =
                    faction.HasFiefs || Player.Kingdom != null || Config.RecruitAnywhere == true;

                if (canInitClanTroops)
                {
                    InformationManager.ShowInquiry(
                        new InquiryData(
                            titleText: L.S("custom_troops_inquiry_title", "Custom Troops Unlocked"),
                            text: L.T(
                                    "custom_troops_inquiry_body",
                                    "Your {FACTION}'s custom troops are now unlocked.\n\nWould you like to clone the entire {CULTURE} troop tree, or would you prefer building them from scratch?\n\nCopying your culture's troops will provide you with good gear and good troops. Starting from scratch is the more difficult choice.\n\nThis decision is irreversible."
                                )
                                .SetTextVariable(
                                    "FACTION",
                                    faction.IsPlayerClan ? "clan" : "kingdom"
                                )
                                .SetTextVariable("CULTURE", faction.Culture?.Name ?? "culture")
                                .ToString(),
                            isAffirmativeOptionShown: true,
                            isNegativeOptionShown: true,
                            affirmativeText: L.T("create_from_culture", "Copy {CULTURE}'s Troops")
                                .SetTextVariable("CULTURE", faction.Culture?.Name ?? "Culture")
                                .ToString(),
                            negativeText: L.S("create_from_scratch", "Start from Scratch"),
                            affirmativeAction: () =>
                            {
                                // Continue with copyWholeTree = true
                                if (!hasBasic)
                                    BuildBasicTree(faction, true);
                                if (!hasElite)
                                    BuildEliteTree(faction, true);
                            },
                            negativeAction: () =>
                            {
                                // Continue with copyWholeTree = false
                                if (!hasBasic)
                                    BuildBasicTree(faction, false);
                                if (!hasElite)
                                    BuildEliteTree(faction, false);
                            }
                        )
                    );
                }
                else
                {
                    Log.Debug("Rules do not allow initializing clan troops right now.");
                }
            }
            else
            {
                Log.Debug("Custom troops found for faction, no need to initialize.");
            }

            bool needsMilitia =
                !faction.MilitiaMelee.IsActive
                || !faction.MilitiaMeleeElite.IsActive
                || !faction.MilitiaRanged.IsActive
                || !faction.MilitiaRangedElite.IsActive;

            if (needsMilitia)
            {
                if (DoctrineAPI.IsDoctrineUnlocked<CulturalPride>())
                {
                    Log.Info("Initializing missing militia troops.");

                    bool anyMilitiaBuilt = false;

                    if (!faction.MilitiaMelee.IsActive)
                    {
                        CreateMilitiaTroop(faction, isElite: false, isMelee: true);
                        anyMilitiaBuilt = true;
                    }
                    if (!faction.MilitiaMeleeElite.IsActive)
                    {
                        CreateMilitiaTroop(faction, isElite: true, isMelee: true);
                        anyMilitiaBuilt = true;
                    }
                    if (!faction.MilitiaRanged.IsActive)
                    {
                        CreateMilitiaTroop(faction, isElite: false, isMelee: false);
                        anyMilitiaBuilt = true;
                    }
                    if (!faction.MilitiaRangedElite.IsActive)
                    {
                        CreateMilitiaTroop(faction, isElite: true, isMelee: false);
                        anyMilitiaBuilt = true;
                    }

                    // Update all existing militias for this faction
                    if (anyMilitiaBuilt)
                    {
                        foreach (var s in faction.Settlements)
                            s.MilitiaParty?.MemberRoster?.SwapTroops(faction);
                    }
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
        /// Builds both retinue troops for the given faction (kept for batch scenarios).
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

            if (root == null)
            {
                Log.Error(
                    $"Cannot create retinue troop for faction {faction.Name} because its culture {faction.Culture.Name} has no {(isElite ? "elite" : "basic")} root troop."
                );
                return;
            }

            var tpl = FindTemplate(root, tierBonus: faction == Player.Kingdom ? 2 : 0);

            Log.Info($"Creating retinue troop from template {tpl.Name} ({tpl})");
            Log.Info($"Tier: {tpl.Tier}");

            var retinue = new WCharacter(faction == Player.Kingdom, isElite, true);
            var originalRace = retinue.Race;
            var originalMin = retinue.Base.GetBodyPropertiesMin();
            var originalMax = retinue.Base.GetBodyPropertiesMax();

            retinue.FillFrom(tpl, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            if (retinue.Race != originalRace)
            {
                retinue.Race = originalRace;
                retinue.EnsureOwnBodyRange();

                var range = Reflector.GetPropertyValue<object>(retinue.Base, "BodyPropertyRange");
                if (range != null)
                {
                    Reflector.InvokeMethod(
                        range,
                        "Init",
                        new[] { typeof(BodyProperties), typeof(BodyProperties) },
                        originalMin,
                        originalMax
                    );
                }

                retinue.Age = (originalMin.Age + originalMax.Age) * 0.5f;
            }

            Log.Info($"Created retinue troop {retinue.Name} for {faction.Name} (from {tpl})");

            // Rename it
            retinue.Name = retinueName;

            // Non-transferable
            retinue.IsNotTransferableInPartyScreen = true;

            // Unlock items
            foreach (var equipment in tpl.Loadout.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();

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
        /// Build the elite tree for a faction.
        /// </summary>
        public static void BuildEliteTree(WFaction faction, bool copyWholeTree)
        {
            var culture = faction.Culture;
            if (culture?.RootElite == null)
            {
                Log.Warn($"Cannot clone elite troops for {faction.Name}: no elite culture root.");
                return;
            }

            var eliteClones = CloneTroopTreeRecursive(
                    culture.RootElite,
                    true,
                    faction,
                    null,
                    copyWholeTree
                )
                .Where(t => t != null)
                .ToList();
            Log.Info($"Cloned {eliteClones.Count} elite troops for {faction.Name}.");
            UnlockAll(eliteClones, null);
        }

        /// <summary>
        /// Build the basic tree for a faction.
        /// </summary>
        public static void BuildBasicTree(WFaction faction, bool copyWholeTree)
        {
            var culture = faction.Culture;
            if (culture?.RootBasic == null)
            {
                Log.Warn($"Cannot clone basic troops for {faction.Name}: no basic culture root.");
                return;
            }

            var basicClones = CloneTroopTreeRecursive(
                    culture.RootBasic,
                    false,
                    faction,
                    null,
                    copyWholeTree
                )
                .Where(t => t != null)
                .ToList();
            Log.Info($"Cloned {basicClones.Count} basic troops for {faction.Name}.");
            UnlockAll(null, basicClones);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         HELPERS                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void UnlockAll(
            IEnumerable<WCharacter> eliteClones,
            IEnumerable<WCharacter> basicClones
        )
        {
            int unlocks = 0;
            foreach (var troop in Enumerable.Concat(eliteClones ?? [], basicClones ?? []))
            {
                try
                {
                    foreach (var equipment in troop.Loadout.Equipments)
                    foreach (var item in equipment.Items)
                    {
                        item.Unlock();
                        unlocks++;
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"Error processing troop {troop?.Name ?? "null"}");
                }
            }
            Log.Debug(
                $"Unlocked {unlocks} items from {(eliteClones?.Count() ?? 0) + (basicClones?.Count() ?? 0)} troops"
            );
        }

        /// <summary>
        /// Recursively clones a vanilla troop tree for a faction, setting up upgrades and unlocking items.
        /// </summary>
        private static IEnumerable<WCharacter> CloneTroopTreeRecursive(
            WCharacter vanilla,
            bool isElite,
            WFaction faction,
            WCharacter parent,
            bool copyWholeTree
        )
        {
            var tpl = FindTemplate(vanilla);

            if (tpl == null)
            {
                Log.Error(
                    $"Cannot clone troop {vanilla.Name} because no template was found for {(isElite ? "elite" : "basic")} troops."
                );
                yield break;
            }

            // Determine the position in the tree
            List<int> path = null;
            if (parent != null)
            {
                path = parent.PositionInTree != null ? [.. parent.PositionInTree] : [];
                path.Add(parent.UpgradeTargets.Length);
            }

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
                yield break; // Should not happen, but other mods might interfere
            else
            {
                // Wrap the custom troop
                var troop = new WCharacter(co);

                // Copy from the original troop
                troop.FillFrom(tpl, keepUpgrades: false, keepEquipment: true, keepSkills: true);

                // Rename it
                troop.Name = BuildTroopName(tpl, faction);

                // Add to upgrade targets of the parent, if any
                parent?.AddUpgradeTarget(troop);

                // Unlock items (vanilla source items)
                foreach (var equipment in vanilla.Loadout.Equipments)
                foreach (var item in equipment.Items)
                    item.Unlock();

                // Activate
                troop.Activate();

                yield return troop;

                if (tpl.UpgradeTargets != null && copyWholeTree)
                {
                    foreach (var child in vanilla.UpgradeTargets)
                    foreach (
                        var descendant in CloneTroopTreeRecursive(
                            child,
                            isElite,
                            faction,
                            troop,
                            copyWholeTree
                        )
                    )
                    {
                        if (descendant == null)
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
                    // If the word is mostly the culture name (e.g. 'Battanian' contains 'Battania'), replace the whole word
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

        /// <summary>
        /// Finds a template troop in the tree matching elite/basic and tier.
        /// </summary>
        private static WCharacter FindTemplate(WCharacter root, int tierBonus = 0)
        {
            if (root == null)
                return null;

            // Find the best matching tier
            WCharacter bestTroop = root;
            var troopTier = root.Tier;
            var targetTier = troopTier + tierBonus;

            foreach (var troop in root.Tree)
            {
                var tier = troop.Tier;
                if (tier > troopTier && tier <= targetTier)
                {
                    troopTier = tier;
                    bestTroop = troop;

                    if (troopTier == targetTier)
                        break;
                }
            }

            return bestTroop;
        }
    }
}
