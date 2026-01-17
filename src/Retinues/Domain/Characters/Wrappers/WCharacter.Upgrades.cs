using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Services.Trees;
using Retinues.Framework.Model.Attributes;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Upgrade Sources                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<WCharacter> UpgradeSources => CharacterTreeCache.GetUpgradeSources(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Upgrade Targets                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // We keep the saved ids even if the base objects cannot be resolved yet.
        // This prevents "wiping" CharacterObject.UpgradeTargets during load.
        private List<string> _upgradeTargetIdsPersisted = [];

        /// <summary>
        /// The upgrade target ids for this troop.
        /// </summary>
        MAttribute<List<string>> UpgradeTargetsAttribute =>
            Attribute<List<string>>(
                getter: _ =>
                {
                    if (_upgradeTargetIdsPersisted != null && _upgradeTargetIdsPersisted.Count > 0)
                        return [.. _upgradeTargetIdsPersisted];

                    var arr = Base.UpgradeTargets;
                    if (arr == null || arr.Length == 0)
                        return [];

                    return
                    [
                        .. arr.Select(t => t?.StringId).Where(id => !string.IsNullOrWhiteSpace(id)),
                    ];
                },
                setter: (_, ids) =>
                {
                    _upgradeTargetIdsPersisted =
                        ids == null
                            ? []
                            :
                            [
                                .. ids.Select(s => s?.Trim())
                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                    .Distinct(),
                            ];

                    TryApplyUpgradeTargetIds();

                    CharacterTreeCache.MarkDirty();
                }
            );

        /// <summary>
        /// Tries to apply the persisted upgrade target ids to the base CharacterObject.
        /// </summary>
        private void TryApplyUpgradeTargetIds()
        {
            var mgr = MBObjectManager.Instance;
            if (mgr == null)
                return;

            if (_upgradeTargetIdsPersisted == null || _upgradeTargetIdsPersisted.Count == 0)
            {
                Reflection.SetPropertyValue(Base, "UpgradeTargets", new CharacterObject[0]);
                return;
            }

            var resolved = new List<CharacterObject>();

            for (int i = 0; i < _upgradeTargetIdsPersisted.Count; i++)
            {
                var id = _upgradeTargetIdsPersisted[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var target = mgr.GetObject<CharacterObject>(id);
                if (target != null)
                    resolved.Add(target);
                else
                    Log.Debug($"Could not resolve upgrade target '{id}' for '{StringId}' yet.");
            }

            Reflection.SetPropertyValue(Base, "UpgradeTargets", resolved.ToArray());
        }

        /// <summary>
        /// The upgrade targets for this troop.
        /// </summary>
        public List<WCharacter> UpgradeTargets
        {
            get
            {
                if (
                    (_upgradeTargetIdsPersisted?.Count ?? 0) > 0
                    && (Base.UpgradeTargets == null || Base.UpgradeTargets.Length == 0)
                )
                {
                    TryApplyUpgradeTargetIds();
                }

                var ids = UpgradeTargetsAttribute.Get();
                if (ids == null || ids.Count == 0)
                    return [];

                var list = new List<WCharacter>(ids.Count);
                for (int i = 0; i < ids.Count; i++)
                {
                    var w = Get(ids[i]);
                    if (w != null)
                        list.Add(w);
                }

                return list;
            }
            set
            {
                var ids =
                    value == null
                        ? []
                        : value
                            .Select(w => w?.StringId)
                            .Where(id => !string.IsNullOrWhiteSpace(id))
                            .Select(id => id.Trim())
                            .ToList();

                UpgradeTargetsAttribute.Set(ids);
            }
        }

        /// <summary>
        /// Adds an upgrade target to this troop.
        /// </summary>
        public bool AddUpgradeTarget(WCharacter target)
        {
            if (target == null)
                return false;

            if (target == this)
                return false;

            var ids = UpgradeTargetsAttribute.Get() ?? [];
            if (ids.Contains(target.StringId))
                return false;

            ids.Add(target.StringId);
            UpgradeTargetsAttribute.Set(ids);
            return true;
        }

        /// <summary>
        /// Removes an upgrade target from this troop.
        /// </summary>
        public bool RemoveUpgradeTarget(WCharacter target)
        {
            if (target == null)
                return false;

            var ids = UpgradeTargetsAttribute.Get() ?? [];
            if (ids.Count == 0)
                return false;

            var removed = false;
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                if (ids[i] == target.StringId)
                {
                    ids.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
                UpgradeTargetsAttribute.Set(ids);

            return removed;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Upgrade Requirements                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

                    UpgradeRequirementHelper.ApplyRequirementToBase(Base, UpgradeReqBaseName, key);
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
            get =>
                UpgradeRequirementHelper.ResolveUpgradeItemCategory(
                    UpgradeRequiresItemFromCategoryKey
                );
            set =>
                UpgradeRequiresItemFromCategoryKey = UpgradeRequirementHelper.ToUpgradeKey(value);
        }

        /// <summary>
        /// Recomputes this troop's requirement from its sources. Optionally also refreshes its targets.
        /// Rule: require the mount category only if none of the sources already have that category (or better).
        /// </summary>
        public void UpdateItemRequirements(bool updateTargets = true)
        {
            var sources = UpgradeRequirementHelper.GetItemRequirementSources(this);
            UpdateItemRequirementsFromSources(sources, updateTargets);
        }

        public void UpdateItemRequirementsFromSources(
            List<WCharacter> sources,
            bool updateTargets = true
        )
        {
            var newKey = UpgradeRequirementHelper.ComputeRequiredKeyFromSources(this, sources);

            if (UpgradeRequiresItemFromCategoryKey != newKey)
                UpgradeRequiresItemFromCategoryKey = newKey;

            if (!updateTargets)
                return;

            var visited = new HashSet<string>(System.StringComparer.Ordinal);

            UpgradeRequirementHelper.UpdateTargetsRecursive(
                root: this,
                visited: visited,
                updateTargetNoRecurse: t => t.UpdateItemRequirements(updateTargets: false)
            );
        }
    }
}
