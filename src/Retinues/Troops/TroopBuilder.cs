using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game;
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
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensures all required custom troops exist for the given faction.
        /// Only refreshes parties and fixes the main party leader if at least
        /// one non-retinue troop was created during this call.
        /// </summary>
        public static void EnsureTroopsExist(WFaction faction)
        {
            if (faction == null)
                return; // Cannot ensure troops exist for null (no kingdom?) faction.

            var createdNonRetinue = UpdateFactionTroops(faction);

            if (!createdNonRetinue)
                return;

            // Update all existing parties for this faction
            WParty.SwapAll(
                members: true,
                prisoners: true,
                skipMainParty: true,
                skipLordParties: true,
                skipCustomParties: true
            );

            // Safety: Ensure main party leader is valid
            Safety.Helpers.EnsureMainPartyLeader();
        }

        /// <summary>
        /// Internal implementation that ensures retinues and, when allowed, regular/militia/special troops.
        /// Returns true if at least one non-retinue troop (regular, militia, special) was created.
        /// </summary>
        private static bool UpdateFactionTroops(WFaction faction)
        {
            if (faction == null)
                return false;

            Log.Debug($"Ensuring troops exist for faction: {faction?.Name ?? "null"}");

            // Retinues are always ensured, but do NOT trigger party swaps.
            EnsureRetinueTroops(faction);

            // Player clan without fiefs and no recruit-anywhere: skip non-retinue troops.
            if (ShouldSkipNonRetinueForPlayerClan(faction))
            {
                Log.Debug(
                    "Skipping non-retinue troop initialization for player clan without fiefs "
                        + "and recruit-anywhere disabled."
                );
                return false;
            }

            bool createdAnyNonRetinue = false;

            createdAnyNonRetinue |= EnsureRegularTroops(faction);
            createdAnyNonRetinue |= EnsureMilitiaTroops(faction);
            createdAnyNonRetinue |= EnsureSpecialTroops(faction);

            return createdAnyNonRetinue;
        }

        /// <summary>
        /// Determines whether we should skip creating non-retinue troops for the player clan.
        /// </summary>
        private static bool ShouldSkipNonRetinueForPlayerClan(WFaction faction)
        {
            if (faction != Player.Clan)
                return false;

            var recruitAnywhereEnabled = Config.RecruitAnywhere == true;
            return !faction.HasFiefs && !recruitAnywhereEnabled;
        }

        /// <summary>
        /// Ensures both basic and elite retinues exist for the faction.
        /// Retinue creation does not count as "non-retinue" for party refresh.
        /// </summary>
        private static void EnsureRetinueTroops(WFaction faction)
        {
            if (faction.RetinueElite is null)
                CreateRetinueTroop(faction, isElite: true);
            else
                Log.Debug("Elite retinue found, no need to initialize.");

            if (faction.RetinueBasic is null)
                CreateRetinueTroop(faction, isElite: false);
            else
                Log.Debug("Basic retinue found, no need to initialize.");
        }

        /// <summary>
        /// Ensures basic and elite regular custom troops exist (if possible).
        /// May show an inquiry to let the player choose between copying the culture
        /// tree or starting from scratch.
        ///
        /// NOTE: The actual creation of regular troops happens later in the
        /// inquiry callbacks, so this method returns false to avoid triggering
        /// party swaps prematurely.
        /// </summary>
        private static bool EnsureRegularTroops(WFaction faction)
        {
            var hasBasic = faction.BasicTroops.Count > 0;
            var hasElite = faction.EliteTroops.Count > 0;

            Log.Debug($"Custom troop presence: Basic={hasBasic}, Elite={hasElite}.");

            if (hasBasic && hasElite)
                return false;

            bool canInitClanTroops = faction.HasFiefs || Config.RecruitAnywhere == true;
            if (!canInitClanTroops)
                return false;

            // Local function to create all missing troops
            void CreateAllTroops(bool copyWholeTree)
            {
                if (!hasBasic)
                    CreateTroops(faction, isElite: false, copyWholeTree: copyWholeTree);
                if (!hasElite)
                    CreateTroops(faction, isElite: true, copyWholeTree: copyWholeTree);
            }

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("custom_troops_inquiry_title", "Custom Troops Unlocked"),
                    text: L.T(
                            "custom_troops_inquiry_body",
                            "Your {FACTION}'s custom troops are now unlocked.\n\nWould you like to clone the entire {CULTURE} troop tree, or would you prefer building them from scratch?\n\nCopying your culture's troops will provide you with good gear and good troops. Starting from scratch is the more difficult choice.\n\nThis decision is irreversible."
                        )
                        .SetTextVariable("FACTION", faction.IsPlayerClan ? "clan" : "kingdom")
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

            // Regular troops are created asynchronously via the inquiry callbacks,
            // so we cannot detect creation here. Do not trigger party swaps.
            return false;
        }

        /// <summary>
        /// Ensures militia troops exist for the faction when the Cultural Pride
        /// doctrine is unlocked. Returns true if any militia troop was created.
        /// </summary>
        private static bool EnsureMilitiaTroops(WFaction faction)
        {
            if (!DoctrineAPI.IsDoctrineUnlocked<CulturalPride>())
                return false;

            bool hasMilitiaMelee = faction.MilitiaMelee != null;
            bool hasMilitiaMeleeElite = faction.MilitiaMeleeElite != null;
            bool hasMilitiaRanged = faction.MilitiaRanged != null;
            bool hasMilitiaRangedElite = faction.MilitiaRangedElite != null;

            Log.Debug(
                $"Militia presence: Melee={hasMilitiaMelee}, MeleeElite={hasMilitiaMeleeElite}, "
                    + $"Ranged={hasMilitiaRanged}, RangedElite={hasMilitiaRangedElite}."
            );

            bool created = false;

            if (!hasMilitiaMelee)
            {
                CreateMilitiaTroop(faction, isElite: false, isMelee: true);
                created = true;
            }

            if (!hasMilitiaMeleeElite)
            {
                CreateMilitiaTroop(faction, isElite: true, isMelee: true);
                created = true;
            }

            if (!hasMilitiaRanged)
            {
                CreateMilitiaTroop(faction, isElite: false, isMelee: false);
                created = true;
            }

            if (!hasMilitiaRangedElite)
            {
                CreateMilitiaTroop(faction, isElite: true, isMelee: false);
                created = true;
            }

            return created;
        }

        /// <summary>
        /// Ensures special troops (caravan guard/master, villager) exist for the
        /// faction when the Royal Patronage doctrine is unlocked.
        /// Returns true if any special troop was created.
        /// </summary>
        private static bool EnsureSpecialTroops(WFaction faction)
        {
            if (!DoctrineAPI.IsDoctrineUnlocked<RoyalPatronage>())
                return false;

            var culture = faction.Culture;
            bool created = false;

            if (faction.CaravanGuard is null && culture?.CaravanGuard != null)
            {
                Log.Info("Creating Caravan Guard troop for faction.");
                CreateSpecialTroop(culture.CaravanGuard, faction, RootCategory.CaravanGuard);
                created = true;
            }

            if (faction.CaravanMaster is null && culture?.CaravanMaster != null)
            {
                Log.Info("Creating Caravan Master troop for faction.");
                CreateSpecialTroop(culture.CaravanMaster, faction, RootCategory.CaravanMaster);
                created = true;
            }

            if (faction.Villager is null && culture?.Villager != null)
            {
                Log.Info("Creating Villager troop for faction.");
                CreateSpecialTroop(culture.Villager, faction, RootCategory.Villager);
                created = true;
            }

            return created;
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

            var root = isElite ? faction.Culture.RootElite : faction.Culture.RootBasic;
            if (root == null)
            {
                Log.Error(
                    $"Cannot create retinue troop for faction {faction.Name} because its culture {faction.Culture.Name} "
                        + $"has no {(isElite ? "elite" : "basic")} root troop."
                );
                return;
            }

            var tpl = FindTemplate(root, tierBonus: faction == Player.Kingdom ? 2 : 0);

            var retinue = isElite
                ? new WCharacter(faction, RootCategory.RetinueElite)
                : new WCharacter(faction, RootCategory.RetinueBasic);

            // Copy from template
            retinue.FillFrom(tpl, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            // Rename it
            retinue.Name = MakeRetinueName(faction, isElite);

            // Common operations
            Initialize(retinue);
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
            WCharacter root;

            if (isMelee)
                root = isElite ? faction.Culture.MilitiaMeleeElite : faction.Culture.MilitiaMelee;
            else
                root = isElite ? faction.Culture.MilitiaRangedElite : faction.Culture.MilitiaRanged;

            if (root == null)
            {
                Log.Error(
                    $"Cannot create militia troop for faction {faction.Name} because its culture {faction.Culture.Name} "
                        + $"has no {(isElite ? "elite" : "basic")} {(isMelee ? "melee" : "ranged")} militia troop."
                );
                return;
            }

            var category = isMelee
                ? (isElite ? RootCategory.MilitiaMeleeElite : RootCategory.MilitiaMelee)
                : (isElite ? RootCategory.MilitiaRangedElite : RootCategory.MilitiaRanged);

            var militia = new WCharacter(faction, category);

            // Copy from template
            militia.FillFrom(root, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            // Rename it
            militia.Name = BuildTroopName(root, faction);

            // Rank up so skill caps/totals match vanilla militia
            militia.Level += 5;

            // Common operations
            Initialize(militia);

            Log.Info($"Created militia troop {militia.Name} for {faction.Name} (from {root})");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Special Troops                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void CreateSpecialTroop(
            WCharacter tpl,
            WFaction faction,
            RootCategory category
        )
        {
            if (tpl == null)
                return;

            var troop = new WCharacter(faction, category);

            // Copy from template
            troop.FillFrom(tpl, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            // Rename it
            troop.Name = BuildTroopName(tpl, faction);

            // Common operations
            Initialize(troop);

            Log.Info($"Created special troop {troop.Name} (from {tpl})");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Regular Troops                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Build the elite or basic tree for a faction.
        /// </summary>
        public static void CreateTroops(WFaction faction, bool isElite, bool copyWholeTree)
        {
            var culture = faction.Culture;

            var root = isElite ? culture?.RootElite : culture?.RootBasic;
            if (root == null)
            {
                Log.Warn(
                    $"Cannot clone {(isElite ? "elite" : "basic")} troops for {faction.Name}: "
                        + $"no {(isElite ? "elite" : "basic")} culture root."
                );
                return;
            }

            var clones = CloneTroopTreeRecursive(root, isElite, faction, null, copyWholeTree)
                .Where(t => t != null)
                .ToList();

            Log.Info(
                $"Cloned {clones.Count} {(isElite ? "elite" : "basic")} troops for {faction.Name}."
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
                    $"Cannot clone troop {vanilla.Name} because no template was found for "
                        + $"{(isElite ? "elite" : "basic")} troops."
                );
                yield break;
            }

            // Wrap the custom troop
            WCharacter troop;

            // If parent is given, create as upgrade target of parent
            if (parent != null)
                troop = new WCharacter(parent);
            else
                // If no parent, create as root troop of faction
                troop = isElite
                    ? new WCharacter(faction, RootCategory.RootElite)
                    : new WCharacter(faction, RootCategory.RootBasic);

            // Copy from the original troop
            troop.FillFrom(tpl, keepUpgrades: false, keepEquipment: true, keepSkills: true);

            // Rename it
            troop.Name = BuildTroopName(tpl, faction);

            // Common operations
            Initialize(troop);

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
        private static void Initialize(WCharacter troop)
        {
            // Recompute derived properties
            troop.ComputeDerivedProperties();

            // Unlock all items in loadout
            foreach (var equipment in troop.Loadout.Equipments)
            foreach (var item in equipment.Items)
                item.Unlock();
        }
    }
}
