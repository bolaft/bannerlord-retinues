using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Domain.Characters.Helpers
{
    [SafeClass]
    public static class CharacterCloner
    {
        /// <summary>
        /// Fired when CharacterCloner unlocks items as part of troop creation.
        /// Listener is in Game layer (UnlockNotifierBehavior).
        /// </summary>
        public static event Action<IReadOnlyList<WItem>> ItemsUnlockedByCloner;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WCharacter CloneVanilla(
            WCharacter template,
            bool skills = true,
            bool equipments = true,
            WCharacter intoStub = null,
            bool unlockItems = true,
            bool notifyUnlocks = true,
            List<WItem> unlockSink = null
        )
        {
            if (template == null)
                return null;

            if (!template.IsVanilla)
                Log.Debug($"CloneVanilla called with non-vanilla troop '{template.StringId}'.");

            var clone = template.Clone(skills: skills, equipments: false, intoStub: intoStub);
            if (clone == null)
                return null;

            clone.UpgradeTargets = [];

            if (!equipments)
            {
                clone.EquipmentRoster.Reset();
                return clone;
            }

            ApplyStarterEquipments(
                template: template,
                clone: clone,
                cultureContext: template.Culture,
                createCivilianSet: true
            );

            if (unlockItems)
            {
                var newly = UnlockAllItems(clone);
                CollectUnlocks(newly, notifyUnlocks, unlockSink);
            }

            return clone;
        }

        public static WCharacter CloneTreeFromRoot(
            WCharacter rootTemplate,
            bool skills = true,
            bool equipments = true,
            bool notifyUnlocks = true,
            List<WItem> unlockSink = null
        )
        {
            if (rootTemplate == null)
                return null;

            var root = rootTemplate.Root ?? rootTemplate;

            if (!root.IsVanilla)
                Log.Warn($"CloneTreeFromRoot expects vanilla root; got '{root.StringId}'.");

            var templates = root.RootTree;
            if (templates == null || templates.Count == 0)
                templates = [root];

            var created = new List<WCharacter>(templates.Count);
            var map = new Dictionary<string, WCharacter>(templates.Count);

            try
            {
                // 1) Clone every troop. IMPORTANT: do not unlock here.
                for (int i = 0; i < templates.Count; i++)
                {
                    var t = templates[i];
                    if (t?.Base == null)
                        continue;

                    var c =
                        CloneVanilla(
                            t,
                            skills: skills,
                            equipments: equipments,
                            intoStub: null,
                            unlockItems: false,
                            notifyUnlocks: false
                        )
                        ?? throw new InvalidOperationException(
                            "No free stub available for tree clone."
                        );

                    created.Add(c);
                    map[t.StringId] = c;
                }

                // 2) Re-wire upgrade targets to point to the cloned nodes
                for (int i = 0; i < templates.Count; i++)
                {
                    var src = templates[i];
                    if (src == null)
                        continue;

                    if (!map.TryGetValue(src.StringId, out var clonedSrc))
                        continue;

                    var targets = src.UpgradeTargets ?? [];
                    if (targets.Count == 0)
                    {
                        clonedSrc.UpgradeTargets = [];
                        continue;
                    }

                    var clonedTargets = new List<WCharacter>();

                    for (int j = 0; j < targets.Count; j++)
                    {
                        var tgt = targets[j];
                        if (tgt == null)
                            continue;

                        if (map.TryGetValue(tgt.StringId, out var clonedTgt))
                            clonedTargets.Add(clonedTgt);
                    }

                    clonedSrc.UpgradeTargets = clonedTargets;
                }

                // 3) Unlock items for the whole tree (single notification).
                if (equipments)
                {
                    var allNew = new List<WItem>(64);
                    var seen = new HashSet<string>(StringComparer.Ordinal);

                    for (int i = 0; i < created.Count; i++)
                    {
                        var c = created[i];
                        if (c?.Base == null)
                            continue;

                        var newly = UnlockAllItems(c);
                        if (newly == null || newly.Count == 0)
                            continue;

                        for (int k = 0; k < newly.Count; k++)
                        {
                            var it = newly[k];
                            var id = it?.StringId;
                            if (string.IsNullOrEmpty(id))
                                continue;

                            if (seen.Add(id))
                                allNew.Add(it);
                        }
                    }

                    CollectUnlocks(allNew, notifyUnlocks, unlockSink);
                }

                map.TryGetValue(root.StringId, out var clonedRoot);
                return clonedRoot;
            }
            catch (Exception ex)
            {
                Log.Error($"CloneTreeFromRoot failed: {ex}");

                CleanupCreated(created);
                return null;
            }
        }

        private static void CollectUnlocks(
            List<WItem> newlyUnlocked,
            bool notifyUnlocks,
            List<WItem> unlockSink
        )
        {
            if (newlyUnlocked == null || newlyUnlocked.Count == 0)
                return;

            if (unlockSink != null)
                unlockSink.AddRange(newlyUnlocked);

            if (!notifyUnlocks)
                return;

            ItemsUnlockedByCloner?.Invoke(newlyUnlocked);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Generic Builder Entry                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class TroopBuildRequest
        {
            public string Name { get; set; }
            public WCulture CultureContext { get; set; }
            public bool CopySkills { get; set; } = true;
            public bool CreateCivilianSet { get; set; } = true;

            public bool UnlockItems { get; set; } = true;

            /// <summary>
            /// When true, CharacterCloner will emit an unlock notification event if it unlocked new items.
            /// </summary>
            public bool NotifyUnlocks { get; set; } = true;

            public bool UnhideInEncyclopedia { get; set; } = true;
        }

        public static WCharacter BuildFromTemplate(WCharacter template, TroopBuildRequest req)
        {
            if (template?.Base == null)
                return null;

            req ??= new TroopBuildRequest();

            var troop = template.Clone(skills: req.CopySkills, equipments: false, intoStub: null);
            if (troop?.Base == null)
                return null;

            troop.UpgradeTargets = [];
            troop.Name = req.Name ?? string.Empty;

            ApplyStarterEquipments(
                template: template,
                clone: troop,
                cultureContext: req.CultureContext ?? template.Culture,
                createCivilianSet: req.CreateCivilianSet
            );

            if (req.UnlockItems)
            {
                var newly = UnlockAllItems(troop);
                CollectUnlocks(newly, req.NotifyUnlocks, unlockSink: null);
            }

            if (req.UnhideInEncyclopedia)
                troop.HiddenInEncyclopedia = false;

            return troop;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Notifier                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static List<WItem> UnlockAllItems(WCharacter troop)
        {
            var list = new List<WItem>(16);

            foreach (WItem item in troop.EquipmentRoster.Items)
            {
                if (item == null)
                    continue;

                if (item.IsUnlocked)
                    continue;

                item.Unlock();
                list.Add(item);
            }

            return list;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Equipment Strategy                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ApplyStarterEquipments(
            WCharacter template,
            WCharacter clone,
            WCulture cultureContext,
            bool createCivilianSet
        )
        {
            if (template == null || clone == null)
                return;

            switch (Settings.StarterEquipment.Value)
            {
                case Settings.EquipmentMode.AllSets:
                    clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.All);
                    return;

                case Settings.EquipmentMode.SingleSet:
                    clone.EquipmentRoster.Copy(
                        template.EquipmentRoster,
                        EquipmentCopyMode.FirstOfEach
                    );
                    return;

                case Settings.EquipmentMode.EmptySet:
                    clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.Reset);
                    return;

                case Settings.EquipmentMode.RandomSet:
                default:
                    break;
            }

            var culture = cultureContext ?? template.Culture;

            int maxTier = MBMath.ClampInt(clone.Tier, 0, 6);
            int minTier = 0;

            var noItemBattle = new Dictionary<EquipmentIndex, float>
            {
                [EquipmentIndex.Head] = 25f,
                [EquipmentIndex.Gloves] = 25f,
                [EquipmentIndex.Horse] = 50f,
            };

            Dictionary<EquipmentIndex, float> noItemCivil = new()
            {
                [EquipmentIndex.Head] = 50f,
                [EquipmentIndex.Gloves] = 50f,
                [EquipmentIndex.Horse] = 100f,
            };

            var battle = RandomEquipmentHelper.CreateRandomEquipment(
                owner: clone,
                civilian: false,
                minTier: minTier,
                maxTier: maxTier,
                acceptableCultures: culture != null ? [culture] : null,
                acceptNeutralCulture: true,
                noItemChanceBySlotPercent: noItemBattle,
                requireSkillForItem: true,
                itemFilter: null,
                enforceLimits: true
            );

            MEquipment civil = null;
            if (createCivilianSet)
            {
                civil = RandomEquipmentHelper.CreateRandomEquipment(
                    owner: clone,
                    civilian: true,
                    minTier: minTier,
                    maxTier: maxTier,
                    acceptableCultures: culture != null ? [culture] : null,
                    acceptNeutralCulture: true,
                    noItemChanceBySlotPercent: noItemCivil,
                    requireSkillForItem: true,
                    itemFilter: null,
                    enforceLimits: true
                );
            }

            clone.EquipmentRoster.Equipments = createCivilianSet ? [battle, civil] : [battle];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cleanup                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void CleanupCreated(List<WCharacter> created)
        {
            if (created == null || created.Count == 0)
                return;

            for (int i = 0; i < created.Count; i++)
            {
                var c = created[i];
                if (c == null)
                    continue;

                try
                {
                    c.Remove();
                }
                catch { }
            }
        }
    }
}
