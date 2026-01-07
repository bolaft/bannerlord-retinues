using System.Collections.Generic;
using Retinues.Framework.Model.Attributes;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Upgrade Requirements                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private enum UpgradeItemRequirementKey
        {
            None = 0,
            Horse = 1,
            WarHorse = 2,
        }

        private const string UpgradeReqKeyAttrName = "UpgradeRequiresItemFromCategoryKey";
        private const string UpgradeReqBaseName = "UpgradeRequiresItemFromCategory";

        private string UpgradeReqStoreKey =>
            $"{GetType().FullName}:{StringId}:{UpgradeReqKeyAttrName}";

        MAttribute<UpgradeItemRequirementKey> UpgradeRequiresItemFromCategoryKeyAttribute =>
            Attribute(
                getter: _ =>
                    MAttribute<UpgradeItemRequirementKey>.Store.GetOrInit(
                        UpgradeReqStoreKey,
                        UpgradeItemRequirementKey.None
                    ),
                setter: (_, key) =>
                {
                    MAttribute<UpgradeItemRequirementKey>.Store.Set(UpgradeReqStoreKey, key);
                    ApplyUpgradeRequiresItemFromCategoryToBase(key);
                },
                persistent: true,
                priority: AttributePriority.Medium,
                name: UpgradeReqKeyAttrName
            );

        private UpgradeItemRequirementKey UpgradeRequiresItemFromCategoryKey
        {
            get => UpgradeRequiresItemFromCategoryKeyAttribute.Get();
            set => UpgradeRequiresItemFromCategoryKeyAttribute.Set(value);
        }

        public ItemCategory UpgradeRequiresItemFromCategory
        {
            get => ResolveUpgradeItemCategory(UpgradeRequiresItemFromCategoryKey);
            set => UpgradeRequiresItemFromCategoryKey = ToUpgradeKey(value);
        }

        private static UpgradeItemRequirementKey ToUpgradeKey(ItemCategory category)
        {
            if (category == null)
                return UpgradeItemRequirementKey.None;

            // Keep it intentionally small and stable.
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

        private static ItemCategory ResolveUpgradeItemCategory(UpgradeItemRequirementKey key)
        {
            return key switch
            {
                UpgradeItemRequirementKey.Horse => DefaultItemCategories.Horse,
                UpgradeItemRequirementKey.WarHorse => DefaultItemCategories.WarHorse,
                _ => null,
            };
        }

        private void ApplyUpgradeRequiresItemFromCategoryToBase(UpgradeItemRequirementKey key)
        {
            var category = ResolveUpgradeItemCategory(key);

            try
            {
                if (Reflection.HasProperty(Base, UpgradeReqBaseName))
                    Reflection.SetPropertyValue(Base, UpgradeReqBaseName, category);
                else if (Reflection.HasField(Base, UpgradeReqBaseName))
                    Reflection.SetFieldValue(Base, UpgradeReqBaseName, category);
            }
            catch
            {
                // If TW changes internals, we just don't apply.
            }
        }

        private UpgradeItemRequirementKey GetBestBattleMountKey()
        {
            var equipments = Equipments;
            if (equipments == null || equipments.Count == 0)
                return UpgradeItemRequirementKey.None;

            var best = UpgradeItemRequirementKey.None;

            for (int i = 0; i < equipments.Count; i++)
            {
                var eq = equipments[i];
                if (eq?.IsCivilian != false)
                    continue; // Ignore civilian sets

                var horse = eq.Get(EquipmentIndex.Horse);
                var key = ToUpgradeKey(horse?.Category);

                if ((int)key > (int)best)
                    best = key;

                // Early out: can't beat WarHorse
                if (best == UpgradeItemRequirementKey.WarHorse)
                    break;
            }

            return best;
        }

        private List<WCharacter> GetItemRequirementSources()
        {
            // Retinues: treat conversion sources as "upgrade sources" for requirement computation.
            // Non-retinues: use the real tree sources.
            return IsRetinue ? ConversionSources : UpgradeSources;
        }

        /// <summary>
        /// Recomputes this troop's requirement from its sources. Optionally also refreshes its targets.
        /// Rule: require the mount category only if none of the sources already have that category (or better).
        /// </summary>
        public void UpdateItemRequirements(bool updateTargets = true)
        {
            var sources = GetItemRequirementSources();
            UpdateItemRequirementsFromSources(sources, updateTargets);
        }

        public void UpdateItemRequirementsFromSources(
            List<WCharacter> sources,
            bool updateTargets = true
        )
        {
            var targetKey = GetBestBattleMountKey();

            if (targetKey == UpgradeItemRequirementKey.None)
            {
                if (UpgradeRequiresItemFromCategoryKey != UpgradeItemRequirementKey.None)
                    UpgradeRequiresItemFromCategoryKey = UpgradeItemRequirementKey.None;
                return;
            }

            if (sources == null || sources.Count == 0)
            {
                if (UpgradeRequiresItemFromCategoryKey != UpgradeItemRequirementKey.None)
                    UpgradeRequiresItemFromCategoryKey = UpgradeItemRequirementKey.None;
                return;
            }

            var require = true;

            for (int i = 0; i < sources.Count; i++)
            {
                var s = sources[i];
                if (s == null)
                    continue;

                var sk = s.GetBestBattleMountKey();
                if ((int)sk >= (int)targetKey)
                {
                    require = false;
                    break;
                }
            }

            var newKey = require ? targetKey : UpgradeItemRequirementKey.None;

            if (UpgradeRequiresItemFromCategoryKey != newKey)
                UpgradeRequiresItemFromCategoryKey = newKey;

            if (!updateTargets)
                return;

            // Recursive propagation, but safety against accidental loops.
            var visited = new HashSet<string>(System.StringComparer.Ordinal);
            UpdateItemRequirementsForTargetsRecursive(visited);
        }

        private void UpdateItemRequirementsForTargetsRecursive(HashSet<string> visited)
        {
            var id = StringId;
            if (string.IsNullOrEmpty(id) || !visited.Add(id))
                return;

            var targets = UpgradeTargets;
            if (targets == null || targets.Count == 0)
                return;

            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t?.Base == null)
                    continue;

                // Let the target compute from its own sources.
                t.UpdateItemRequirements(updateTargets: false);

                // And keep going upward (tier chains) safely.
                t.UpdateItemRequirementsForTargetsRecursive(visited);
            }
        }
    }
}
