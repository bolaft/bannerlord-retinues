using System;
using System.Collections.Generic;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Domain.Equipments.Helpers
{
    internal static class ItemHolsterSafety
    {
        private const int MaxHolsters = 4;
        private static readonly HashSet<string> Reported = new(StringComparer.Ordinal);

        // Armor and other non-weapons legitimately omit holsters. Empty arrays/strings also
        // mean "no holster" and are valid. Null strings are different: the engine dereferences
        // them, and its fixed-size output cannot hold more than four entries.
        internal static bool HasInvalidWeaponHolsters(ItemObject item)
        {
            if (item?.WeaponComponent == null)
                return false;

            var holsters = item.ItemHolsters;
            if (holsters == null || holsters.Length > MaxHolsters)
                return true;
            foreach (var holster in holsters)
                if (holster == null)
                    return true;
            return false;
        }

        internal static bool TryGetFallbackIndices(
            ItemObject item, Func<string, int> resolve, out StackArray.StackArray4Int indices
        )
        {
            indices = default;
            if (!HasInvalidWeaponHolsters(item))
                return false;

            var holsters = item.ItemHolsters;
            for (int i = 0; i < MaxHolsters; i++)
            {
                var holster = holsters != null && i < holsters.Length ? holsters[i] : null;
                indices[i] = string.IsNullOrEmpty(holster) ? -1 : resolve(holster);
            }
            Report(item, "Using safe weapon holster fallback");
            return true;
        }

        internal static void Report(ItemObject item, string action)
        {
            if (item == null)
                return;
            var id = item.StringId ?? "<no id>";
            lock (Reported)
                if (!Reported.Add(action + ":" + id))
                    return;

            var holsters = item.ItemHolsters;
            var description = holsters == null ? "<null array>" : string.Join(", ",
                Array.ConvertAll(holsters, h => h == null ? "<null>" : "'" + h + "'"));
            Log.Warning($"{action}: item='{id}', holsters=[{description}]. Item definition and saved equipment left intact.");
        }

        [StaticClearAction]
        public static void ClearReports()
        {
            lock (Reported)
                Reported.Clear();
        }
    }
}
