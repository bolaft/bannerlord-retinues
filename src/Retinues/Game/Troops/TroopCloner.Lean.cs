using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Game.Troops
{
    public static partial class TroopCloner
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Lean Trees                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private enum LeanFormation
        {
            Infantry,
            Ranged,
            Cavalry,
            HorseArcher,
        }

        private static LeanFormation ClassifyFormation(WCharacter wc)
        {
            if (wc == null)
                return LeanFormation.Infantry;

            var f = wc.FormationClass;

            if (f == FormationClass.Cavalry)
                return LeanFormation.Cavalry;

            if (f == FormationClass.HorseArcher)
                return LeanFormation.HorseArcher;

            if (f == FormationClass.Ranged)
                return LeanFormation.Ranged;

            return LeanFormation.Infantry;
        }

        private static List<WCharacter> BuildLeanTemplateList(
            WCharacter root,
            IReadOnlyList<WCharacter> fullTemplates
        )
        {
            if (root?.Base == null || fullTemplates == null || fullTemplates.Count == 0)
                return fullTemplates as List<WCharacter> ?? [];

            var depth = BuildDepthMap(root);

            var byTier = new Dictionary<int, List<WCharacter>>();

            for (int i = 0; i < fullTemplates.Count; i++)
            {
                var t = fullTemplates[i];
                if (t?.Base == null)
                    continue;

                var tier = t.Tier;

                if (!byTier.TryGetValue(tier, out var list))
                {
                    list = [];
                    byTier[tier] = list;
                }

                list.Add(t);
            }

            var keep = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(root.StringId))
                keep.Add(root.StringId);

            foreach (var kvp in byTier)
            {
                var tierList = kvp.Value;
                if (tierList == null || tierList.Count == 0)
                    continue;

                PickBestForFormation(tierList, depth, LeanFormation.Infantry, keep);
                PickBestForFormation(tierList, depth, LeanFormation.Ranged, keep);
                PickBestForFormation(tierList, depth, LeanFormation.Cavalry, keep);
                PickBestForFormation(tierList, depth, LeanFormation.HorseArcher, keep);
            }

            // Parent map from UpgradeTargets (template side).
            var parents = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            for (int i = 0; i < fullTemplates.Count; i++)
            {
                var src = fullTemplates[i];
                if (src?.Base == null)
                    continue;

                var srcId = src.StringId;
                if (string.IsNullOrEmpty(srcId))
                    continue;

                var targets = src.UpgradeTargets ?? [];
                for (int j = 0; j < targets.Count; j++)
                {
                    var tgtId = targets[j]?.StringId;
                    if (string.IsNullOrEmpty(tgtId))
                        continue;

                    if (!parents.TryGetValue(tgtId, out var list))
                    {
                        list = [];
                        parents[tgtId] = list;
                    }

                    list.Add(srcId);
                }
            }

            // Add ancestors.
            var stack = new Stack<string>(keep.Count);
            foreach (var id in keep)
                stack.Push(id);

            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (string.IsNullOrEmpty(id))
                    continue;

                if (!parents.TryGetValue(id, out var ps) || ps == null || ps.Count == 0)
                    continue;

                for (int i = 0; i < ps.Count; i++)
                {
                    var p = ps[i];
                    if (string.IsNullOrEmpty(p))
                        continue;

                    if (keep.Add(p))
                        stack.Push(p);
                }
            }

            // Preserve original order.
            var result = new List<WCharacter>(keep.Count);

            for (int i = 0; i < fullTemplates.Count; i++)
            {
                var t = fullTemplates[i];
                var id = t?.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (keep.Contains(id))
                    result.Add(t);
            }

            // Ensure root first.
            if (!string.IsNullOrEmpty(root.StringId))
            {
                int rootIndex = -1;
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i]?.StringId == root.StringId)
                    {
                        rootIndex = i;
                        break;
                    }
                }

                if (rootIndex > 0)
                {
                    var r = result[rootIndex];
                    result.RemoveAt(rootIndex);
                    result.Insert(0, r);
                }
            }

            return result;
        }

        private static void PickBestForFormation(
            List<WCharacter> tierList,
            Dictionary<string, int> depth,
            LeanFormation formation,
            HashSet<string> keep
        )
        {
            WCharacter best = null;
            int bestDepth = int.MaxValue;

            for (int i = 0; i < tierList.Count; i++)
            {
                var t = tierList[i];
                if (t?.Base == null)
                    continue;

                if (ClassifyFormation(t) != formation)
                    continue;

                var id = t.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                depth.TryGetValue(id, out var d);

                if (best == null || d < bestDepth)
                {
                    best = t;
                    bestDepth = d;
                }
            }

            if (best?.Base == null)
                return;

            keep.Add(best.StringId);
        }

        private static Dictionary<string, int> BuildDepthMap(WCharacter root)
        {
            var depth = new Dictionary<string, int>(StringComparer.Ordinal);

            if (root?.Base == null || string.IsNullOrEmpty(root.StringId))
                return depth;

            depth[root.StringId] = 0;

            var q = new Queue<WCharacter>();
            q.Enqueue(root);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur?.Base == null)
                    continue;

                if (!depth.TryGetValue(cur.StringId, out var curDepth))
                    curDepth = 0;

                var targets = cur.UpgradeTargets ?? [];
                for (int i = 0; i < targets.Count; i++)
                {
                    var t = targets[i];
                    if (t?.Base == null)
                        continue;

                    var id = t.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (depth.ContainsKey(id))
                        continue;

                    depth[id] = curDepth + 1;
                    q.Enqueue(t);
                }
            }

            return depth;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Lean Tree Naming                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ApplyLeanFactionNames(
            WCharacter root,
            string factionName,
            bool nobleLine = false
        )
        {
            if (root?.Base == null)
                return;

            factionName = (factionName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(factionName))
                return;

            // Enumerate cloned subtree (do NOT rely on root.Tree here).
            var tree = EnumerateSubtree(root);

            // Root
            root.Name = nobleLine
                ? $"{factionName} {L.S("troop_noble", "Noble")} {L.S("troop_recruit", "Recruit")}"
                : $"{factionName} {L.S("troop_recruit", "Recruit")}";

            // Prepare nodes excluding root, then sort by tier/formation/id for stability.
            var nodes = new List<WCharacter>(tree.Count);

            for (int i = 0; i < tree.Count; i++)
            {
                var n = tree[i];
                if (n?.Base == null)
                    continue;

                if (n.StringId == root.StringId)
                    continue;

                nodes.Add(n);
            }

            nodes.Sort(
                (a, b) =>
                {
                    int ta = a?.Tier ?? 0;
                    int tb = b?.Tier ?? 0;

                    int c = ta.CompareTo(tb);
                    if (c != 0)
                        return c;

                    var fa = (int)ClassifyFormation(a);
                    var fb = (int)ClassifyFormation(b);

                    c = fa.CompareTo(fb);
                    if (c != 0)
                        return c;

                    return string.CompareOrdinal(
                        a?.StringId ?? string.Empty,
                        b?.StringId ?? string.Empty
                    );
                }
            );

            var counters = new Dictionary<LeanFormation, int>
            {
                [LeanFormation.Infantry] = 0,
                [LeanFormation.Ranged] = 0,
                [LeanFormation.Cavalry] = 0,
                [LeanFormation.HorseArcher] = 0,
            };

            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n?.Base == null)
                    continue;

                var f = ClassifyFormation(n);

                int index = counters[f];
                counters[f] = index + 1;

                var formationLabel = GetFormationLabel(n, f);
                var prefix = GetUpgradePrefix(index, n.Tier);

                if (nobleLine)
                {
                    if (string.IsNullOrEmpty(prefix))
                        n.Name = $"{factionName} {L.S("troop_noble", "Noble")} {formationLabel}";
                    else
                        n.Name =
                            $"{factionName} {L.S("troop_noble", "Noble")} {prefix} {formationLabel}";
                }
                else
                {
                    if (string.IsNullOrEmpty(prefix))
                        n.Name = $"{factionName} {formationLabel}";
                    else
                        n.Name = $"{factionName} {prefix} {formationLabel}";
                }
            }
        }

        private static string GetFormationLabel(WCharacter wc, LeanFormation f)
        {
            AnalyzeWeapons(
                wc,
                out bool hasBow,
                out bool hasCrossbow,
                out bool hasSling,
                out bool hasThrown,
                out bool hasOtherRangedNotThrown
            );

            // 1) Ranged formation naming
            if (f == LeanFormation.Ranged)
            {
                if (hasBow)
                    return L.S("troop_formation_archer", "Archer");

                if (hasCrossbow)
                    return L.S("troop_formation_crossbowman", "Crossbowman");

#if BL13
                if (hasSling)
                    return L.S("troop_formation_slinger", "Slinger");
#endif

                return L.S("troop_formation_skirmisher", "Skirmisher");
            }

            // 2) Thrown overrides for infantry/cavalry (and horse archer)
            if (hasThrown)
            {
                if (f == LeanFormation.Cavalry || f == LeanFormation.HorseArcher)
                    return L.S("troop_formation_mounted_skirmisher", "Mounted Skirmisher");

                if (f == LeanFormation.Infantry)
                    return L.S("troop_formation_skirmisher", "Skirmisher");
            }

            // 3) Default formation labels
            return f switch
            {
                LeanFormation.Infantry => L.S("troop_formation_infantry", "Infantry"),
                LeanFormation.Cavalry => L.S("troop_formation_cavalry", "Cavalry"),
                LeanFormation.HorseArcher => L.S("troop_formation_horse_archer", "Horse Archer"),
                _ => L.S("troop_formation_infantry", "Infantry"),
            };
        }

        private static void AnalyzeWeapons(
            WCharacter wc,
            out bool hasBow,
            out bool hasCrossbow,
            out bool hasSling,
            out bool hasThrown,
            out bool hasOtherRangedNotThrown
        )
        {
            hasBow = false;
            hasCrossbow = false;
            hasSling = false;
            hasThrown = false;
            hasOtherRangedNotThrown = false;

            if (wc?.Base == null)
                return;

            try
            {
                foreach (WItem item in wc.EquipmentRoster.Items)
                {
                    if (item?.Base == null)
                        continue;

                    // Thrown (javelins, throwing axes, etc)
                    if (item.IsThrownWeapon)
                        hasThrown = true;

                    // We only care about actual weapons here
                    var primary = item.PrimaryWeapon;
                    if (primary == null)
                        continue;

                    // "True ranged" (exclude thrown)
                    if (!item.IsRangedWeapon)
                        continue;

                    // Classify by item type (stable across weapons)
                    var type = item.Base.ItemType;

                    if (type == ItemObject.ItemTypeEnum.Bow)
                    {
                        hasBow = true;
                        continue;
                    }

                    if (type == ItemObject.ItemTypeEnum.Crossbow)
                    {
                        hasCrossbow = true;
                        continue;
                    }

#if BL13
                    if (type == ItemObject.ItemTypeEnum.Sling)
                    {
                        hasSling = true;
                        continue;
                    }
#endif

                    // Any other ranged weapon except thrown (pistol/musket/modded ranged, etc)
                    hasOtherRangedNotThrown = true;
                }
            }
            catch { }
        }

        private static List<WCharacter> EnumerateSubtree(WCharacter root)
        {
            var result = new List<WCharacter>(64);

            if (root?.Base == null || string.IsNullOrEmpty(root.StringId))
                return result;

            var seen = new HashSet<string>(StringComparer.Ordinal);

            var q = new Queue<WCharacter>();
            q.Enqueue(root);
            seen.Add(root.StringId);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur?.Base == null)
                    continue;

                result.Add(cur);

                // This uses wrapper targets, which we now keep in sync with BASE targets.
                var targets = cur.UpgradeTargets ?? [];
                for (int i = 0; i < targets.Count; i++)
                {
                    var t = targets[i];
                    var id = t?.StringId;

                    if (t?.Base == null || string.IsNullOrEmpty(id))
                        continue;

                    if (seen.Add(id))
                        q.Enqueue(t);
                }
            }

            return result;
        }

        private static string GetUpgradePrefix(int index, int tier)
        {
            if (index <= 0)
                return string.Empty;

            // If tier > 2, remove the "Trained" step:
            // index 1 becomes Veteran, index 2 becomes Elite, etc.
            if (tier > 2)
                index += 1;

            return index switch
            {
                1 => L.S("troop_prefix_trained", "Trained"),
                2 => L.S("troop_prefix_veteran", "Veteran"),
                3 => L.S("troop_prefix_elite", "Elite"),
                4 => L.S("troop_prefix_champion", "Champion"),
                5 => L.S("troop_prefix_master", "Master"),
                6 => L.S("troop_prefix_legendary", "Legendary"),
                7 => L.S("troop_prefix_mythic", "Mythic"),
                8 => L.S("troop_prefix_immortal", "Immortal"),
                _ => L.S("troop_prefix_divine", "Divine"),
            };
        }
    }
}
