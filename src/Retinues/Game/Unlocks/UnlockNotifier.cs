using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.UI.Services;

namespace Retinues.Game.Unlocks
{
    [SafeClass]
    public static class UnlockNotifier
    {
        public enum UnlockMethod
        {
            Kills,
            Discards,
            Workshops,
        }

        public struct WorkshopStartInfo
        {
            public string WorkshopLabel;
            public WItem Item;
        }

        public static void ItemsUnlocked(UnlockMethod method, IReadOnlyList<WItem> items)
        {
            if (items == null || items.Count == 0)
                return;

            // Dedupe by stringId to avoid double-reporting during catch-up days.
            var unique = new List<WItem>(items.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var id = it?.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (seen.Add(id))
                    unique.Add(it);
            }

            if (unique.Count == 0)
                return;

            var methodText = MethodLabel(method);
            var summary = Summarize(unique.Select(GetItemName), max: 5);

            var title = L.T("item_unlock_title", "Items unlocked");
            var desc = L.T("item_unlock_desc", "{METHOD}: {ITEMS}")
                .SetTextVariable("METHOD", methodText)
                .SetTextVariable("ITEMS", summary);

            UnlockNotifierBehavior.Notify(title, desc);
        }

        public static void WorkshopsStarted(IReadOnlyList<WorkshopStartInfo> starts)
        {
            if (starts == null || starts.Count == 0)
                return;

            // Dedupe by workshop label + item id (helps during multi-day catch-up).
            var lines = new List<string>(starts.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < starts.Count; i++)
            {
                var s = starts[i];
                var itemId = s.Item?.StringId ?? string.Empty;
                var wk = s.WorkshopLabel ?? string.Empty;
                if (string.IsNullOrEmpty(wk) || string.IsNullOrEmpty(itemId))
                    continue;

                var key = wk + "|" + itemId;
                if (!seen.Add(key))
                    continue;

                lines.Add($"{wk}: {GetItemName(s.Item)}");
            }

            if (lines.Count == 0)
                return;

            var summary = Summarize(lines, max: 5);

            var title = L.T("workshop_unlock_start_title", "Workshops");
            var desc = L.T("workshop_unlock_start_desc", "{ITEMS}")
                .SetTextVariable("ITEMS", summary);

            UnlockNotifierBehavior.Notify(title, desc);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string MethodLabel(UnlockMethod method)
        {
            return method switch
            {
                UnlockMethod.Kills => L.S("item_unlock_method_kills", "Battle"),
                UnlockMethod.Discards => L.S("item_unlock_method_discards", "Discards"),
                UnlockMethod.Workshops => L.S("item_unlock_method_workshops", "Workshops"),
                _ => method.ToString(),
            };
        }

        private static string GetItemName(WItem item)
        {
            // Prefer underlying base name if wrapper doesn't expose Name.
            var name = item?.Base?.Name?.ToString();
            if (!string.IsNullOrEmpty(name))
                return name;

            return item?.StringId ?? "Unknown";
        }

        private static string Summarize(IEnumerable<string> items, int max)
        {
            if (items == null)
                return string.Empty;

            var list = items.Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (list.Count == 0)
                return string.Empty;

            var take = Math.Min(max, list.Count);
            var shown = string.Join(", ", list.Take(take));

            if (list.Count <= max)
                return shown;

            var remaining = list.Count - max;
            return $"{shown}, ... (+{remaining})";
        }
    }
}
