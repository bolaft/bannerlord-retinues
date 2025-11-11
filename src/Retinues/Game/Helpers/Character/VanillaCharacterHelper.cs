using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Helper for vanilla troops.
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
            var culture = sample?.Culture;
            if (culture == null)
                return null;

            var cid = culture.StringId;
            if (string.IsNullOrEmpty(cid))
                return null;

            if (_cache.TryGetValue(cid, out var c))
                return c;

            c = new CultureCache
            {
                CultureId = cid,
                BasicRoot = culture.BasicTroop,
                EliteRoot = culture.EliteBasicTroop,
                MilitiaMelee = culture.MeleeMilitiaTroop,
                MilitiaMeleeElite = culture.MeleeEliteMilitiaTroop,
                MilitiaRanged = culture.RangedMilitiaTroop,
                MilitiaRangedElite = culture.RangedEliteMilitiaTroop,
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
        /// Resolves the faction for a vanilla troop ID (always null).
        /// </summary>
        public WFaction ResolveFaction(WCharacter node) => null;

        /// <summary>
        /// Returns true if the ID is a retinue troop (always false for vanilla).
        /// </summary>
        public bool IsRetinue(WCharacter node) => false;

        /// <summary>
        /// Returns true if the ID is a melee militia troop.
        /// </summary>
        public bool IsMilitiaMelee(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            var c = GetOrBuildCache(co);
            if (c == null || co == null)
                return false;
            return ReferenceEquals(co, c.MilitiaMelee) || ReferenceEquals(co, c.MilitiaMeleeElite);
        }

        /// <summary>
        /// Returns true if the ID is a ranged militia troop.
        /// </summary>
        public bool IsMilitiaRanged(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            var c = GetOrBuildCache(co);
            if (c == null || co == null)
                return false;
            return ReferenceEquals(co, c.MilitiaRanged)
                || ReferenceEquals(co, c.MilitiaRangedElite);
        }

        /// <summary>
        /// Returns true if the ID is an armed trader troop.
        /// </summary>
        public bool IsArmedTrader(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            var cul = co?.Culture;
            return cul != null && ReferenceEquals(co, cul.ArmedTrader);
        }

        /// <summary>
        /// Returns true if the ID is a caravan guard troop.
        /// </summary>
        public bool IsCaravanGuard(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            var cul = co?.Culture;
            return cul != null && ReferenceEquals(co, cul.CaravanGuard);
        }

        /// <summary>
        /// Returns true if the ID is a caravan master troop.
        /// </summary>
        public bool IsCaravanMaster(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            var cul = co?.Culture;
            return cul != null && ReferenceEquals(co, cul.CaravanMaster);
        }

        /// <summary>
        /// Returns true if the ID is a villager troop.
        /// </summary>
        public bool IsVillager(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            var cul = co?.Culture;
            return cul != null && ReferenceEquals(co, cul.Villager);
        }

        /// <summary>
        /// Returns true if the ID is an elite troop.
        /// </summary>
        public bool IsElite(WCharacter node)
        {
            var id = node.StringId;
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
        public bool IsKingdom(WCharacter node) => false;

        /// <summary>
        /// Returns true if the ID is a clan troop (always false for vanilla).
        /// </summary>
        public bool IsClan(WCharacter node) => false;

        /// <summary>
        /// Resolves the faction for a vanilla troop ID (always null).
        /// </summary>
        public WFaction ResolveFaction(string id) => null;

        /// <summary>
        /// Gets the parent troop for a node, or null if root.
        /// </summary>
        public WCharacter GetParent(WCharacter node)
        {
            if (node == null)
                return null;

            var id = node.StringId;

            var co = GetCharacterObject(id);
            if (co == null)
                return null;
            var c = GetOrBuildCache(co);
            if (c == null)
                return null;

            var pid = c.ParentMap.TryGetValue(id, out var tmpPid) ? tmpPid : null;

            if (string.IsNullOrEmpty(pid))
                return null;

            var pco = GetCharacterObject(pid);

            return pco != null ? new WCharacter(pco) : null;
        }
    }
}
