using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Framework.Runtime;
using Retinues.Utilities;

namespace Retinues.Campaign
{
    /// <summary>
    /// Centralized troop creation and cloning helpers.
    /// Applies starter equipment policy from configuration.
    /// </summary>
    [SafeClass]
    public static class TroopBuilder
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clones a vanilla troop into a custom stub.
        /// Equipment initialization is driven by Settings.StarterEquipmentOption.
        /// </summary>
        public static WCharacter CloneVanilla(
            WCharacter template,
            bool skills = true,
            bool equipments = true,
            WCharacter intoStub = null
        )
        {
            if (template == null)
                return null;

            if (!template.IsVanilla)
                Log.Info($"CloneVanilla called with non-vanilla troop '{template.StringId}'.");

            // Clone with equipment copy disabled here; we apply the configured strategy below.
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

            ApplyStarterEquipments(template, clone);
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
            {
                Log.Warn($"CloneTreeFromRoot expects vanilla root; got '{root.StringId}'.");
            }

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

                    var c = CloneVanilla(t, skills: skills, equipments: equipments);
                    if (c == null)
                        throw new System.InvalidOperationException(
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
            catch (System.Exception ex)
            {
                Log.Error($"CloneTreeFromRoot failed: {ex}");

                // Best-effort rollback: return stubs to the pool.
                CleanupCreated(created);
                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Equipment Strategy                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ApplyStarterEquipments(WCharacter template, WCharacter clone)
        {
            if (template == null || clone == null)
                return;

            switch (Settings.StarterEquipment.Value)
            {
                case Settings.EquipmentMode.AllSets:
                    clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.All);
                    break;

                case Settings.EquipmentMode.SingleSet:
                    clone.EquipmentRoster.Copy(
                        template.EquipmentRoster,
                        EquipmentCopyMode.FirstOfEach
                    );
                    break;

                case Settings.EquipmentMode.RandomSet:
                    // Not implemented yet; behave like EmptySet for now.
                    clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.Reset);
                    break;

                case Settings.EquipmentMode.EmptySet:
                default:
                    clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.Reset);
                    break;
            }
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
