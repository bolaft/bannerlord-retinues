using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Troops
{
    /// <summary>
    /// Static helpers for saving and loading custom troop data.
    /// Handles serialization to TroopSaveData and reconstruction from save payloads.
    /// </summary>
    [SafeClass]
    public static class TroopLoader
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Loading                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Loads a WCharacter from TroopSaveData, recursively restoring upgrades and properties.
        /// </summary>
        public static WCharacter Load(TroopSaveData data)
        {
            // Wrap it
            var troop = new WCharacter(data.StringId);

            // Get vanilla base
            var vanilla = new WCharacter(data.VanillaStringId);

            // Fill it
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: false, keepSkills: false);

            // Set properties
            troop.Name = data.Name;
            troop.Level = data.Level;
            troop.IsFemale = data.IsFemale;
            troop.Skills = SkillsFromCode(data.SkillCode);

            if (data.EquipmentCode != null)
            {
                // Legacy support for single equipment code
                Log.Warn(
                    "TroopSaveData.EquipmentCode is deprecated, use EquipmentCodes list instead."
                );
                troop.Loadout.Equipments =
                [
                    WEquipment.FromCode(
                        data.EquipmentCode,
                        troop.Loadout,
                        (int)EquipmentCategory.Battle
                    ),
                    WEquipment.FromCode(
                        data.EquipmentCode,
                        troop.Loadout,
                        (int)EquipmentCategory.Civilian
                    ),
                ];
            }
            else
            {
                if (troop.Loadout.Equipments.Count == 0)
                {
                    // create two empty equipments if none exist
                    troop.Loadout.Clear();
                }
                else if (troop.Loadout.Equipments.Count == 1)
                {
                    // ensure we have a second equipment for civilian
                    troop.Loadout.Equipments =
                    [
                        troop.Loadout.Equipments[0],
                        WEquipment.FromCode(null, troop.Loadout, (int)EquipmentCategory.Civilian),
                    ];
                }
                else
                {
                    // set correct flags
                    troop.Loadout.Equipments =
                    [
                        .. data.EquipmentCodes.Select(
                            (code, idx) => WEquipment.FromCode(code, troop.Loadout, idx)
                        ),
                    ];
                }
            }

            // Restore upgrade targets
            foreach (var child in data.UpgradeTargets ?? [])
                troop.AddUpgradeTarget(Load(child));

            // Retinues are not transferable
            if (troop.IsRetinue)
                troop.IsNotTransferableInPartyScreen = true;

            // Set culture visuals if present
            if (!string.IsNullOrEmpty(data.CultureId) && data.CultureId != vanilla.Culture.StringId)
            {
                var culture = MBObjectManager.Instance.GetObject<CultureObject>(data.CultureId);
                if (culture != null)
                {
                    Log.Info($"Applying culture '{data.CultureId}' to troop '{data.StringId}'");
                    troop.Culture = new WCulture(culture);
                    CharacterCustomization.ApplyPropertiesFromCulture(troop, culture);
                }
            }

            if (data.Race >= 0)
            {
                troop.Race = data.Race;
                troop.EnsureOwnBodyRange();
            }

            if (Config.EnableTroopCustomization)
            {
                // Set dynamic properties (already handles nulls)
                troop.SetDynamicEnd(true, data.AgeMin, data.WeightMin, data.BuildMin);
                troop.SetDynamicEnd(false, data.AgeMax, data.WeightMax, data.BuildMax);
                troop.Age = troop.AgeMin + troop.AgeMax / 2;

                // Set height properties
                if (data.HeightMin > 0 && data.HeightMax > 0)
                {
                    troop.HeightMin = data.HeightMin;
                    troop.HeightMax = data.HeightMax;
                }
            }

            // Activate
            troop.Activate();

            // Return the created troop
            return troop;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Saving                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Serializes a WCharacter to TroopSaveData, including upgrades and key properties.
        /// </summary>
        public static TroopSaveData Save(WCharacter character)
        {
            var data = new TroopSaveData
            {
                StringId = character.StringId,
                VanillaStringId = character.VanillaStringId,
                UpgradeTargets = character.UpgradeTargets?.Select(Save).ToList(),
                Name = character.Name,
                Level = character.Level,
                IsFemale = character.IsFemale,
                SkillCode = CodeFromSkills(character.Skills),
                EquipmentCodes = [.. character.Loadout.Equipments.Select(we => we.Code)],
                CultureId = character.Culture.StringId,
                AgeMin = character.AgeMin,
                AgeMax = character.AgeMax,
                WeightMin = character.WeightMin,
                WeightMax = character.WeightMax,
                BuildMin = character.BuildMin,
                BuildMax = character.BuildMax,
                HeightMin = character.HeightMin,
                HeightMax = character.HeightMax,
                Race = character.Race,
            };

            return data;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Serializes a skill dictionary to a string code for saving.
        /// </summary>
        public static string CodeFromSkills(Dictionary<SkillObject, int> skills)
        {
            return string.Join(";", skills.Select(kv => $"{kv.Key.StringId}:{kv.Value}"));
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
