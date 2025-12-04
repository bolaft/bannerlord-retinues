using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Safety.Legacy
{
    public sealed class LegacyTroopSaveData
    {
        [SaveableField(1)]
        public string StringId;

        [SaveableField(2)]
        public string VanillaStringId;

        [SaveableField(3)]
        public string Name;

        [SaveableField(4)]
        public int Level;

        [SaveableField(5)]
        public bool IsFemale;

        [SaveableField(6)]
        public string SkillCode;

        [SaveableField(7)]
        public string EquipmentCode; // Legacy

        [SaveableField(8)]
        [XmlArray("UpgradeTargets")]
        [XmlArrayItem("TroopSaveData")]
        public List<LegacyTroopSaveData> UpgradeTargets = [];

        [SaveableField(9)]
        public int XpPool = 0; // Legacy

        [SaveableField(10)]
        public List<string> EquipmentCodes = [];

        [SaveableField(11)]
        public string CultureId;

        [SaveableField(12)]
        public float AgeMin;

        [SaveableField(13)]
        public float AgeMax;

        [SaveableField(14)]
        public float WeightMin;

        [SaveableField(15)]
        public float WeightMax;

        [SaveableField(16)]
        public float BuildMin;

        [SaveableField(17)]
        public float BuildMax;

        [SaveableField(18)]
        public float HeightMin;

        [SaveableField(19)]
        public float HeightMax;

        [SaveableField(20)]
        public int Race = 0;
    }

    /// <summary>
    /// Static helpers for loading legacy custom troop data.
    /// </summary>
    [SafeClass]
    public static class LegacyTroopSaveConverter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Loading                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static (FactionSaveData clan, FactionSaveData kingdom) ConvertLegacyFactionData(
            List<LegacyTroopSaveData> roots
        )
        {
            Log.Info($"{roots.Count} legacy root troops found, migrating.");

            FactionSaveData clanData = new();
            FactionSaveData kingdomData = new();

            foreach (var root in roots)
            {
                // Load legacy troop data
                var data = ConvertLegacyTroopData(root);

                // Determine if kingdom or clan
                bool isKingdom = IsKingdom(root.StringId);

                // Select appropriate faction save data
                FactionSaveData faction = isKingdom ? kingdomData : clanData;

                // Determine category
                var category = GetCategory(root.StringId);

                // Add to appropriate faction based on category
                switch (category)
                {
                    case RootCategory.RetinueBasic:
                        faction.RetinueBasic = data;
                        break;
                    case RootCategory.RetinueElite:
                        faction.RetinueElite = data;
                        break;
                    case RootCategory.RootElite:
                        faction.RootElite = data;
                        break;
                    case RootCategory.RootBasic:
                        faction.RootBasic = data;
                        break;
                    case RootCategory.MilitiaMelee:
                        faction.MilitiaMelee = data;
                        break;
                    case RootCategory.MilitiaMeleeElite:
                        faction.MilitiaMeleeElite = data;
                        break;
                    case RootCategory.MilitiaRanged:
                        faction.MilitiaRanged = data;
                        break;
                    case RootCategory.MilitiaRangedElite:
                        faction.MilitiaRangedElite = data;
                        break;
                    case RootCategory.CaravanGuard:
                        faction.CaravanGuard = data;
                        break;
                    case RootCategory.CaravanMaster:
                        faction.CaravanMaster = data;
                        break;
                    case RootCategory.Villager:
                        faction.Villager = data;
                        break;
                    default:
                        Log.Warn(
                            $"Legacy troop '{root.StringId}' has unrecognized category, skipping."
                        );
                        break;
                }
            }

            return (clanData, kingdomData);
        }

        /// <summary>
        /// Loads a WCharacter from LegacyTroopSaveData, recursively restoring upgrades and properties.
        /// </summary>
        public static TroopSaveData ConvertLegacyTroopData(LegacyTroopSaveData data)
        {
            TroopSaveData troop = new()
            {
                StringId = data.StringId,
                VanillaStringId = data.VanillaStringId,
                Name = data.Name,
                Level = data.Level,
                IsFemale = data.IsFemale,
                CultureId = data.CultureId,
                Race = data.Race,
                UpgradeTargets = [.. data.UpgradeTargets.Select(ConvertLegacyTroopData)],
                EquipmentData = ConvertEquipmentData(data),
                SkillData = new TroopSkillData(SkillsFromCode(data.SkillCode)),
                BodyData = Config.EnableTroopCustomization ? ConvertBodyData(data) : null,
            };

            // Return the created troop data
            return troop;
        }

        private static TroopEquipmentData ConvertEquipmentData(LegacyTroopSaveData data)
        {
            List<string> codes;
            List<bool> flags;

            string emptyCode = new Equipment().CalculateEquipmentCode();

            if (data.EquipmentCode != null)
            {
                // Legacy single equipment code
                codes = [data.EquipmentCode, emptyCode];
                flags = [false, true];
            }
            else
            {
                if (data.EquipmentCodes == null || data.EquipmentCodes.Count == 0)
                {
                    codes = [emptyCode, emptyCode];
                    flags = [false, true];
                }
                else if (data.EquipmentCodes.Count == 1)
                {
                    codes = [data.EquipmentCodes[0], emptyCode];
                    flags = [false, true];
                }
                else
                {
                    codes = data.EquipmentCodes;
                    flags = [.. Enumerable.Repeat(false, codes.Count)];
                    flags[1] = true; // Mark second equipment as civilian
                }
            }

            return new TroopEquipmentData { Codes = codes, Civilians = flags };
        }

        private static TroopBodySaveData ConvertBodyData(LegacyTroopSaveData data)
        {
            var body = new TroopBodySaveData
            {
                AgeMin = data.AgeMin,
                AgeMax = data.AgeMax,
                WeightMin = data.WeightMin,
                WeightMax = data.WeightMax,
                BuildMin = data.BuildMin,
                BuildMax = data.BuildMax,
            };

            // Set height properties
            if (data.HeightMin > 0 && data.HeightMax > 0)
            {
                body.HeightMin = data.HeightMin;
                body.HeightMax = data.HeightMax;
            }

            return body;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the ID is an elite troop.
        /// </summary>
        private static bool IsElite(string id) => id != null && id.Contains("_elite_");

        /// <summary>
        /// Returns true if the ID is a kingdom troop.
        /// </summary>
        private static bool IsKingdom(string id) => id != null && id.Contains("_kingdom_");

        /// <summary>
        /// Determines the troop type from the ID.
        /// </summary>
        private static RootCategory GetCategory(string id)
        {
            if (id == null)
                return RootCategory.Other;

            // Special troops
            if (id.EndsWith("_retinue"))
                return IsElite(id) ? RootCategory.RetinueElite : RootCategory.RetinueBasic;
            if (id.EndsWith("_mmilitia"))
                return IsElite(id) ? RootCategory.MilitiaMeleeElite : RootCategory.MilitiaMelee;
            if (id.EndsWith("_rmilitia"))
                return IsElite(id) ? RootCategory.MilitiaRangedElite : RootCategory.MilitiaRanged;

            // Regular troops
            return IsElite(id) ? RootCategory.RootElite : RootCategory.RootBasic;
        }

        /// <summary>
        /// Parses a skill code string into a skill dictionary for loading.
        /// </summary>
        public static Dictionary<SkillObject, int> SkillsFromCode(string skillsString)
        {
            var result = new Dictionary<SkillObject, int>();
            if (string.IsNullOrWhiteSpace(skillsString))
                return result;

            var dict = skillsString
                .Split(';')
                .Select(part => part.Split(':'))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => int.Parse(parts[1]));

            foreach (var kv in dict)
            {
                var skill = MBObjectManager.Instance.GetObject<SkillObject>(kv.Key);
                if (skill != null)
                    result[skill] = kv.Value;
            }

            return result;
        }
    }
}
