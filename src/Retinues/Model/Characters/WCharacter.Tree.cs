using System.Collections.Generic;
using System.Linq;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Upgrade Targets                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // We keep the saved ids even if the base objects cannot be resolved yet.
        // This prevents "wiping" CharacterObject.UpgradeTargets during load.
        private List<string> _upgradeTargetIdsPersisted = [];

        MAttribute<List<string>> UpgradeTargetsAttribute =>
            Attribute<List<string>>(
                getter: _ =>
                {
                    // Prefer persisted ids if we have them (e.g. after load).
                    if (_upgradeTargetIdsPersisted != null && _upgradeTargetIdsPersisted.Count > 0)
                        return [.. _upgradeTargetIdsPersisted];

                    // Otherwise reflect current base state.
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
                            : ids.Select(s => s?.Trim())
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Distinct()
                                .ToList();

                    // Try to apply to the underlying CharacterObject now if possible.
                    TryApplyUpgradeTargetIds();

                    // Invalidate related caches.
                    InvalidateTroopSourceFlagsCache();
                    InvalidateTroopFactionsCache();

                    // Rebuild tree caches (safe even if application is deferred).
                    CharacterTreeCacheHelper.MarkDirty();
                }
            );

        private void TryApplyUpgradeTargetIds()
        {
            // If MBObjectManager is not ready yet, keep ids and apply later.
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
                    Log.Info($"Could not resolve upgrade target '{id}' for '{StringId}' yet.");
            }

            Reflection.SetPropertyValue(Base, "UpgradeTargets", resolved.ToArray());
        }

        // Public API stays as List<WCharacter>
        public List<WCharacter> UpgradeTargets
        {
            get
            {
                // If we have persisted ids and the base array is empty (typical after a bad early load),
                // try to apply again when the editor queries this.
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
        //                     Character Tree                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<WCharacter> UpgradeSources => CharacterTreeCacheHelper.GetUpgradeSources(this);
        public int Depth => CharacterTreeCacheHelper.GetDepth(this);
        public WCharacter Root => CharacterTreeCacheHelper.GetRoot(this) ?? this;
        public List<WCharacter> Tree => CharacterTreeCacheHelper.GetSubtree(this);
        public List<WCharacter> RootTree => CharacterTreeCacheHelper.GetTree(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Cache Helper                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal static class CharacterTreeCacheHelper
        {
            private sealed class Node
            {
                public WCharacter Root;
                public int Depth;
                public readonly List<WCharacter> Sources = [];
            }

            private static readonly Dictionary<WCharacter, Node> Nodes = [];
            private static readonly Dictionary<WCharacter, List<WCharacter>> Trees = [];

            private static readonly object Sync = new();
            private static bool _initialized;
            private static bool _building;
            public static bool IsBuilding => _building;

            public static void MarkDirty()
            {
                lock (Sync)
                {
                    _initialized = false;
                    Nodes.Clear();
                    Trees.Clear();
                }
            }

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                     Initialization                     //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            /// <summary>
            /// Initializes the hierarchy cache if it has not been initialized yet.
            /// </summary>
            public static void InitializeIfNeeded()
            {
                if (_initialized)
                    return;

                lock (Sync)
                {
                    if (_initialized)
                        return;

                    RebuildAllInternal();
                    _initialized = true;
                }
            }

            [StaticClearAction]
            public static void ClearAll()
            {
                lock (Sync)
                {
                    Nodes.Clear();
                    Trees.Clear();
                    _initialized = false;
                    _building = false;
                }
            }

            /// <summary>
            /// Rebuilds the entire hierarchy cache.
            /// </summary>
            public static void RebuildAll()
            {
                lock (Sync)
                {
                    RebuildAllInternal();
                    _initialized = true;
                }
            }

            /// <summary>
            /// Internal method to rebuild the entire hierarchy
            /// cache without locking.
            /// </summary>
            private static void RebuildAllInternal()
            {
                _building = true;
                try
                {
                    Nodes.Clear();
                    Trees.Clear();

                    // Materialize once so we do not enumerate WCharacter.All multiple times.
                    var all = new List<WCharacter>();
                    foreach (var troop in All)
                    {
                        if (troop == null)
                            continue;

                        all.Add(troop);
                        GetOrCreateNode(troop);
                    }

                    // Build reverse edges (UpgradeSources).
                    for (int i = 0; i < all.Count; i++)
                    {
                        var troop = all[i];
                        var targets = troop.UpgradeTargets;
                        if (targets == null || targets.Count == 0)
                            continue;

                        for (int j = 0; j < targets.Count; j++)
                        {
                            var target = targets[j];
                            if (target == null)
                                continue;

                            var targetNode = GetOrCreateNode(target);
                            targetNode.Sources.Add(troop);
                        }
                    }

                    // Assign roots and depths via DFS starting from nodes with no sources.
                    var visited = new HashSet<WCharacter>();
                    foreach (var pair in Nodes)
                    {
                        var troop = pair.Key;
                        var node = pair.Value;

                        if (node.Sources.Count == 0)
                        {
                            AssignTree(troop, troop, 0, visited);
                        }
                    }

                    // Fallback for nodes in cycles or components without a clear source free root.
                    foreach (var pair in Nodes)
                    {
                        var troop = pair.Key;
                        if (!visited.Contains(troop))
                        {
                            AssignTree(troop, troop, 0, visited);
                        }
                    }
                }
                finally
                {
                    _building = false;
                }
            }

            /// <summary>
            /// Gets or creates the node for the given troop.
            /// </summary>
            private static Node GetOrCreateNode(WCharacter troop)
            {
                if (!Nodes.TryGetValue(troop, out var node))
                {
                    node = new Node();
                    Nodes.Add(troop, node);
                }

                return node;
            }

            /// <summary>
            /// Recursively assigns root and depth for the subtree starting at current.
            /// </summary>
            private static void AssignTree(
                WCharacter root,
                WCharacter current,
                int depth,
                HashSet<WCharacter> visited
            )
            {
                if (current == null)
                    return;

                if (!visited.Add(current))
                    return;

                var node = GetOrCreateNode(current);
                node.Root = root;
                node.Depth = depth;

                if (!Trees.TryGetValue(root, out var tree))
                {
                    tree = [];
                    Trees.Add(root, tree);
                }

                if (!tree.Contains(current))
                {
                    tree.Add(current);
                }

                var targets = current.UpgradeTargets;
                if (targets == null || targets.Count == 0)
                    return;

                for (int i = 0; i < targets.Count; i++)
                {
                    var child = targets[i];
                    if (child == null)
                        continue;

                    AssignTree(root, child, depth + 1, visited);
                }
            }

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                         Queries                        //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            /// <summary>
            /// Gets the upgrade sources for the given troop.
            /// </summary>
            public static List<WCharacter> GetUpgradeSources(WCharacter troop)
            {
                if (troop == null)
                    return [];

                InitializeIfNeeded();

                if (!Nodes.TryGetValue(troop, out var node))
                    return [];

                return node.Sources;
            }

            /// <summary>
            /// Gets the depth of the given troop in its upgrade hierarchy.
            /// </summary>
            public static int GetDepth(WCharacter troop)
            {
                if (troop == null)
                    return 0;

                InitializeIfNeeded();

                if (!Nodes.TryGetValue(troop, out var node))
                    return 0;

                return node.Depth;
            }

            /// <summary>
            /// Gets the root upgrade troop for the given troop.
            /// </summary>
            public static WCharacter GetRoot(WCharacter troop)
            {
                if (troop == null)
                    return null;

                InitializeIfNeeded();

                if (!Nodes.TryGetValue(troop, out var node))
                    return troop;

                return node.Root ?? troop;
            }

            /// <summary>
            /// Gets the entire upgrade tree for the given troop.
            /// </summary>
            public static List<WCharacter> GetTree(WCharacter troop)
            {
                if (troop == null)
                    return [];

                InitializeIfNeeded();

                if (!Nodes.TryGetValue(troop, out var node))
                    return [troop];

                var root = node.Root ?? troop;

                if (!Trees.TryGetValue(root, out var tree))
                    return [troop];

                return tree;
            }

            /// <summary>
            /// Gets the upgrade subtree for the given troop (self + descendants only).
            /// </summary>
            public static List<WCharacter> GetSubtree(WCharacter troop)
            {
                if (troop == null)
                    return [];

                InitializeIfNeeded();

                var visited = new HashSet<WCharacter>();
                var result = new List<WCharacter>();

                void Dfs(WCharacter current)
                {
                    if (current == null)
                        return;

                    if (!visited.Add(current))
                        return;

                    result.Add(current);

                    var targets = current.UpgradeTargets;
                    if (targets == null || targets.Count == 0)
                        return;

                    for (int i = 0; i < targets.Count; i++)
                    {
                        var child = targets[i];
                        if (child == null)
                            continue;

                        Dfs(child);
                    }
                }

                Dfs(troop);
                return result;
            }

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                    Partial Recompute                   //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            /// <summary>
            /// Recomputes the hierarchy cache for the subtree starting at the given root.
            /// </summary>
            public static void RecomputeForRoot(WCharacter root)
            {
                if (root == null)
                    return;

                // Make sure we have an initial global map.
                InitializeIfNeeded();

                _building = true;
                try
                {
                    lock (Sync)
                    {
                        // 1) Snapshot the old tree for this root

                        List<WCharacter> oldTree = [];
                        if (Trees.TryGetValue(root, out var existingTree) && existingTree != null)
                        {
                            oldTree = [.. existingTree];
                        }

                        var oldTreeSet = new HashSet<WCharacter>(oldTree);

                        // 2) Discover the new tree from this root (current data)

                        var visited = new HashSet<WCharacter>();
                        var newTreeList = new List<WCharacter>();
                        var newTreeSet = new HashSet<WCharacter>();
                        var depthMap = new Dictionary<WCharacter, int>();
                        var newSourcesMap = new Dictionary<WCharacter, List<WCharacter>>();

                        void EnsureSourceList(WCharacter troop)
                        {
                            if (!newSourcesMap.TryGetValue(troop, out var list))
                            {
                                list = new List<WCharacter>();
                                newSourcesMap.Add(troop, list);
                            }
                        }

                        void Dfs(WCharacter current, int depth)
                        {
                            if (current == null)
                                return;

                            if (!visited.Add(current))
                                return;

                            depthMap[current] = depth;
                            newTreeList.Add(current);
                            newTreeSet.Add(current);

                            EnsureSourceList(current);

                            var targets = current.UpgradeTargets;
                            if (targets == null || targets.Count == 0)
                                return;

                            for (int i = 0; i < targets.Count; i++)
                            {
                                var target = targets[i];
                                if (target == null)
                                    continue;

                                EnsureSourceList(target);
                                newSourcesMap[target].Add(current);

                                Dfs(target, depth + 1);
                            }
                        }

                        Dfs(root, 0);

                        // 3) Add sources from outside this tree
                        // (so UpgradeSources sees cross-tree parents too)

                        foreach (var candidate in All)
                        {
                            if (candidate == null)
                                continue;

                            var targets = candidate.UpgradeTargets;
                            if (targets == null || targets.Count == 0)
                                continue;

                            for (int i = 0; i < targets.Count; i++)
                            {
                                var target = targets[i];
                                if (target == null)
                                    continue;

                                if (!newTreeSet.Contains(target))
                                    continue;

                                EnsureSourceList(target);

                                var list = newSourcesMap[target];
                                // Avoid duplicates if candidate is also inside the DFS tree.
                                var already = false;
                                for (int j = 0; j < list.Count; j++)
                                {
                                    if (list[j] == candidate)
                                    {
                                        already = true;
                                        break;
                                    }
                                }

                                if (!already)
                                    list.Add(candidate);
                            }
                        }

                        // 4) Clear nodes that used to be in this root's tree
                        // but are no longer reachable from it

                        foreach (var troop in oldTreeSet)
                        {
                            if (troop == null || newTreeSet.Contains(troop))
                                continue;

                            if (!Nodes.TryGetValue(troop, out var node))
                                continue;

                            if (node.Root == root)
                            {
                                node.Root = null;
                                node.Depth = 0;
                                node.Sources.Clear();
                            }
                        }

                        // 5) Update nodes for all troops in the new tree

                        foreach (var troop in newTreeList)
                        {
                            var node = GetOrCreateNode(troop);

                            // If this troop used to belong to another root's tree,
                            // remove it from that tree list.
                            var previousRoot = node.Root;
                            if (previousRoot != null && previousRoot != root)
                            {
                                if (
                                    Trees.TryGetValue(previousRoot, out var prevTree)
                                    && prevTree != null
                                )
                                {
                                    prevTree.Remove(troop);
                                }
                            }

                            node.Root = root;
                            node.Depth = depthMap.TryGetValue(troop, out var d) ? d : 0;

                            node.Sources.Clear();
                            if (
                                newSourcesMap.TryGetValue(troop, out var sources)
                                && sources != null
                            )
                            {
                                for (int i = 0; i < sources.Count; i++)
                                {
                                    var src = sources[i];
                                    if (src != null)
                                        node.Sources.Add(src);
                                }
                            }
                        }

                        // 6) Replace the tree list for this root

                        Trees[root] = newTreeList;
                    }
                }
                finally
                {
                    _building = false;
                }
            }
        }
    }
}
