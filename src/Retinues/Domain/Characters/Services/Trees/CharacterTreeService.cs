using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Runtime;

namespace Retinues.Domain.Characters.Services.Trees
{
    [SafeClass]
    public static class CharacterTreeCache
    {
        /// <summary>
        /// Node information for a character in the upgrade tree.
        /// </summary>
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

        /// <summary>
        /// Marks the cache as dirty, requiring re-initialization.
        /// </summary>
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
        /// Initializes the cache if it is not already initialized.
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

        /// <summary>
        /// Clears all cached data.
        /// </summary>
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
        /// Rebuilds all cached data.
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
        /// Rebuilds all cached data (internal, assumes lock held).
        /// </summary>
        private static void RebuildAllInternal()
        {
            _building = true;
            try
            {
                Nodes.Clear();
                Trees.Clear();

                var all = new List<WCharacter>();
                foreach (var troop in WCharacter.All)
                {
                    if (troop == null)
                        continue;

                    all.Add(troop);
                    GetOrCreateNode(troop);
                }

                // Reverse edges (UpgradeSources).
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

                // Assign roots and depths via DFS from nodes with no sources.
                var visited = new HashSet<WCharacter>();
                foreach (var pair in Nodes)
                {
                    var troop = pair.Key;
                    var node = pair.Value;

                    if (node.Sources.Count == 0)
                        AssignTree(troop, troop, 0, visited);
                }

                // Fallback for cycles or disconnected components.
                foreach (var pair in Nodes)
                {
                    var troop = pair.Key;
                    if (!visited.Contains(troop))
                        AssignTree(troop, troop, 0, visited);
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
        /// Assigns the tree information starting from the given root.
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
                tree.Add(current);

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
        /// Gets the depth of the given troop in its upgrade tree.
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
        /// Gets the root of the upgrade tree for the given troop.
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
        /// Gets the entire upgrade tree for the given troop's root.
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
        /// Gets the subtree starting from the given troop.
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
        /// Recomputes the tree information for the given root.
        /// </summary>
        public static void RecomputeForRoot(WCharacter root)
        {
            if (root == null)
                return;

            InitializeIfNeeded();

            _building = true;
            try
            {
                lock (Sync)
                {
                    // 1) Snapshot the old tree for this root

                    List<WCharacter> oldTree = [];
                    if (Trees.TryGetValue(root, out var existingTree) && existingTree != null)
                        oldTree = [.. existingTree];

                    var oldTreeSet = new HashSet<WCharacter>(oldTree);

                    // 2) Discover the new tree from this root

                    var visited = new HashSet<WCharacter>();
                    var newTreeList = new List<WCharacter>();
                    var newTreeSet = new HashSet<WCharacter>();
                    var depthMap = new Dictionary<WCharacter, int>();
                    var newSourcesMap = new Dictionary<WCharacter, List<WCharacter>>();

                    void EnsureSourceList(WCharacter troop)
                    {
                        if (!newSourcesMap.TryGetValue(troop, out var list))
                        {
                            list = [];
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

                    // 5) Update nodes for troops in the new tree

                    foreach (var troop in newTreeList)
                    {
                        var node = GetOrCreateNode(troop);

                        var previousRoot = node.Root;
                        if (previousRoot != null && previousRoot != root)
                        {
                            if (
                                Trees.TryGetValue(previousRoot, out var prevTree)
                                && prevTree != null
                            )
                                prevTree.Remove(troop);
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
            finally
            {
                _building = false;
            }
        }
    }
}
