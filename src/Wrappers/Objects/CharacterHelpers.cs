using System;
using System.Collections.Generic;
using CustomClanTroops.Wrappers.Objects;
using TaleWorlds.Core;

namespace CustomClanTroops.Wrappers.Objects
{
    public static class CharacterHelpers
    {
        public static CharacterWrapper CreateUpgradePath(CharacterWrapper origin, string namePrefix, string idPrefix, string name, string id)
        {
            int newLevel = origin.Level + 5;
            var newSkills = new List<(SkillObject skill, int value)>(origin.Skills);
            var newEquipments = new List<Equipment>(origin.Equipments);
            var culture = origin.BaseCharacter.Culture;
            var troop = new CharacterWrapper(
                namePrefix + name,
                idPrefix + id,
                newLevel,
                culture,
                newSkills,
                newEquipments,
                null, // upgradeTargets
                origin.UpgradeRequiresItemFromCategory
            );
            AddUpgradeTarget(origin, troop);
            return troop;
        }

        public static CharacterWrapper CloneTroop(CharacterWrapper original, string namePrefix, string idPrefix)
        {
            string newName = namePrefix + original.Name;
            string newId = idPrefix + original.StringId;
            var clone = new CharacterWrapper(
                newName,
                newId,
                original.Level,
                original.Culture,
                original.Skills,
                original.Equipments,
                null, // upgradeTargets
                original.UpgradeRequiresItemFromCategory
            );
            return clone;
        }

        public static void CloneTroopTree(CharacterWrapper root, string namePrefix, string idPrefix)
        {
            CloneTroopTree(root, namePrefix, idPrefix, null);
        }

        public static CharacterWrapper CloneTroopTree(CharacterWrapper original, string namePrefix, string idPrefix, CharacterWrapper parent = null)
        {
            CharacterWrapper clone = CloneTroop(original, namePrefix, idPrefix);

            if (parent != null)
            {
                AddUpgradeTarget(parent, clone);
            }

            foreach (var child in original.UpgradeTargets ?? new TaleWorlds.CampaignSystem.CharacterObject[0])
            {
                var childWrapper = new CharacterWrapper(child);
                CloneTroopTree(childWrapper, namePrefix, idPrefix, clone);
            }

            return clone;
        }

        public static void AddUpgradeTarget(CharacterWrapper source, CharacterWrapper target)
        {
            var oldTargets = source.UpgradeTargets ?? new TaleWorlds.CampaignSystem.CharacterObject[0];
            var newTargets = new List<TaleWorlds.CampaignSystem.CharacterObject>(oldTargets);
            newTargets.Add(target.BaseCharacter);
            source.BaseCharacter.GetType().GetProperty("UpgradeTargets")?.SetValue(source.BaseCharacter, newTargets.ToArray());

        }
    }
}
