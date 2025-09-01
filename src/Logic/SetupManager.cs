using TaleWorlds.CampaignSystem;
using System;
using System.Collections.Generic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Logic
{
    public static class SetupManager
    {
        public static void Setup()
        {
            HeroWrapper hero = new HeroWrapper();
            var clan = hero.Clan;
            var culture = hero.Culture;

            CloneCultureTroops(clan, culture);

            EquipmentManager.UnlockFromCulture(culture);
        }

        private static void CloneCultureTroops(ClanWrapper clan, CultureWrapper culture)
        {
            Log.Debug("[SetupManager] Starting troop setup...");
            try
            {
                if (clan == null || culture == null)
                {
                    Log.Warn("[SetupManager] Clan or culture not found. Aborting troop setup.");
                    return;
                }

                string namePrefix = clan.Name + " ";

                Log.Debug($"[SetupManager] Cloning troop trees for clan '{clan.Name}' and culture '{culture.Name}'.");

                // Clone basic troop tree and collect clones
                CharacterWrapper basicRoot = culture.RootBasic;
                int basicCount = 0;
                if (basicRoot != null)
                {
                    foreach (var clone in CloneTroopTree(basicRoot, namePrefix))
                    {
                        TroopManager.AddBasicTroop(clone);
                        basicCount++;
                    }
                    Log.Debug($"[SetupManager] Cloned basic troop tree. {basicCount} troops cloned.");
                }
                else
                {
                    Log.Warn("[SetupManager] No basic troop root found.");
                }

                // Clone elite troop tree and collect clones
                CharacterWrapper eliteRoot = culture.RootElite;
                int eliteCount = 0;
                if (eliteRoot != null)
                {
                    foreach (var clone in CloneTroopTree(eliteRoot, namePrefix))
                    {
                        TroopManager.AddEliteTroop(clone);
                        eliteCount++;
                    }
                    Log.Debug($"[SetupManager] Cloned elite troop tree. {eliteCount} troops cloned.");
                }
                else
                {
                    Log.Warn("[SetupManager] No elite troop root found.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SetupManager] Exception during troop setup: {ex}");
            }
        }

        private static IEnumerable<CharacterWrapper> CloneTroopTree(CharacterWrapper root, string namePrefix)
        {
            foreach (var clone in CloneTroopTree(root, namePrefix, null))
                yield return clone;
        }

        private static IEnumerable<CharacterWrapper> CloneTroopTree(CharacterWrapper original, string namePrefix, CharacterWrapper parent = null)
        {
            string newName = namePrefix + original.Name;

            CharacterWrapper clone = original.Clone();
            clone.Name = newName;

            if (parent != null)
            {
                clone.Parent = parent;
                parent.AddUpgradeTarget(clone);
            }

            // Copied troops' equipment is always unlocked
            EquipmentManager.UnlockFromTroop(clone);

            yield return clone;

            foreach (var child in original.UpgradeTargets ?? new TaleWorlds.CampaignSystem.CharacterObject[0])
            {
                var childWrapper = new CharacterWrapper(child);
                childWrapper.Parent = clone;
                foreach (var descendant in CloneTroopTree(childWrapper, namePrefix, clone))
                {
                    yield return descendant;
                }
            }
        }
    }
}
