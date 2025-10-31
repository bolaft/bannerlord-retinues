using System.Collections.Generic;
using System.Linq;
using Retinues.Safety.Legacy;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Troops
{
    public static class TroopIndex
    {
        private static Dictionary<string, TroopIndexEntry> Map =>
            TroopBehavior.Index ??= new Dictionary<string, TroopIndexEntry>(
                System.StringComparer.Ordinal
            );

        public static TroopIndexEntry GetOrCreate(string id) =>
            Map.TryGetValue(id, out var e) ? e : (Map[id] = new TroopIndexEntry { Id = id });

        public static TroopIndexEntry FindBySignature(
            bool isKingdom,
            bool isElite,
            bool isRetinue,
            bool isMelee,
            bool isRanged,
            IReadOnlyList<int> path
        )
        {
            foreach (var e in Map.Values)
            {
                if (e.IsKingdom != isKingdom)
                    continue;
                if (e.IsElite != isElite)
                    continue;
                if (e.IsRetinue != isRetinue)
                    continue;
                if (e.IsMilitiaMelee != isMelee)
                    continue;
                if (e.IsMilitiaRanged != isRanged)
                    continue;

                var ePath = e.Path ?? new List<int>();
                var pPath = path ?? System.Array.Empty<int>();
                if (ePath.SequenceEqual(pPath))
                    return e;
            }
            return null;
        }

        public static TroopIndexEntry FindByPath(IReadOnlyList<int> path)
        {
            var pPath = path ?? System.Array.Empty<int>();
            return Map.Values.FirstOrDefault(e => (e.Path ?? new List<int>()).SequenceEqual(pPath));
        }

        public static void SetFlags(
            string id,
            bool isKingdom,
            bool isElite,
            bool isRetinue,
            bool isMelee,
            bool isRanged
        )
        {
            var e = GetOrCreate(id);
            e.IsKingdom = isKingdom;
            e.IsElite = isElite;
            e.IsRetinue = isRetinue;
            e.IsMilitiaMelee = isMelee;
            e.IsMilitiaRanged = isRanged;
        }

        public static void SetParent(string id, string parentId, int branchIndex)
        {
            var e = GetOrCreate(id);
            e.ParentId = parentId;
            if (!string.IsNullOrEmpty(parentId))
            {
                var p = GetOrCreate(parentId);
                if (!p.ChildrenIds.Contains(id))
                    p.ChildrenIds.Add(id);

                // rebuild Path if parent has one
                e.Path = [.. p.Path ?? []];
                e.Path.Add(branchIndex);
            }
        }

        public static IReadOnlyList<int> GetPath(string id) =>
            Map.TryGetValue(id, out var e) ? (IReadOnlyList<int>)(e.Path ?? []) : [];

        public static IEnumerable<string> GetChildrenIds(string id) =>
            Map.TryGetValue(id, out var e) ? (IEnumerable<string>)(e.ChildrenIds ?? []) : [];

        // Allocate the next free generic stub id
        public static string AllocateStub()
        {
            // enumerate preloaded stubs:
            var pool = MBObjectManager
                .Instance.GetObjectTypeList<CharacterObject>()
                .Where(co =>
                    co.StringId.StartsWith("retinues_custom_", System.StringComparison.Ordinal)
                )
                .Select(co => co.StringId)
                .OrderBy(s => s) // because of zero padding from your generator
                .ToList();

            foreach (var id in pool)
                if (!Map.ContainsKey(id)) // not allocated yet
                    return id;

            Log.Warn("No free stub ids left in pool retinues_custom_*");
            return null;
        }
    }
}
