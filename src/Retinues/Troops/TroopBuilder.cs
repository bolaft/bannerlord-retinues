using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game;
using Retinues.Game.Helpers.Character;
using Retinues.Game.Wrappers;
using Retinues.Utils;
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
        private static bool troopsAwaitingRegistration = false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensures all required custom troops exist for the given faction.
        /// Registers new troops if any were created.
        /// </summary>
        public static void EnsureTroopsExist(WFaction faction)
        {
            if (faction == null)
                return; // Cannot ensure troops exist for null (no kingdom?) faction.

            UpdateFactionTroops(faction);
            RegisterNewTroops(faction);
        }

        /// <summary>
        /// Registers any newly created troops into the CharacterGraphIndex.
        /// </summary>
        public static void RegisterNewTroops(WFaction faction)
        {
            if (faction == null)
                return; // Cannot ensure troops exist for null (no kingdom?) faction.

            if (!troopsAwaitingRegistration)
                return; // No new troops to register

            CharacterGraphIndex.RegisterFactionRoots(faction);
            troopsAwaitingRegistration = false; // Reset the flag after registration

            // Update all existing parties for this faction
            WParty.SwapAll(members: true, prisoners: true, skipGarrisons: true);
        }

        /// <summary>
        /// Ensures all required custom troops exist for the given faction, creating defaults if needed.
        /// </summary>
        public static void UpdateFactionTroops(WFaction faction)
        {
            Log.Debug($"Ensuring troops exist for faction: {faction?.Name ?? "null"}");

            if (faction.RetinueElite is null)
                CreateRetinueTroop(faction, isElite: true);
            else
                Log.Debug("Elite retinue found, no need to initialize.");

            if (faction.RetinueBasic is null)
                CreateRetinueTroop(faction, isElite: false);
            else
                Log.Debug("Basic retinue found, no need to initialize.");

            if (faction == Player.Clan)
                if (!faction.HasFiefs && !Config.RecruitAnywhere == true)
                    return; // Can't initialize troops for player clan without fiefs or recruit anywhere

            var hasBasic = faction.BasicTroops.Count > 0;
            var hasElite = faction.EliteTroops.Count > 0;

            Log.Debug($"Custom troop presence: Basic={hasBasic}, Elite={hasElite}.");

            if (!hasBasic || !hasElite)
            {
                bool canInitClanTroops = faction.HasFiefs || Config.RecruitAnywhere == true;

                if (canInitClanTroops)
                {
                    // Local function to create all missing troops
                    void CreateAllTroops(bool copyWholeTree)
                    {
                        if (!hasBasic)
                            CreateTroops(faction, false, copyWholeTree);
                        if (!hasElite)
                            CreateTroops(faction, true, copyWholeTree);
                    }

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
                            affirmativeAction: () => CreateAllTroops(copyWholeTree: true),
                            negativeAction: () => CreateAllTroops(copyWholeTree: false)
                        )
                    );
                }
            }

            if (DoctrineAPI.IsDoctrineUnlocked<CulturalPride>())
            {
                bool hasMilitiaMelee = faction.MilitiaMelee != null;
                bool hasMilitiaMeleeElite = faction.MilitiaMeleeElite != null;
                bool hasMilitiaRanged = faction.MilitiaRanged != null;
                bool hasMilitiaRangedElite = faction.MilitiaRangedElite != null;

                Log.Debug(
                    $"Militia presence: Melee={hasMilitiaMelee}, MeleeElite={hasMilitiaMeleeElite}, Ranged={hasMilitiaRanged}, RangedElite={hasMilitiaRangedElite}."
                );

                if (
                    !(
                        hasMilitiaMelee
                        && hasMilitiaMeleeElite
                        && hasMilitiaRanged
                        && hasMilitiaRangedElite
                    )
                )
                {
                    {
                        Log.Info("Initializing missing militia troops.");

                        if (!hasMilitiaMelee)
                            CreateMilitiaTroop(faction, isElite: false, isMelee: true);
                        if (!hasMilitiaMeleeElite)
                            CreateMilitiaTroop(faction, isElite: true, isMelee: true);
                        if (!hasMilitiaRanged)
                            CreateMilitiaTroop(faction, isElite: false, isMelee: false);
                        if (!hasMilitiaRangedElite)
                            CreateMilitiaTroop(faction, isElite: true, isMelee: false);
                    }
                }
            }

            if (DoctrineAPI.IsDoctrineUnlocked<RoyalPatronage>())
            {
                var culture = faction.Culture;

                if (faction.CaravanGuard is null && culture?.CaravanGuard != null)
                {
                    Log.Info("Creating Caravan Guard troop for faction.");
                    CreateSpecialTroop(
                        culture.CaravanGuard,
                        faction,
                        SpecialTroopType.CaravanGuard
                    );
                }

                if (faction.CaravanMaster is null && culture?.CaravanMaster != null)
                {
                    Log.Info("Creating Caravan Master troop for faction.");
                    CreateSpecialTroop(
                        culture.CaravanMaster,
                        faction,
                        SpecialTroopType.CaravanMaster
                    );
                }

                if (faction.Villager is null && culture?.Villager != null)
                {
                    Log.Info("Creating Villager troop for faction.");
                    CreateSpecialTroop(culture.Villager, faction, SpecialTroopType.Villager);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates a retinue troop for the given faction.
        /// </summary>
        private static void CreateRetinueTroop(WFaction faction, bool isElite)
        {
            Log.Info($"Creating retinue troop for faction {faction.Name} (elite={isElite})");

            troopsAwaitingRegistration = true; // Mark that new troops have been created

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

            var retinue = new WCharacter();

            retinue.FillFrom(tpl, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            // Rename it
            retinue.Name = MakeRetinueName(faction, isElite);

            Log.Info(
                $"Created retinue troop {retinue.Name} ({retinue}) for {faction.Name} (from {tpl})"
            );

            // Non-transferable
            retinue.IsNotTransferableInPartyScreen = true;

            // Unlock items
            UnlockAll(retinue);

            // Activate
            retinue.Activate();

            // Assign to faction
            if (isElite)
                faction.RetinueElite = retinue;
            else
                faction.RetinueBasic = retinue;
        }

        /// <summary>
        /// Builds a retinue troop name for a custom troop, based on faction and elite status.
        /// </summary>
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Militias                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates a militia troop for the given faction.
        /// </summary>
        private static void CreateMilitiaTroop(WFaction faction, bool isElite, bool isMelee)
        {
            troopsAwaitingRegistration = true; // Mark that new troops have been created

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

            var militia = new WCharacter();

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
            UnlockAll(militia);

            // Activate
            militia.Activate();

            // Assign to faction
            if (isMelee)
            {
                if (isElite)
                    faction.MilitiaMeleeElite = militia;
                else
                    faction.MilitiaMelee = militia;
            }
            else
            {
                if (isElite)
                    faction.MilitiaRangedElite = militia;
                else
                    faction.MilitiaRanged = militia;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Special Troops                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private enum SpecialTroopType
        {
            CaravanGuard,
            CaravanMaster,
            Villager,
        }

        private static void CreateSpecialTroop(
            WCharacter tpl,
            WFaction faction,
            SpecialTroopType type
        )
        {
            troopsAwaitingRegistration = true; // Mark that new troops have been created

            if (tpl == null)
                return;

            var troop = new WCharacter();

            troop.FillFrom(tpl, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            Log.Info($"Created special troop {troop.Name} (from {tpl})");
            Log.Info($"troop vanilla id set to {troop.VanillaStringId}");

            // Rename it
            troop.Name = BuildTroopName(tpl, faction);

            // Unlock items
            UnlockAll(troop);

            // Activate
            troop.Activate();

            // Assign to faction
            switch (type)
            {
                case SpecialTroopType.CaravanGuard:
                    faction.CaravanGuard = troop;
                    break;
                case SpecialTroopType.CaravanMaster:
                    faction.CaravanMaster = troop;
                    break;
                case SpecialTroopType.Villager:
                    faction.Villager = troop;
                    break;
                default:
                    break;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Regular Troops                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Build the elite tree for a faction.
        /// </summary>
        public static void CreateTroops(WFaction faction, bool isElite, bool copyWholeTree)
        {
            troopsAwaitingRegistration = true; // Mark that new troops have been created

            var culture = faction.Culture;

            var root = isElite ? culture?.RootElite : culture?.RootBasic;
            if (root == null)
            {
                Log.Warn(
                    $"Cannot clone {(isElite ? "elite" : "basic")} troops for {faction.Name}: no {(isElite ? "elite" : "basic")} culture root."
                );
                return;
            }

            var clones = CloneTroopTreeRecursive(root, isElite, faction, null, copyWholeTree)
                .Where(t => t != null)
                .ToList();

            Log.Info(
                $"Cloned {clones.Count} {(isElite ? "elite" : "basic")} troops for {faction.Name}."
            );

            // Assign to faction
            if (isElite)
                faction.RootElite = clones.FirstOrDefault();
            else
                faction.RootBasic = clones.FirstOrDefault();
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

            // Wrap the custom troop
            var troop = new WCharacter();

            // Copy from the original troop
            troop.FillFrom(tpl, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            // Rename it
            troop.Name = BuildTroopName(tpl, faction);

            // Add to upgrade targets of the parent, if any
            parent?.AddUpgradeTarget(troop);

            // Unlock items (vanilla source items)
            UnlockAll(vanilla);

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unlocks items for a single troop.
        /// </summary>
        private static void UnlockAll(WCharacter troop)
        {
            foreach (var equipment in troop.Loadout.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();
        }
    }
}
