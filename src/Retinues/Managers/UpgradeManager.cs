using System;
using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Managers
{
    /// <summary>
    /// Upgrade target creation, rank up flow, and source suggestions.
    /// </summary>
    [SafeClass]
    public static class UpgradeManager
    {
        /// <summary>
        /// Create and activate a new upgrade target for a troop.
        /// </summary>
        public static WCharacter AddUpgradeTarget(WCharacter troop, string targetName)
        {
            Log.Debug($"AddUpgradeTarget: '{targetName}' for {troop?.Name}");

            // Create child troop using parent constructor
            var child = new WCharacter(troop);

            // Fill child from parent
            child.FillFrom(
                troop,
                keepUpgrades: false,
                keepEquipment: Config.PayForEquipment == false, // don't copy equipment unless it's free
                keepSkills: true
            );

            // Customize child properties
            child.Name = targetName.Trim();
            child.Level = troop.Level + 5;

            return child;
        }

        /// <summary>
        /// Returns true if the troop can add an upgrade target.
        /// </summary>
        public static bool CanAddUpgradeToTroop(WCharacter character)
        {
            return GetAddUpgradeToTroopReason(character) == null;
        }

        /// <summary>
        /// Returns a text reason why the troop cannot be upgraded, or null if allowed.
        /// </summary>
        public static TextObject GetAddUpgradeToTroopReason(WCharacter character)
        {
            if (character == null)
                return L.T("invalid_args", "Invalid arguments.");

            if (character.IsMilitia)
                return L.T("militia_no_upgrade", "Militia cannot be upgraded.");

            if (character.IsRetinue)
                return L.T("retinue_no_upgrade", "Retinues cannot be upgraded.");

            // Max tier reached
            if (character.IsMaxTier)
                return L.T("max_tier", "Troop is at max tier.");

            // Determine max upgrades allowed
            int maxUpgrades = character.IsElite ? Config.MaxEliteUpgrades : Config.MaxBasicUpgrades;

            if (DoctrineAPI.IsDoctrineUnlocked<MastersAtArms>() && character.IsElite)
                maxUpgrades += 1; // +1 upgrade slot for elite troops with Masters at Arms

            // Cap upgrades at 4
            maxUpgrades = Math.Min(maxUpgrades, 4);

            // Max upgrades reached
            if (character.UpgradeTargets.Count() >= maxUpgrades)
                return L.T("max_upgrades_reached", "Troop has reached maximum amount of upgrades.");

            return null;
        }
    }
}
