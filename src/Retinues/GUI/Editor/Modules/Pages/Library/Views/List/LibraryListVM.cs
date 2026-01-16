using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Framework.Model.Exports;
using Retinues.GUI.Editor.Modules.Pages.Library.Views.List;
using Retinues.GUI.Editor.Shared.Views;
using Retinues.GUI.Services;
using Retinues.Utilities;

namespace Retinues.GUI.Editor.VM.List.Library
{
    /// <summary>
    /// Library list ViewModel (exported factions and characters).
    /// </summary>
    public sealed class LibraryListVM : BaseListVM
    {
        protected override EditorPage Page => EditorPage.Library;

        public override void Build()
        {
            BuildSortButtons();
            BuildSections();
            RecomputeHeaderStates();
        }

        private void BuildSortButtons()
        {
            SortButtons.Clear();

            SortButtons.Add(
                new ListSortButtonVM(this, ListSortKey.Name, L.S("sort_by_name", "Name"), 3)
            );

            SortButtons.Add(
                new ListSortButtonVM(this, ListSortKey.Value, L.S("sort_by_date", "Date"), 2)
            );

            RecomputeSortButtonProperties();
        }

        private void BuildSections()
        {
            var headers = new List<ListHeaderVM>();

            var all = MLibrary.GetAll();
            if (all == null || all.Count == 0)
            {
                SetHeaders(headers);
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

                var header = new ListHeaderVM(this, id, name);
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
                name: L.S("list_header_exports_characters", "Troops"),
                items: all.Where(x => x.Kind == MLibraryKind.Character),
                isFaction: false
            );

            SetHeaders(headers);

            // Default selection: keep current if still present, otherwise select first.
            try
            {
                var currentPath = EditorState.Instance.LibraryItem?.FilePath;

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

                if (selected != null && EditorState.Instance.LibraryItem == null)
                    EditorState.Instance.LibraryItem = selected;
                else if (
                    selected != null
                    && EditorState.Instance.LibraryItem != null
                    && !string.Equals(
                        EditorState.Instance.LibraryItem.FilePath,
                        selected.FilePath,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && all.All(x =>
                        !string.Equals(
                            x.FilePath,
                            EditorState.Instance.LibraryItem.FilePath,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                    EditorState.Instance.LibraryItem = selected;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Library list default selection failed.");
            }
        }
    }
}
