
using System;
using System.Text;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace CustomClanTroops.Helpers
{
    public static class ItemHelpers
    {
        private static ItemObject _itemForTooltip;

        public static BasicTooltipViewModel MakeTooltip(ItemObject item)
        {
            if (item == null) return null;
            _itemForTooltip = item;
            return new BasicTooltipViewModel(BuildTooltip);
        }

        private static List<TooltipProperty> BuildTooltip()
        {
            var properties = new List<TooltipProperty>();

            properties.Add(new TooltipProperty(
                "", $"{ItemHelpers.GetItemClass(_itemForTooltip)} (Tier {ItemHelpers.GetItemTier(_itemForTooltip)})", 0, false, TooltipProperty.TooltipPropertyFlags.Title
            ));

            foreach (var line in ItemHelpers.DescribeItemStats(_itemForTooltip))
            {
                properties.Add(new TooltipProperty(
                    line, "", 0, false, TooltipProperty.TooltipPropertyFlags.None
                ));
            }

            return properties;
        }

        public static int GetItemTier(ItemObject item)
        {
            if (item == null) return 0;

            int tierIndex = Array.IndexOf(Enum.GetValues(item.Tier.GetType()), item.Tier);
            return tierIndex + 1;
        }

        public static string GetItemClass(ItemObject item)
        {
            if (item == null) return string.Empty;

            // --- Armor / slot-specific ---
            switch (item.ItemType)
            {
                case ItemObject.ItemTypeEnum.HeadArmor:     return "Helmet";
                case ItemObject.ItemTypeEnum.Cape:          return "Cape";
                case ItemObject.ItemTypeEnum.BodyArmor:     return "Armor";
                case ItemObject.ItemTypeEnum.HandArmor:     return "Gloves";
                case ItemObject.ItemTypeEnum.LegArmor:      return "Boots";
                case ItemObject.ItemTypeEnum.HorseHarness:  return "Horse Harness";
            }

            // --- Mounts (regular / war / noble / pack) ---
            if (item.HorseComponent != null)
            {
                var cat = item.ItemCategory;
                if (cat == DefaultItemCategories.NobleHorse) return "Noble Mount";
                if (cat == DefaultItemCategories.WarHorse)   return "War Mount";
                if (cat == DefaultItemCategories.Horse)      return "Mount";
                if (cat == DefaultItemCategories.PackAnimal) return "Pack Animal";
                // Fallback for modded categories: still a mount
                return "Mount";
            }

            // --- Weapons (from primary usage class) ---
            if (item.WeaponComponent != null && item.PrimaryWeapon != null)
                return FormatClass(item.PrimaryWeapon.WeaponClass.ToString());

            return string.Empty;
        }

        private static string FormatClass(string className)
        {
            // Insert spaces between camel case words in class names
            return System.Text.RegularExpressions.Regex.Replace(className, "([a-z])([A-Z])", "$1 $2");
        }

        public static List<string> DescribeItemStats(ItemObject item)
        {
            var lines = new List<string>();

            // No tooltip if item is null
            if (item == null)
                return lines;

            // ---------- ARMOR ----------
            var ac = item.ArmorComponent; // HeadArmor, BodyArmor, ArmArmor, LegArmor, ... (1.2.12)
            if (ac != null)
            {
                // Only print parts that are > 0 to keep it compact
                if (ac.HeadArmor > 0) lines.Add($"Head Armor: {ac.HeadArmor}");
                if (ac.BodyArmor > 0) lines.Add($"Body Armor: {ac.BodyArmor}");
                if (ac.ArmArmor  > 0) lines.Add($"Arm Armor: {ac.ArmArmor}");
                if (ac.LegArmor  > 0) lines.Add($"Leg Armor: {ac.LegArmor}");
            }

            // ---------- HORSE ----------
            var hc = item.HorseComponent; // Speed, Maneuver, ChargeDamage, HitPoints, etc. (1.2.12)
            if (hc != null)
            {
                lines.Add($"Speed: {hc.Speed}");
                lines.Add($"Maneuver: {hc.Maneuver}");
                lines.Add($"Charge: {hc.ChargeDamage}");
                lines.Add($"Hit Points: {hc.HitPoints}" + (hc.HitPointBonus != 0 ? $" (+{hc.HitPointBonus})" : ""));
            }

            // ---------- WEAPON / SHIELD ----------
            var wc = item.WeaponComponent;            // container for weapon usages
            var w  = item.PrimaryWeapon;              // main usage (WeaponComponentData) (1.2.12)
            if (wc != null && w != null)
            {
                // Shield (no ShieldComponent in 1.2.12; use WeaponComponentData fields)
                if (w.IsShield)
                {
                    if (w.Handling > 0) lines.Add($"Speed: {w.Handling}");
                    if (w.MaxDataValue > 0) lines.Add($"Hit Points: {w.MaxDataValue}");
                }
                else if (w.IsRangedWeapon)
                {
                    // Ranged
                    if (w.MissileSpeed  > 0) lines.Add($"Missile Speed: {w.MissileSpeed}");
                    if (w.MissileDamage > 0) lines.Add($"Damage: {w.MissileDamage}");
                    if (w.Accuracy      > 0) lines.Add($"Accuracy: {w.Accuracy}");
                }
                else // melee
                {
                    // Swing / Thrust blocks only if > 0 to avoid clutter on e.g., blunt-only
                    if (w.SwingDamage > 0 && w.SwingSpeed > 0)
                        lines.Add($"Swing Damage: {w.SwingDamage}");
                        lines.Add($"Swing Speed: {w.SwingSpeed}");
                    if (w.ThrustDamage > 0 && w.ThrustSpeed > 0)
                        lines.Add($"Thrust Damage: {w.ThrustDamage}");
                        lines.Add($"Thrust Speed: {w.ThrustSpeed}");
                    if (w.WeaponLength > 0) lines.Add($"Length: {w.WeaponLength}");
                    if (w.Handling     > 0) lines.Add($"Handling: {w.Handling}");
                }
            }

            // Difficulty on ItemObject can serve as a requirement hint (when set)
            if (item.RelevantSkill != null && item.Difficulty > 0)
                lines.Add($"{item.RelevantSkill.Name}: {item.Difficulty}");

            return lines;
        }
    }
}