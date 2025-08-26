using TaleWorlds.CampaignSystem;
using System;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Logic
{
    public static class TroopSetup
    {
        /// <summary>
        /// Sets up the initial custom troops for the player's clan by cloning both troop trees (noble and non-noble)
        /// of the player's culture, prefixing with the clan's name.
        /// </summary>
        public static void ClonePlayerCultureTroops()
        {
            Log.Info("[TroopSetup] Starting troop setup...");
            try
            {
                HeroWrapper hero = new HeroWrapper();
                var clan = hero.Clan;
                var culture = hero.Culture;
                if (clan == null || culture == null)
                {
                    Log.Warn("[TroopSetup] Clan or culture not found. Aborting troop setup.");
                    return;
                }

                string namePrefix = clan.Name + " ";

                Log.Info($"[TroopSetup] Cloning troop trees for clan '{clan.Name}' and culture '{culture.Name}'.");

                // Clone basic troop tree
                CharacterWrapper basicRoot = culture.RootBasic;
                if (basicRoot != null)
                {
                    CharacterHelpers.CloneTroopTree(basicRoot, namePrefix, CharacterWrapper.BasicIdPrefix);
                    Log.Info("[TroopSetup] Cloned basic troop tree.");
                }
                else
                {
                    Log.Warn("[TroopSetup] No basic troop root found.");
                }

                // Clone elite troop tree
                CharacterWrapper eliteRoot = culture.RootElite;
                if (eliteRoot != null)
                {
                    CharacterHelpers.CloneTroopTree(eliteRoot, namePrefix, CharacterWrapper.EliteIdPrefix);
                    Log.Info("[TroopSetup] Cloned elite troop tree.");
                }
                else
                {
                    Log.Warn("[TroopSetup] No elite troop root found.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[TroopSetup] Exception during troop setup: {ex}");
            }
        }
    }
}
