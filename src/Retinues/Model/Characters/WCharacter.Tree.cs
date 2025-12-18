using System.Collections.Generic;
using System.Linq;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Upgrade Targets                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<List<WCharacter>> UpgradeTargetsAttribute =>
            Attribute<List<WCharacter>>(
                getter: _ => [.. Base.UpgradeTargets.Select(Get)],
                setter: (_, list) =>
                {
                    // Convert to array.
                    var array = (list ?? []).Select(w => w?.Base).ToArray();

                    // Apply to base.
                    Reflection.SetPropertyValue(Base, "UpgradeTargets", array);

                    // Keep hierarchy cache in sync whenever targets change.
                    CharacterTreeCacheHelper.RecomputeForRoot(Root);

                    // Invalidate related caches.
                    InvalidateTroopSourceFlagsCache();
                    InvalidateTroopFactionsCache();
                }
            );

        public List<WCharacter> UpgradeTargets
        {
            get => UpgradeTargetsAttribute.Get();
            set => UpgradeTargetsAttribute.Set(value ?? []);
        }

        public bool AddUpgradeTarget(WCharacter target)
        {
            if (target == null)
                return false;

            // Avoid self-loops.
            if (target == this)
                return false;

            var list = UpgradeTargets;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == target)
                    return false;
            }

            list.Add(target);
            UpgradeTargets = list;
            return true;
        }

        public bool RemoveUpgradeTarget(WCharacter target)
        {
            if (target == null)
                return false;

            var list = UpgradeTargets;
            if (list.Count == 0)
                return false;

            var removed = false;

            // Backwards in case of duplicates (should not happen, but safe).
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == target)
                {
                    list.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
                UpgradeTargets = list;

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
                Nodes.Clear();
                Trees.Clear();

                // Materialize once so we do not enumerate WCharacter.All multiple times.
                var all = new List<WCharacter>();
                foreach (var troop in WCharacter.All)
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

                    foreach (var candidate in WCharacter.All)
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
                        if (newSourcesMap.TryGetValue(troop, out var sources) && sources != null)
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
        }
    }
}
