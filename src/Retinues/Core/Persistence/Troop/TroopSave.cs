using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Persistence.Troop
{
    public static class TroopSave
    {
        // =========================================================================
        // Saving
        // =========================================================================

        public static TroopSaveData Save(WCharacter character)
        {
            var data = new TroopSaveData
            {
                StringId = character.StringId,
                VanillaStringId = character.VanillaStringId,
                IsKingdomTroop = character.Faction == Player.Kingdom,
                IsElite = character.IsElite,
                IsEliteRetinue = character == character.Faction.RetinueElite,
                IsBasicRetinue = character == character.Faction.RetinueBasic,
                UpgradeTargets = character.UpgradeTargets?.Select(Save).ToList(),
                Name = character.Name,
                Level = character.Level,
                IsFemale = character.IsFemale,
                SkillCode = CodeFromSkills(character.Skills),
                EquipmentCode = character.Equipment.Code,
            };

            return data;
        }

        // =========================================================================
        // Loading
        // =========================================================================

        public static WCharacter Load(TroopSaveData data, WCharacter parent = null)
        {
            // Create character object from vanilla id
            var co = MBObjectManager.Instance.GetObject<CharacterObject>(data.VanillaStringId);

            // Wrap it
            var wco = new WCharacter(co);

            // Determine faction
            var faction = data.IsKingdomTroop ? Player.Kingdom : Player.Clan;

            // Clone it
            var clone = wco.Clone(
                faction: faction,
                parent: parent,
                keepUpgrades: false,
                keepEquipment: false,
                keepSkills: false
            );

            // Create the wrapped character
            clone.VanillaStringId = data.VanillaStringId;
            clone.Name = data.Name;
            clone.Level = data.Level;
            clone.IsFemale = data.IsFemale;
            clone.Skills = SkillsFromCode(data.SkillCode);
            clone.Equipments = [WEquipment.FromCode(data.EquipmentCode)];

            // Restore upgrade targets
            foreach (var child in data.UpgradeTargets ?? [])
                clone.AddUpgradeTarget(Load(child, parent: clone));

            // Toggle visibility flags
            clone.Register();

            // Retinues are not transferable
            if (data.IsEliteRetinue || data.IsBasicRetinue)
                clone.IsNotTransferableInPartyScreen = true;

            // Add to the appropriate troop list
            if (data.IsEliteRetinue)
                faction.RetinueElite = clone;
            else if (data.IsBasicRetinue)
                faction.RetinueBasic = clone;
            else if (data.IsElite)
                faction.EliteTroops.Add(clone);
            else
                faction.BasicTroops.Add(clone);

            // Return the created troop
            return clone;
        }

        // =========================================================================
        // Helpers
        // =========================================================================

        public static string CodeFromSkills(Dictionary<SkillObject, int> skills)
        {
            return string.Join(";", skills.Select(kv => $"{kv.Key.StringId}:{kv.Value}"));
        }

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
