using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Core;
using System.Reflection;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Wrappers.Objects
{
    public static class CharacterHelpers
    {
        public static CharacterWrapper CloneTroop(CharacterWrapper original, string newName, string newId)
        {
            // Clone from the source troop
            var cloneObj = CharacterObject.CreateFrom(original.GetCharacterObject());
            Log.Info($"[CharacterHelpers] Cloning troop '{original.StringId}' to '{cloneObj.StringId}' with name '{newName}'.");

            // Wrap it
            CharacterWrapper clone = new CharacterWrapper(cloneObj);

            // Set Name
            clone.SetName(newName);

            // Set UpgradeTargets
            clone.SetUpgradeTargets(new CharacterObject[0]);

            return clone;
        }

        public static IEnumerable<CharacterWrapper> CloneTroopTree(CharacterWrapper root, string namePrefix, string idPrefix)
        {
            foreach (var clone in CloneTroopTree(root, namePrefix, idPrefix, null))
                yield return clone;
        }

        public static IEnumerable<CharacterWrapper> CloneTroopTree(CharacterWrapper original, string namePrefix, string idPrefix, CharacterWrapper parent = null)
        {
            string newName = namePrefix + original.Name;
            string newId = idPrefix + original.StringId;

            CharacterWrapper clone = CloneTroop(original, newName, newId);

            if (parent != null)
            {
                parent.AddUpgradeTarget(clone);
            }

            yield return clone;

            foreach (var child in original.UpgradeTargets ?? new TaleWorlds.CampaignSystem.CharacterObject[0])
            {
                var childWrapper = new CharacterWrapper(child);
                foreach (var descendant in CloneTroopTree(childWrapper, namePrefix, idPrefix, clone))
                {
                    yield return descendant;
                }
            }
        }
    }
}
