using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Helper for vanilla troops.
    /// </summary>
    public sealed class CharacterHelperVanilla : CharacterHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Culture Cache                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class CultureCache
        {
            public string CultureId;

            public CharacterObject BasicRoot;
            public CharacterObject EliteRoot;

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

        public override WFaction ResolveFaction(WCharacter node) => null;

        public override bool IsRetinue(WCharacter node) => false;

        public override bool IsMilitiaMelee(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            return IsCultureRef(co, c => c.MeleeMilitiaTroop)
                || IsCultureRef(co, c => c.MeleeEliteMilitiaTroop);
        }

        public override bool IsMilitiaRanged(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            return IsCultureRef(co, c => c.RangedMilitiaTroop)
                || IsCultureRef(co, c => c.RangedEliteMilitiaTroop);
        }

        public override bool IsCaravanGuard(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            return IsCultureRef(co, c => c.CaravanGuard);
        }

        public override bool IsCaravanMaster(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            return IsCultureRef(co, c => c.CaravanMaster);
        }

        public override bool IsVillager(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            return IsCultureRef(co, c => c.Villager);
        }

        public override bool IsKingdom(WCharacter node) => false;

        public override bool IsClan(WCharacter node) => false;

        public override bool IsElite(WCharacter node)
        {
            var co = GetCharacterObject(node.StringId);
            var c = GetOrBuildCache(co);
            if (c == null || co == null)
                return false;

            // explicit militia elite fast-path
            if (
                IsCultureRef(co, cul => cul.MeleeEliteMilitiaTroop)
                || IsCultureRef(co, cul => cul.RangedEliteMilitiaTroop)
            )
                return true;
            if (
                IsCultureRef(co, cul => cul.MeleeMilitiaTroop)
                || IsCultureRef(co, cul => cul.RangedMilitiaTroop)
            )
                return false;

            // in-culture trees
            return c.EliteSet.Contains(co.StringId);
        }

        public override WCharacter GetParent(WCharacter node)
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool IsCultureRef(
            CharacterObject co,
            Func<CultureObject, CharacterObject> sel
        )
        {
            var cul = co?.Culture;
            if (cul == null)
                return false;
            var target = sel(cul);
            return ReferenceEquals(co, target);
        }
    }
}
