using System;
using System.Collections.Generic;
using CustomClanTroops.Wrappers.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Core;
using CustomClanTroops.Utils;
using System.Reflection;

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

            // 1) Clone from the source troop
            var cloneObj = CharacterObject.CreateFrom(original.BaseCharacter);

            // 2) Set a unique StringId via reflection
            if (MBObjectManager.Instance.GetObject<CharacterObject>(newId) != null)
                throw new System.InvalidOperationException($"An object with id '{newId}' already exists.");

            var stringIdProp = typeof(MBObjectBase).GetProperty("StringId",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            stringIdProp.SetValue(cloneObj, newId);

            // 3) Register the new object so the game "knows" it
            MBObjectManager.Instance.RegisterObject(cloneObj);

            // 4) Wrap it and finish your tweaks
            CharacterWrapper clone = new CharacterWrapper(cloneObj);
            clone.SetName(newName);

            // Set UpgradeTargets
            try
            {
                clone.SetUpgradeTargets(new CharacterObject[0]);
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception setting UpgradeTargets: {ex}");
            }

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
