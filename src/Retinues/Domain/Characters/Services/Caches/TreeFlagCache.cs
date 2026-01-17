using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;

namespace Retinues.Domain.Characters.Services.Caches
{
    /// <summary>
    /// Caches the factions for troops based on their presence in faction rosters.
    /// </summary>
    [SafeClass]
    public static class TreeFlagCache
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Custom Tree Flag Cache                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly object Sync = new();

        private static bool _built;
        private static readonly HashSet<string> ById = [];

        /// <summary>
        /// Invalidates the cache.
        /// </summary>
        [StaticClearAction]
        public static void Invalidate()
        {
            lock (Sync)
            {
                ById.Clear();
                _built = false;
            }
        }

        /// <summary>
        /// Gets whether the given wrapped character is a faction troop.
        /// </summary>
        public static bool Get(WCharacter wc)
        {
            if (wc == null)
                return false;

            EnsureBuilt();

            var id = wc.StringId;
            if (string.IsNullOrEmpty(id))
                return false;

            lock (Sync)
            {
                return ById.Contains(id);
            }
        }

        /// <summary>
        /// Ensures the cache is built.
        /// </summary>
        private static void EnsureBuilt()
        {
            if (_built)
                return;

            lock (Sync)
            {
                if (_built)
                    return;

                BuildLocked();
                _built = true;
            }
        }

        /// <summary>
        /// Builds the cache.
        /// </summary>
        private static void BuildLocked()
        {
            ById.Clear();

            IndexMapFactionMany(WClan.All);
            IndexMapFactionMany(WKingdom.All);
        }

        /// <summary>
        /// Indexes many map factions into the cache.
        /// </summary>
        private static void IndexMapFactionMany<TFaction>(IEnumerable<TFaction> factions)
            where TFaction : IBaseFaction
        {
            if (factions == null)
                return;

            foreach (var faction in factions)
            {
                if (faction == null)
                    continue;

                // Custom roots: mark entire trees.
                MarkTree(faction.RootBasic);
                MarkTree(faction.RootElite);

                // Retinues: mark ONLY retinue-to-retinue trees (conversion targets must not count).
                MarkManyRetinueTrees(faction.RosterRetinues);
            }
        }

        /// <summary>
        /// Indexes many retinue troops into the cache.
        /// </summary>
        private static void MarkManyRetinueTrees(IEnumerable<WCharacter> troops)
        {
            if (troops == null)
                return;

            foreach (var wc in troops)
                MarkRetinueTree(wc);
        }

        /// <summary>
        /// Marks the retinue tree starting from the given root.
        /// </summary>
        private static void MarkRetinueTree(WCharacter root)
        {
            if (root == null)
                return;

            // Always mark the root retinue troop itself.
            Mark(root);

            // Only traverse upgrade edges where BOTH sides are retinues.
            var visited = new HashSet<WCharacter>();

            void Dfs(WCharacter current)
            {
                if (current == null)
                    return;

                if (!visited.Add(current))
                    return;

                Mark(current);

                var targets = current.UpgradeTargets;
                if (targets == null || targets.Count == 0)
                    return;

                for (int i = 0; i < targets.Count; i++)
                {
                    var child = targets[i];
                    if (child == null)
                        continue;

                    if (!child.IsRetinue)
                        continue;

                    Dfs(child);
                }
            }

            if (root.IsRetinue)
                Dfs(root);
        }

        /// <summary>
        /// Marks the entire tree starting from the given root.
        /// </summary>
        private static void MarkTree(WCharacter root)
        {
            if (root == null)
                return;

            var tree = root.Tree;
            if (tree == null)
            {
                Mark(root);
                return;
            }

            for (int i = 0; i < tree.Count; i++)
                Mark(tree[i]);
        }

        /// <summary>
        /// Marks a wrapped character as being part of a custom troop tree.
        /// </summary>
        private static void Mark(WCharacter wc)
        {
            if (wc == null)
                return;

            var id = wc.StringId;
            if (string.IsNullOrEmpty(id))
                return;

            ById.Add(id);
        }
    }
}
