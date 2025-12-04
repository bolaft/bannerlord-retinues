using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers
{
    public class VanillaHelper
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
        //                   Vanilla Inferences                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WCharacter GetParent(WCharacter node)
        {
            if (node?.Base == null)
                return null;

            var c = GetOrBuildCache(node.Base);
            if (c == null)
                return null;

            var pid = c.ParentMap.TryGetValue(node.StringId, out var tmpPid) ? tmpPid : null;

            if (string.IsNullOrEmpty(pid))
                return null;

            var pco = MBObjectManager.Instance.GetObject<CharacterObject>(pid);

            return pco != null ? new WCharacter(pco) : null;
        }
    }
}
