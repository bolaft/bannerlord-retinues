using System;
using System.IO;

namespace CustomClanTroops.Utils
{
    public static class Config
    {
        private static string ConfigFile
        {
            get
            {
                var asmDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
                var moduleRoot = Directory.GetParent(asmDir)!.Parent!.FullName;
                return Path.Combine(moduleRoot, "config.ini");
            }
        }

        private static readonly bool _recruitAnywhere = false;
        private static readonly bool _payForEquipment = true;
        private static readonly int _allowedTierDifference = 3;
        private static readonly bool _noMountForTier1 = true;
        private static readonly bool _allEquipmentUnlocked = false;
        private static readonly bool _unlockFromKills = true;
        private static readonly bool _playerKillsOnly = false;
        private static readonly int _killsForUnlock = 100;

        public static bool RecruitAnywhere { get; set; } = _recruitAnywhere;
        public static bool PayForEquipment { get; set; } = _payForEquipment;
        public static int AllowedTierDifference { get; set; } = _allowedTierDifference;
        public static bool NoMountForTier1 { get; set; } = _noMountForTier1;
        public static bool AllEquipmentUnlocked { get; set; } = _allEquipmentUnlocked;
        public static bool UnlockFromKills { get; set; } = _unlockFromKills;
        public static bool PlayerKillsOnly { get; set; } = _playerKillsOnly;
        public static int KillsForUnlock { get; set; } = _killsForUnlock;

        public const string RecruitAnywhereHint = "Player can recruit clan troops in any settlement.";
        public const string PayForEquipmentHint = "Upgrading troop equipment costs money.";
        public const string AllowedTierDifferenceHint = "Maximum allowed tier difference between troops and equipment.";
        public const string NoMountForTier1Hint = "Tier 1 troops cannot have mounts.";
        public const string AllEquipmentUnlockedHint = "All equipment is already unlocked at game start.";
        public const string UnlockFromKillsHint = "Equipment is unlocked by defeating enemies that wear it.";
        public const string PlayerKillsOnlyHint = "Only kills made by the player count towards unlocking equipment.";
        public const string KillsForUnlockHint = "How many enemies wearing an item need to be defeated to unlock it.";

        static Config()
        {
            LoadConfig();
        }

        private static void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFile))
                    return;

                foreach (var line in File.ReadAllLines(ConfigFile))
                {
                    var trimmed = line.Trim();

                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || !trimmed.Contains("="))
                        continue;

                    var parts = trimmed.Split(['='], 2);
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (key.Equals("RecruitAnywhere", StringComparison.OrdinalIgnoreCase))
                        RecruitAnywhere = ParseBool(value, _recruitAnywhere);
                    else if (key.Equals("PayForEquipment", StringComparison.OrdinalIgnoreCase))
                        PayForEquipment = ParseBool(value, _payForEquipment);
                    else if (key.Equals("AllowedTierDifference", StringComparison.OrdinalIgnoreCase))
                        AllowedTierDifference = ParseInt(value, _allowedTierDifference);
                    else if (key.Equals("NoMountForTier1", StringComparison.OrdinalIgnoreCase))
                        NoMountForTier1 = ParseBool(value, _noMountForTier1);
                    else if (key.Equals("AllEquipmentUnlocked", StringComparison.OrdinalIgnoreCase))
                        AllEquipmentUnlocked = ParseBool(value, _allEquipmentUnlocked);
                    else if (key.Equals("UnlockFromKills", StringComparison.OrdinalIgnoreCase))
                        UnlockFromKills = ParseBool(value, _unlockFromKills);
                    else if (key.Equals("PlayerKillsOnly", StringComparison.OrdinalIgnoreCase))
                        PlayerKillsOnly = ParseBool(value, _playerKillsOnly);
                    else if (key.Equals("KillsForUnlock", StringComparison.OrdinalIgnoreCase))
                        KillsForUnlock = ParseInt(value, _killsForUnlock);
                }
            }
            catch { } // ignore errors, use defaults

            Log.Debug($"Config loaded:");
            Log.Debug($"  -> RecruitAnywhere = {RecruitAnywhere}");
            Log.Debug($"  -> PayForEquipment = {PayForEquipment}");
            Log.Debug($"  -> AllowedTierDifference = {AllowedTierDifference}");
            Log.Debug($"  -> NoMountForTier1 = {NoMountForTier1}");
            Log.Debug($"  -> AllEquipmentUnlocked = {AllEquipmentUnlocked}");
            Log.Debug($"  -> UnlockFromKills = {UnlockFromKills}");
            Log.Debug($"  -> PlayerKillsOnly = {PlayerKillsOnly}");
            Log.Debug($"  -> KillsForUnlock = {KillsForUnlock}");
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (bool.TryParse(value, out var b)) return b;
            if (int.TryParse(value, out var i)) return i != 0;
            return fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            if (int.TryParse(value, out var i)) return i;
            return fallback;
        }
    }
}