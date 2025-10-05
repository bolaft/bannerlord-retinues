using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Troops.Save;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Troops
{
    /// <summary>
    /// Static helpers for saving and loading custom troop data.
    /// Handles serialization to TroopSaveData and reconstruction from save payloads.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
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

            // Fill it
            troop.FillFrom(
                new WCharacter(data.VanillaStringId),
                keepUpgrades: false,
                keepEquipment: false,
                keepSkills: false
            );

            // Set properties
            troop.Name = data.Name;
            troop.Level = data.Level;
            troop.IsFemale = data.IsFemale;
            troop.Skills = SkillsFromCode(data.SkillCode);
            troop.Equipments = [WEquipment.FromCode(data.EquipmentCode)];

            // Restore upgrade targets
            foreach (var child in data.UpgradeTargets ?? [])
                troop.AddUpgradeTarget(Load(child));

            // Retinues are not transferable
            if (troop.IsRetinue)
                troop.IsNotTransferableInPartyScreen = true;

            // Activate
            troop.Activate();

            // Force recalculation of formation class based on equipment
            troop.ResetFormationClass();

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
                EquipmentCode = character.Equipment.Code,
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
