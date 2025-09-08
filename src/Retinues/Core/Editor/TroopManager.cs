using TaleWorlds.Core;
using Retinues.Core.Game.Wrappers;
using System.Collections.Generic;

namespace Retinues.Core.Editor
{
    public static class TroopManager
    {
        public static List<WCharacter> CollectEliteTroops(WFaction faction)
        {
            return faction.EliteTroops;
        }

        public static List<WCharacter> CollectBasicTroops(WFaction faction)
        {
            return faction.BasicTroops;
        }

        public static void Rename(WCharacter troop, string newName)
        {
            troop.Name = newName.Trim();
        }

        public static void ChangeGender(WCharacter troop)
        {
            troop.IsFemale = !troop.IsFemale;
        }

        public static void ModifySkill(WCharacter troop, SkillObject skill, int delta)
        {
            troop.SetSkill(skill, troop.GetSkill(skill) + delta);
        }

        public static WCharacter AddUpgradeTarget(WCharacter troop, string targetName)
        {
            // Create the new troop by cloning
            var target = troop.Clone(
                faction: troop.Faction,
                parent: troop,
                keepUpgrades: false,
                keepEquipment: false,
                keepSkills: true
            );

            // Set name and level
            target.Name = targetName.Trim();
            target.Level = troop.Level + 5;

            // Add as an upgrade target
            troop.AddUpgradeTarget(target);

            // Add it the the faction's troop list
            if (target.IsElite)
                troop.Faction.EliteTroops.Add(target);
            else
                troop.Faction.BasicTroops.Add(target);

            return target;
        }
        
        public static void Remove(WCharacter troop)
        {
            // Stock the troop's equipment
            foreach (var item in troop.Equipment.Items)
                item.Stock();

            // Remove the troop
            troop.Remove();
        }
    }
}
