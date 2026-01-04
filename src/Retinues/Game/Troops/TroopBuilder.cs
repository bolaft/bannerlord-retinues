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

namespace Retinues.Game.Troops
{
    /// <summary>
    /// Centralized troop creation and cloning helpers.
    /// Starter equipment is always applied from Settings.StarterEquipment.
    /// </summary>
    [SafeClass]
    public static class TroopBuilder
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clones a vanilla troop into a custom stub.
        /// Equipment initialization is driven by Settings.StarterEquipment.
        /// </summary>
        public static WCharacter CloneVanilla(
            WCharacter template,
            bool skills = true,
            bool equipments = true,
            WCharacter intoStub = null,
            bool unlockItems = true
        )
        {
            if (template == null)
                return null;

            if (!template.IsVanilla)
                Log.Info($"CloneVanilla called with non-vanilla troop '{template.StringId}'.");

            // Clone with equipment copy disabled here; we apply configured strategy below.
            var clone = template.Clone(skills: skills, equipments: false, intoStub: intoStub);
            if (clone == null)
                return null;

            // Ensure no upgrade links by default (tree-building re-wires them explicitly).
            clone.UpgradeTargets = [];

            if (!equipments)
            {
                clone.EquipmentRoster.Reset();
                return clone;
            }

            // Apply configured starter equipment strategy (Settings-driven).
            // Equipment creation strategies never depend on unlock status.
            ApplyStarterEquipments(
                template: template,
                clone: clone,
                cultureContext: template.Culture,
                createCivilianSet: true
            );

            // Unlock all items if requested.
            if (unlockItems)
            {
                foreach (WItem item in clone.EquipmentRoster.Items)
                    item.Unlock();
            }

            return clone;
        }

        /// <summary>
        /// Clones the full upgrade tree starting from a vanilla root troop.
        /// Returns a result containing the cloned root and all cloned troops.
        /// </summary>
        public static WCharacter CloneTreeFromRoot(
            WCharacter rootTemplate,
            bool skills = true,
            bool equipments = true
        )
        {
            if (rootTemplate == null)
                return null;

            // Be forgiving: if caller passes a non-root troop, build from its root anyway.
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
                // 1) Clone every troop with configured equipment policy
                for (int i = 0; i < templates.Count; i++)
                {
                    var t = templates[i];
                    if (t?.Base == null)
                        continue;

                    var c =
                        CloneVanilla(t, skills: skills, equipments: equipments)
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

                map.TryGetValue(root.StringId, out var clonedRoot);
                return clonedRoot;
            }
            catch (Exception ex)
            {
                Log.Error($"CloneTreeFromRoot failed: {ex}");

                // Best-effort rollback: return stubs to the pool.
                CleanupCreated(created);
                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Generic builder entry                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class TroopBuildRequest
        {
            public string Name { get; set; }
            public WCulture CultureContext { get; set; }
            public bool CopySkills { get; set; } = true;
            public bool CreateCivilianSet { get; set; } = true;

            /// <summary>
            /// When true, every item assigned to the troop is immediately unlocked.
            /// </summary>
            public bool UnlockItems { get; set; } = true;

            /// <summary>
            /// If true, the created troop is unhidden in the encyclopedia.
            /// </summary>
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
                foreach (WItem item in troop.EquipmentRoster.Items)
                    item.Unlock();
            }

            // Make sure the retinue is visible in the encyclopedia.
            if (req.UnhideInEncyclopedia)
                troop.HiddenInEncyclopedia = false;

            return troop;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Equipment Strategy                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Applies starter equipment policy driven by Settings.StarterEquipment.
        /// Equipment creation strategies never depend on unlock status.
        /// </summary>
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

            /* ━━━━━━━━ Random ━━━━━━━━ */

            // Culture
            var culture = cultureContext ?? template.Culture;

            // Max tier: never exceed troop tier.
            int maxTier = MBMath.ClampInt(clone.Tier, 0, 6);

            // Minimum tier: fixed value.
            int minTier = 0;

            // Battle: empty slot chances.
            var noItemBattle = new Dictionary<EquipmentIndex, float>
            {
                [EquipmentIndex.Head] = 25f,
                [EquipmentIndex.Gloves] = 25f,
                [EquipmentIndex.Horse] = 50f,
                // Note: no HorseHarness entry on purpose.
                // RandomHelper enforces:
                // - if horse present => harness attempted 100%
                // - if no horse => harness never present
            };

            // Civilian: empty slot chances.
            Dictionary<EquipmentIndex, float> noItemCivil = new()
            {
                [EquipmentIndex.Head] = 50f,
                [EquipmentIndex.Gloves] = 50f,
                [EquipmentIndex.Horse] = 100f, // No horses for civilian sets
            };

            // IMPORTANT: No unlocked-only filtering here.
            var battle = RandomHelper.CreateRandomEquipment(
                owner: clone,
                civilian: false,
                minTier: minTier,
                maxTier: maxTier,
                acceptableCultures: culture != null ? [culture] : null,
                acceptNeutralCulture: true,
                noItemChanceBySlotPercent: noItemBattle,
                requireSkillForItem: true,
                itemFilter: null
            );

            MEquipment civil = null;
            if (createCivilianSet)
            {
                civil = RandomHelper.CreateRandomEquipment(
                    owner: clone,
                    civilian: true,
                    minTier: minTier,
                    maxTier: maxTier,
                    acceptableCultures: culture != null ? [culture] : null,
                    acceptNeutralCulture: true,
                    noItemChanceBySlotPercent: noItemCivil,
                    requireSkillForItem: true,
                    itemFilter: null
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
