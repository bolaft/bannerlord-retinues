using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Model;
using Retinues.Utilities;

namespace Retinues.Editor.VM.List.Library
{
    /// <summary>
    /// Builds the Library list (exported factions and characters).
    /// </summary>
    public sealed class LibraryListBuilder : BaseListBuilder
    {
        protected override void BuildSortButtons(ListVM list)
        {
            list.SortButtons.Clear();

            list.SortButtons.Add(
                new ListSortButtonVM(list, ListSortKey.Name, L.S("sort_by_name", "Name"), 3)
            );

            // Date sort: reuse Value (your LibraryExportRowVM already maps Value -> CreatedUtc).
            list.SortButtons.Add(
                new ListSortButtonVM(list, ListSortKey.Value, L.S("sort_by_date", "Date"), 2)
            );

            list.RecomputeSortButtonProperties();
        }

        protected override void BuildSections(ListVM list)
        {
            var headers = new List<ListHeaderVM>();

            var all = MLibrary.GetAll();
            if (all == null || all.Count == 0)
            {
                list.SetHeaders(headers);
                return;
            }

            void AddSection(
                string id,
                string name,
                IEnumerable<MLibrary.Item> items,
                bool isFaction
            )
            {
                var sectionItems = items?.ToList();
                if (sectionItems == null || sectionItems.Count == 0)
                    return;

                var header = new LibraryListHeaderVM(list, id, name);
                headers.Add(header);

                header.IsExpanded = true;

                foreach (var item in sectionItems)
                {
                    if (item == null)
                        continue;

                    header.AddRow(
                        isFaction
                            ? new LibraryFactionExportRowVM(header, item)
                            : new LibraryCharacterExportRowVM(header, item)
                    );
                }

                header.UpdateRowCount();
                header.UpdateState();
            }

            AddSection(
                id: "exports_factions",
                name: L.S("list_header_exports_factions", "Factions"),
                items: all.Where(x => x.Kind == MLibraryKind.Faction),
                isFaction: true
            );

            AddSection(
                id: "exports_characters",
                name: L.S("list_header_exports_characters", "Characters"),
                items: all.Where(x => x.Kind == MLibraryKind.Character),
                isFaction: false
            );

            list.SetHeaders(headers);

            // Default selection: keep current if still present, otherwise select first.
            try
            {
                var currentPath = State.Instance.LibraryItem?.FilePath;

                var selected = !string.IsNullOrWhiteSpace(currentPath)
                    ? all.FirstOrDefault(x =>
                        x != null
                        && string.Equals(
                            x.FilePath,
                            currentPath,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    : null;

                selected ??= all.FirstOrDefault(x => x != null);

                if (selected != null && State.Instance.LibraryItem == null)
                    State.Instance.LibraryItem = selected;
                else if (
                    selected != null
                    && State.Instance.LibraryItem != null
                    && !string.Equals(
                        State.Instance.LibraryItem.FilePath,
                        selected.FilePath,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && all.All(x =>
                        !string.Equals(
                            x.FilePath,
                            State.Instance.LibraryItem.FilePath,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                    State.Instance.LibraryItem = selected;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryListBuilder.BuildSections: default selection failed.");
            }
        }
    }
}
