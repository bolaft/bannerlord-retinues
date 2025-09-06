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

        private static readonly bool _disallowMountsForTier1 = true;
        private static readonly bool _limitEquipmentByTier = true;
        private static readonly bool _payForTroopEquipment = true;
        private static readonly bool _allEquipmentUnlocked = false;
        private static readonly bool _recruitClanTroopsAnywhere = false;

        public static bool DisallowMountsForTier1 { get; set; } = _disallowMountsForTier1;
        public static bool LimitEquipmentByTier { get; set; } = _limitEquipmentByTier;
        public static bool PayForTroopEquipment { get; set; } = _payForTroopEquipment;
        public static bool AllEquipmentUnlocked { get; set; } = _allEquipmentUnlocked;
        public static bool RecruitClanTroopsAnywhere { get; set; } = _recruitClanTroopsAnywhere;

        static Config()
        {
            LoadConfig();
        }

        // Loads config from a simple INI-like file: key=value per line, ignores unknowns and comments
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
                    if (key.Equals("DisallowMountsForTier1", StringComparison.OrdinalIgnoreCase))
                        DisallowMountsForTier1 = ParseBool(value, _disallowMountsForTier1);
                    else if (key.Equals("LimitEquipmentByTier", StringComparison.OrdinalIgnoreCase))
                        LimitEquipmentByTier = ParseBool(value, _limitEquipmentByTier);
                    else if (key.Equals("PayForTroopEquipment", StringComparison.OrdinalIgnoreCase))
                        PayForTroopEquipment = ParseBool(value, _payForTroopEquipment);
                    else if (key.Equals("AllEquipmentUnlocked", StringComparison.OrdinalIgnoreCase))
                        AllEquipmentUnlocked = ParseBool(value, _allEquipmentUnlocked);
                    else if (key.Equals("RecruitClanTroopsAnywhere", StringComparison.OrdinalIgnoreCase))
                        RecruitClanTroopsAnywhere = ParseBool(value, _recruitClanTroopsAnywhere);
                }
            }
            catch { /* ignore errors, use defaults */ }
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (bool.TryParse(value, out var b)) return b;
            if (int.TryParse(value, out var i)) return i != 0;
            return fallback;
        }
    }
}