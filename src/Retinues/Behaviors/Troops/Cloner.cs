using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Services.Random;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Settings;
using Retinues.Utilities;

namespace Retinues.Behaviors.Troops
{
    [SafeClass]
    public static partial class Cloner
    {
        /// <summary>
        /// Fired when CharacterCloner unlocks items as part of troop creation.
        /// Listener is in Game layer (UnlockNotifierBehavior).
        /// </summary>
        public static event Action<IReadOnlyList<WItem>> ItemsUnlockedByCloner;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clone a vanilla troop into a free custom stub (default),
        /// or into the provided stub if specified.
        /// </summary>
        public static WCharacter CloneTroop(
            WCharacter template,
            bool skills = true,
            bool equipments = true,
            WCharacter intoStub = null,
            bool unlockItems = true,
            bool notifyUnlocks = true,
            List<WItem> unlockSink = null,
            RandomEquipmentReuseContext equipmentReuseContext = null
        )
        {
            if (template == null)
                return null;

            var clone = CharacterCloner.Clone(
                template,
                skills: skills,
                equipments: false,
                stub: intoStub
            );
            if (clone == null)
                return null;

            // Important: FillFrom copied origin UpgradeTargets to the stub.
            // Ensure the BASE object does not still point to vanilla nodes.
            clone.UpgradeTargets = [];
            CharacterCloner.SetBaseUpgradeTargets(clone, []);

            if (skills)
                EnforceSkillLimits(clone);

            if (!equipments)
            {
                clone.EquipmentRoster.Reset();
                return clone;
            }

            ApplyStarterEquipments(
                template: template,
                clone: clone,
                cultureContext: template.Culture,
                createCivilianSet: true,
                reuseContext: equipmentReuseContext
            );

            if (unlockItems)
            {
                var newly = UnlockAllItems(clone);
                CollectUnlocks(newly, notifyUnlocks, unlockSink);
            }

            return clone;
        }

        /// <summary>
        /// Clone an entire upgrade tree from the specified root template.
        /// </summary>
        public static WCharacter CloneTreeFromRoot(
            WCharacter rootTemplate,
            bool lean = false,
            bool skills = true,
            bool equipments = true,
            bool notifyUnlocks = true,
            List<WItem> unlockSink = null
        )
        {
            if (rootTemplate == null)
                return null;

            // IMPORTANT:
            // Do NOT use rootTemplate.Root here.
            // Root is computed from upgrade SOURCES, so Culture.Villager often becomes the component root.
            var root = rootTemplate;

            // Tree is subtree: self + descendants only (follows UpgradeTargets only).
            var templates = root.Tree;

            if (templates == null || templates.Count == 0)
                templates = [root];

            if (lean)
                templates = BuildLeanTemplateList(root, templates);

            var created = new List<WCharacter>(templates.Count);
            var map = new Dictionary<string, WCharacter>(templates.Count);

            RandomEquipmentReuseContext reuseContext = null;
            if (
                equipments
                && Configuration.StarterEquipment.Value == Configuration.EquipmentMode.RandomSet
            )
                reuseContext = new RandomEquipmentReuseContext();

            try
            {
                // 1) Clone every troop. Do not unlock here.
                for (int i = 0; i < templates.Count; i++)
                {
                    var t = templates[i];
                    if (t?.Base == null)
                        continue;

                    var c =
                        CloneTroop(
                            t,
                            skills: skills,
                            equipments: equipments,
                            intoStub: null,
                            unlockItems: false,
                            notifyUnlocks: false,
                            unlockSink: null,
                            equipmentReuseContext: reuseContext
                        )
                        ?? throw new InvalidOperationException(
                            "No free stub available for tree clone."
                        );

                    created.Add(c);
                    map[t.StringId] = c;
                }

                // 2) Re-wire upgrade targets to point to cloned nodes (and only kept nodes).
                for (int i = 0; i < templates.Count; i++)
                {
                    var src = templates[i];
                    if (src?.Base == null)
                        continue;

                    if (!map.TryGetValue(src.StringId, out var clonedSrc))
                        continue;

                    var targets = src.UpgradeTargets ?? [];
                    if (targets.Count == 0)
                    {
                        clonedSrc.UpgradeTargets = [];
                        CharacterCloner.SetBaseUpgradeTargets(clonedSrc, []);
                        continue;
                    }

                    var clonedTargets = new List<WCharacter>();

                    for (int j = 0; j < targets.Count; j++)
                    {
                        var tgt = targets[j];
                        if (tgt?.Base == null)
                            continue;

                        if (map.TryGetValue(tgt.StringId, out var clonedTgt))
                            clonedTargets.Add(clonedTgt);
                    }

                    clonedSrc.UpgradeTargets = clonedTargets;
                    CharacterCloner.SetBaseUpgradeTargets(clonedSrc, clonedTargets);
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

        /// <summary>
        /// Handle unlock notifications and sink collection.
        /// </summary>
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

        /// <summary>
        /// Request parameters for troop building from template.
        /// </summary>
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

        /// <summary>
        /// Build a troop from a template according to the provided request.
        /// </summary>
        public static WCharacter BuildFromTemplate(WCharacter template, TroopBuildRequest req)
        {
            if (template?.Base == null)
                return null;

            req ??= new TroopBuildRequest();

            var troop = CharacterCloner.Clone(
                template,
                skills: req.CopySkills,
                equipments: false,
                stub: null
            );
            if (troop?.Base == null)
                return null;

            troop.UpgradeTargets = [];
            CharacterCloner.SetBaseUpgradeTargets(troop, []);

            troop.Name = req.Name ?? string.Empty;

            if (req.CopySkills)
                EnforceSkillLimits(troop);

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
