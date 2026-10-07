using System;
using System.Collections.Generic;

namespace Retinues.Editor.MVC.Pages.Equipment.Services
{
    internal static class CraftedItemSelection
    {
        // Stock and equipment requirements are keyed by item ID, not design. Keep every owned
        // copy; choose one representative only when a design has no stock or equipped copies.
        internal static List<T> Select<T>(
            IReadOnlyList<T> items,
            Func<T, string> getDesign,
            Func<T, bool> isOwned
        )
        {
            var ownedDesigns = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                var design = getDesign(item);
                if (design != null && isOwned(item))
                    ownedDesigns.Add(design);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<T>();
            foreach (var item in items)
            {
                var design = getDesign(item);
                if (
                    design == null
                    || (ownedDesigns.Contains(design) ? isOwned(item) : seen.Add(design))
                )
                    result.Add(item);
            }
            return result;
        }
    }
}
