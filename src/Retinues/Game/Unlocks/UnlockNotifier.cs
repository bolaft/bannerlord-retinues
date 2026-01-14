using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.UI.Services;
using TaleWorlds.Localization;

namespace Retinues.Game.Unlocks
{
    [SafeClass]
    public static class UnlockNotifier
    {
        /// <summary>
        /// Method by which items were unlocked.
        /// </summary>
        public enum UnlockMethod
        {
            Kills,
            Discards,
            Workshops,
            Troops,
        }

        /// <summary>
        /// Information about a workshop starting an unlock project.
        /// </summary>
        public struct WorkshopStartInfo
        {
            public string WorkshopTypeName;
            public string SettlementName;
            public WItem Item;
        }

        /// <summary>
        /// Notify the player of unlocked items.
        /// </summary>
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

            var names = unique.Select(s => s.Name).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (names.Count == 0)
                return;

            var listText = JoinWithAnd(names, max: 5, out var isPlural);
            var wasWere = isPlural
                ? L.S("unlock_verb_were", "were")
                : L.S("unlock_verb_was", "was");

            var title = L.T("item_unlock_title", "Items unlocked");

            var desc = method switch
            {
                UnlockMethod.Kills => L.T(
                    "item_unlock_sentence_battle",
                    "{ITEMS} {VERB} unlocked in battle."
                ),
                UnlockMethod.Discards => L.T(
                    "item_unlock_sentence_donations",
                    "{ITEMS} {VERB} unlocked through direct donations."
                ),
                UnlockMethod.Workshops => L.T(
                    "item_unlock_sentence_workshops",
                    "{ITEMS} {VERB} unlocked through workshops."
                ),
                UnlockMethod.Troops => L.T(
                    "item_unlock_sentence_troops",
                    "{ITEMS} {VERB} unlocked while creating troops."
                ),
                _ => L.T("item_unlock_sentence_generic", "{ITEMS} {VERB} unlocked."),
            };

            desc = desc.SetTextVariable("ITEMS", listText).SetTextVariable("VERB", wasWere);

            UnlockNotifierBehavior.Notify(title, desc);
        }

        /// <summary>
        /// Join a list of strings with commas and "and".
        /// </summary>
        private static string JoinWithAnd(IReadOnlyList<string> items, int max, out bool isPlural)
        {
            if (items == null)
            {
                isPlural = true;
                return string.Empty;
            }

            // Filter empties first so counts are correct.
            var filtered = items.Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (filtered.Count == 0)
            {
                isPlural = true;
                return string.Empty;
            }

            isPlural = filtered.Count != 1;

            var take = Math.Min(max, filtered.Count);
            var shown = filtered.Take(take).ToList();
            var more = filtered.Count - take;

            var andWord = L.S("unlock_and", "and");

            // If we have more items than we show, use:
            // "A, B and 3 more"
            if (more > 0)
            {
                var prefix = string.Join(", ", shown);
                var moreText = L.T("unlock_more", "{COUNT} more")
                    .SetTextVariable("COUNT", more)
                    .ToString();

                return $"{prefix} {andWord} {moreText}";
            }

            if (shown.Count == 1)
                return shown[0];

            if (shown.Count == 2)
                return $"{shown[0]} {andWord} {shown[1]}";

            return $"{string.Join(", ", shown.GetRange(0, shown.Count - 1))} {andWord} {shown[shown.Count - 1]}";
        }

        /// <summary>
        /// Notify the player of workshops starting unlock projects.
        /// </summary>
        public static void WorkshopsStarted(IReadOnlyList<WorkshopStartInfo> starts)
        {
            if (starts == null || starts.Count == 0)
                return;

            // Dedupe by workshop+town+item (helps during multi-day catch-up).
            var distinct = new List<WorkshopStartInfo>(starts.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < starts.Count; i++)
            {
                var s = starts[i];

                var wk = s.WorkshopTypeName ?? string.Empty;
                var town = s.SettlementName ?? string.Empty;
                var itemId = s.Item?.StringId ?? string.Empty;

                if (
                    string.IsNullOrEmpty(wk)
                    || string.IsNullOrEmpty(town)
                    || string.IsNullOrEmpty(itemId)
                )
                    continue;

                var key = wk + "|" + town + "|" + itemId;
                if (!seen.Add(key))
                    continue;

                distinct.Add(s);
            }

            if (distinct.Count == 0)
                return;

            var title = L.T("workshop_unlock_start_title", "Workshops");

            // Keep the popup readable.
            const int maxParagraphs = 3;
            var take = Math.Min(maxParagraphs, distinct.Count);

            var paragraphs = new List<string>(take + 1);

            for (var i = 0; i < take; i++)
            {
                var s = distinct[i];

                var line = L.T(
                        "workshop_unlock_start_line",
                        "Your {WORKSHOP} in {TOWN} started work on unlocking {ITEM}."
                    )
                    .SetTextVariable("WORKSHOP", s.WorkshopTypeName.ToLowerInvariant())
                    .SetTextVariable("TOWN", s.SettlementName)
                    .SetTextVariable("ITEM", s.Item.Name)
                    .ToString();

                paragraphs.Add(line);
            }

            if (distinct.Count > take)
            {
                var more = distinct.Count - take;
                var moreLine = L.T(
                        "workshop_unlock_start_more",
                        "And {COUNT} more workshops started new projects."
                    )
                    .SetTextVariable("COUNT", more)
                    .ToString();

                paragraphs.Add(moreLine);
            }

            var desc = new TextObject(string.Join("\n\n", paragraphs));

            UnlockNotifierBehavior.Notify(title, desc);
        }
    }
}
