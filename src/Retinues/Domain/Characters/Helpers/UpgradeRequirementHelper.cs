using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Helpers
{
    /// <summary>
    /// Compact keys representing upgrade item requirement categories.
    /// </summary>
    public enum UpgradeItemRequirementKey
    {
        None = 0,
        Horse = 1,
        WarHorse = 2,
    }

    /// <summary>
    /// Utilities for computing and applying item-based upgrade requirements (e.g., mounts).
    /// </summary>
    public static class UpgradeRequirementHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Keys                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Maps an ItemCategory to a persisted UpgradeItemRequirementKey.
        /// </summary>
        public static UpgradeItemRequirementKey ToUpgradeKey(ItemCategory category)
        {
            if (category == null)
                return UpgradeItemRequirementKey.None;

            if (
                category == DefaultItemCategories.WarHorse
                || category == DefaultItemCategories.NobleHorse
            )
                return UpgradeItemRequirementKey.WarHorse;

            if (category == DefaultItemCategories.Horse)
                return UpgradeItemRequirementKey.Horse;

            // Unknown category: treat as "Horse" rather than exploding saves.
            return UpgradeItemRequirementKey.Horse;
        }

        /// <summary>
        /// Resolves the ItemCategory represented by a requirement key.
        /// </summary>
        public static ItemCategory ResolveUpgradeItemCategory(UpgradeItemRequirementKey key)
        {
            return key switch
            {
                UpgradeItemRequirementKey.Horse => DefaultItemCategories.Horse,
                UpgradeItemRequirementKey.WarHorse => DefaultItemCategories.WarHorse,
                _ => null,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Base application                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Applies the resolved upgrade requirement category to the given base object property/field.
        /// </summary>
        public static void ApplyRequirementToBase(
            object baseObj,
            string upgradeReqBaseName,
            UpgradeItemRequirementKey key
        )
        {
            if (baseObj == null || string.IsNullOrWhiteSpace(upgradeReqBaseName))
                return;

            var category = ResolveUpgradeItemCategory(key);

            try
            {
                if (Reflection.HasProperty(baseObj, upgradeReqBaseName))
                    Reflection.SetPropertyValue(baseObj, upgradeReqBaseName, category);
                else if (Reflection.HasField(baseObj, upgradeReqBaseName))
                    Reflection.SetFieldValue(baseObj, upgradeReqBaseName, category);
            }
            catch
            {
                // If TW changes internals, we just don't apply.
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Requirement computation                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines the highest mount requirement key present in a character's battle equipment sets.
        /// </summary>
        public static UpgradeItemRequirementKey GetBestBattleMountKey(WCharacter c)
        {
            if (c == null)
                return UpgradeItemRequirementKey.None;

            var equipments = c.Equipments;
            if (equipments == null || equipments.Count == 0)
                return UpgradeItemRequirementKey.None;

            var best = UpgradeItemRequirementKey.None;

            for (int i = 0; i < equipments.Count; i++)
            {
                var eq = equipments[i];
                if (eq?.IsCivilian != false)
                    continue;

                var horse = eq.Get(EquipmentIndex.Horse);
                var key = ToUpgradeKey(horse?.Category);

                if ((int)key > (int)best)
                    best = key;

                if (best == UpgradeItemRequirementKey.WarHorse)
                    break;
            }

            return best;
        }

        /// <summary>
        /// Returns the collection of source characters to consider when computing requirements.
        /// </summary>
        public static List<WCharacter> GetItemRequirementSources(WCharacter c)
        {
            if (c == null)
                return null;

            // Retinues: treat conversion sources as "upgrade sources" for requirement computation.
            // Non-retinues: use the real tree sources.
            return c.IsRetinue ? c.ConversionSources : c.UpgradeSources;
        }

        /// <summary>
        /// Computes the requirement key that should be applied to a character based on its sources.
        /// </summary>
        public static UpgradeItemRequirementKey ComputeRequiredKeyFromSources(
            WCharacter c,
            List<WCharacter> sources
        )
        {
            if (c == null)
                return UpgradeItemRequirementKey.None;

            var targetKey = GetBestBattleMountKey(c);

            if (targetKey == UpgradeItemRequirementKey.None)
                return UpgradeItemRequirementKey.None;

            if (sources == null || sources.Count == 0)
                return UpgradeItemRequirementKey.None;

            for (int i = 0; i < sources.Count; i++)
            {
                var s = sources[i];
                if (s == null)
                    continue;

                var sk = GetBestBattleMountKey(s);
                if ((int)sk >= (int)targetKey)
                    return UpgradeItemRequirementKey.None;
            }

            return targetKey;
        }

        /// <summary>
        /// Recursively propagates requirement updates upward through upgrade targets with loop protection.
        /// </summary>
        public static void UpdateTargetsRecursive(
            WCharacter root,
            HashSet<string> visited,
            System.Action<WCharacter> updateTargetNoRecurse
        )
        {
            if (root == null || visited == null || updateTargetNoRecurse == null)
                return;

            var id = root.StringId;
            if (string.IsNullOrEmpty(id) || !visited.Add(id))
                return;

            var targets = root.UpgradeTargets;
            if (targets == null || targets.Count == 0)
                return;

            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t?.Base == null)
                    continue;

                updateTargetNoRecurse(t);

                UpdateTargetsRecursive(t, visited, updateTargetNoRecurse);
            }
        }
    }
}
