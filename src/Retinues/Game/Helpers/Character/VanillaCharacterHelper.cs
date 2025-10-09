using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Helper for vanilla troops. Handles culture caching, parent/child lookup, and wrapper convenience for vanilla troop logic.
    /// </summary>
    public sealed class VanillaCharacterHelper : CharacterHelperBase, ICharacterHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Culture Cache                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class CultureCache
        {
            public string CultureId;

            public CharacterObject BasicRoot;
            public CharacterObject EliteRoot;

            public CharacterObject MilitiaMelee;
            public CharacterObject MilitiaMeleeElite;
            public CharacterObject MilitiaRanged;
            public CharacterObject MilitiaRangedElite;

            public readonly HashSet<string> BasicSet = new(StringComparer.Ordinal);
            public readonly HashSet<string> EliteSet = new(StringComparer.Ordinal);

            // childId -> parentId (per culture)
            public readonly Dictionary<string, string> ParentMap = new(StringComparer.Ordinal);
        }

        private static readonly Dictionary<string, CultureCache> _cache = new(
            StringComparer.Ordinal
        );

        private static CultureCache GetOrBuildCache(CharacterObject sample)
        {
            var cul = sample?.Culture;
            if (cul == null)
                return null;

            var cid = cul.StringId;
            if (string.IsNullOrEmpty(cid))
                return null;

            if (_cache.TryGetValue(cid, out var c))
                return c;

            c = new CultureCache
            {
                CultureId = cid,
                BasicRoot = cul.BasicTroop,
                EliteRoot = cul.EliteBasicTroop,
                MilitiaMelee = cul.MeleeMilitiaTroop,
                MilitiaMeleeElite = cul.MeleeEliteMilitiaTroop,
                MilitiaRanged = cul.RangedMilitiaTroop,
                MilitiaRangedElite = cul.RangedEliteMilitiaTroop,
            };

            void Crawl(CharacterObject root, HashSet<string> set)
            {
                if (root == null)
                    return;

                var visited = new HashSet<string>(StringComparer.Ordinal) { root.StringId };
                var q = new Queue<CharacterObject>();
                q.Enqueue(root);
                set.Add(root.StringId);

                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    var kids = cur.UpgradeTargets ?? [];

                    foreach (var co in kids)
                    {
                        if (co?.Culture != root.Culture)
                            continue;
                        if (!visited.Add(co.StringId))
                            continue;

                        set.Add(co.StringId);
                        c.ParentMap[co.StringId] = cur.StringId;
                        q.Enqueue(co);
                    }
                }
            }

            Crawl(c.BasicRoot, c.BasicSet);
            Crawl(c.EliteRoot, c.EliteSet);

            _cache[cid] = c;
            return c;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Public API                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets a CharacterObject by vanilla troop ID.
        /// </summary>
        public CharacterObject GetCharacterObject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            return MBObjectManager.Instance.GetObject<CharacterObject>(id);
        }

        /// <summary>
        /// Not applicable for vanilla; always returns null.
        /// </summary>
        public CharacterObject GetCharacterObject(
            bool isKingdom,
            bool isElite,
            bool isRetinue,
            bool isMilitiaMelee,
            bool isMilitiaRanged,
            IReadOnlyList<int> path = null
        )
        {
            // Not applicable for vanilla; caller should use GetCharacterObject(string id).
            return null;
        }

        /// <summary>
        /// Returns true if the ID is a custom troop (always false for vanilla).
        /// </summary>
        public bool IsCustom(string id) => false;

        /// <summary>
        /// Returns true if the ID is a retinue troop (always false for vanilla).
        /// </summary>
        public bool IsRetinue(string id) => false;

        /// <summary>
        /// Returns true if the ID is a melee militia troop.
        /// </summary>
        public bool IsMilitiaMelee(string id)
        {
            var co = GetCharacterObject(id);
            var c = GetOrBuildCache(co);
            if (c == null || co == null)
                return false;
            return ReferenceEquals(co, c.MilitiaMelee) || ReferenceEquals(co, c.MilitiaMeleeElite);
        }

        /// <summary>
        /// Returns true if the ID is a ranged militia troop.
        /// </summary>
        public bool IsMilitiaRanged(string id)
        {
            var co = GetCharacterObject(id);
            var c = GetOrBuildCache(co);
            if (c == null || co == null)
                return false;
            return ReferenceEquals(co, c.MilitiaRanged)
                || ReferenceEquals(co, c.MilitiaRangedElite);
        }

        /// <summary>
        /// Returns true if the ID is an elite troop.
        /// </summary>
        public bool IsElite(string id)
        {
            var co = GetCharacterObject(id);
            var c = GetOrBuildCache(co);
            if (c == null || co == null)
                return false;

            // Fast path: explicit militia elites / basics
            if (
                ReferenceEquals(co, c.MilitiaMeleeElite)
                || ReferenceEquals(co, c.MilitiaRangedElite)
            )
                return true;
            if (ReferenceEquals(co, c.MilitiaMelee) || ReferenceEquals(co, c.MilitiaRanged))
                return false;

            // In-culture trees
            if (c.EliteSet.Contains(id))
                return true;
            if (c.BasicSet.Contains(id))
                return false;

            return false;
        }

        /// <summary>
        /// Returns true if the ID is a kingdom troop (always false for vanilla).
        /// </summary>
        public bool IsKingdom(string id) => false;

        /// <summary>
        /// Returns true if the ID is a clan troop (always false for vanilla).
        /// </summary>
        public bool IsClan(string id) => false;

        /// <summary>
        /// Gets the path (upgrade tree) for a vanilla troop ID.
        /// </summary>
        public IReadOnlyList<int> GetPath(string id)
        {
            var co = GetCharacterObject(id);
            var c = GetOrBuildCache(co);
            if (c == null || co == null)
                return [];

            // Try from Basic root, then Elite
            if (TryPath(c.BasicRoot, co, c.ParentMap, out var p))
                return p;
            if (TryPath(c.EliteRoot, co, c.ParentMap, out p))
                return p;
            return [];
        }

        /// <summary>
        /// Resolves the faction for a vanilla troop ID (always null).
        /// </summary>
        public WFaction ResolveFaction(string id) => null;

        /// <summary>
        /// Gets the parent ID for a vanilla troop.
        /// </summary>
        public string GetParentId(string id)
        {
            var co = GetCharacterObject(id);
            var c = GetOrBuildCache(co);
            if (c == null || co == null)
                return null;

            return c.ParentMap.TryGetValue(id, out var pid) ? pid : null;
        }

        /// <summary>
        /// Gets the child IDs for a vanilla troop.
        /// </summary>
        public IEnumerable<string> GetChildrenIds(string id)
        {
            var co = GetCharacterObject(id);
            if (co == null)
                yield break;

            var kids = co.UpgradeTargets ?? [];
            foreach (var child in kids)
            {
                if (child?.Culture == co.Culture)
                    yield return child.StringId;
            }
        }

        /// <summary>
        /// Gets the parent troop for a node, or null if root.
        /// </summary>
        public WCharacter GetParent(WCharacter node)
        {
            if (node == null)
                return null;
            var pid = GetParentId(node.StringId);
            if (string.IsNullOrEmpty(pid))
                return null;

            var pco = GetCharacterObject(pid);
            return pco != null ? new WCharacter(pco) : null;
        }

        /// <summary>
        /// Gets the child troops for a node.
        /// </summary>
        public IEnumerable<WCharacter> GetChildren(WCharacter node)
        {
            if (node == null)
                yield break;

            var kids = node.Base?.UpgradeTargets ?? [];
            foreach (var child in kids)
            {
                if (child?.Culture == node.Base.Culture)
                    yield return new WCharacter(child);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Helper to find the path from root to target using parent map.
        /// </summary>
        private static bool TryPath(
            CharacterObject root,
            CharacterObject target,
            Dictionary<string, string> parentMap,
            out IReadOnlyList<int> path
        )
        {
            path = [];
            if (root == null || target == null)
                return false;
            if (root.Culture != target.Culture)
                return false;

            // Build local parent map indexes by BFS (we already keep parentMap for id->parentId; we reconstruct branch indices)
            var visited = new HashSet<string>(StringComparer.Ordinal) { root.StringId };
            var q = new Queue<CharacterObject>();
            var idxMap = new Dictionary<string, (string parentId, int branch)>(
                StringComparer.Ordinal
            );

            q.Enqueue(root);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                var kids = cur.UpgradeTargets ?? [];

                int i = 0;
                foreach (var co in kids)
                {
                    if (co?.Culture != root.Culture)
                    {
                        i++;
                        continue;
                    }
                    if (!visited.Add(co.StringId))
                    {
                        i++;
                        continue;
                    }

                    idxMap[co.StringId] = (cur.StringId, i);

                    if (ReferenceEquals(co, target) || co.StringId == target.StringId)
                    {
                        var rev = new List<int>(8);
                        var id = co.StringId;
                        while (idxMap.TryGetValue(id, out var p))
                        {
                            rev.Add(p.branch);
                            id = p.parentId;
                            if (id == root.StringId)
                                break;
                        }
                        rev.Reverse();
                        path = rev;
                        return true;
                    }

                    q.Enqueue(co);
                    i++;
                }
            }

            return false;
        }
    }
}
