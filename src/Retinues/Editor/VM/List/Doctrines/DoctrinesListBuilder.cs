using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Editor.Controllers.Doctrines;
using Retinues.Game.Doctrines;
using Retinues.UI.Services;

namespace Retinues.Editor.VM.List.Doctrines
{
    /// <summary>
    /// Builds the doctrines list for the editor.
    /// </summary>
    public sealed class DoctrinesListBuilder : BaseListBuilder
    {
        /// <summary>
        /// Builds the sort buttons for the doctrines list.
        /// </summary>
        protected override void BuildSortButtons(ListVM list)
        {
            list.SortButtons.Clear();

            list.SortButtons.Add(
                new ListSortButtonVM(list, ListSortKey.Name, L.S("sort_by_name", "Name"), 3)
            );

            list.RecomputeSortButtonProperties();
        }

        /// <summary>
        /// Builds the sections for the doctrines list.
        /// </summary>
        protected override void BuildSections(ListVM list)
        {
            var headers = new List<ListHeaderVM>();

            if (!Settings.EnableDoctrines)
            {
                list.SetHeaders(headers);
                return;
            }

            var categories = DoctrinesCatalog.Categories;
            if (categories == null || categories.Count == 0)
            {
                list.SetHeaders(headers);
                return;
            }

            foreach (var kvp in categories)
            {
                var category = kvp.Value;
                if (category == null)
                    continue;

                var header = new DoctrinesListHeader(
                    list,
                    category.Id,
                    category.Name?.ToString() ?? string.Empty
                )
                {
                    IsExpanded = true,
                };

                var any = false;

                var ids = category.DoctrineIds;
                if (ids != null)
                {
                    for (var i = 0; i < ids.Count; i++)
                    {
                        var doctrineId = ids[i];
                        if (string.IsNullOrEmpty(doctrineId))
                            continue;

                        // Only add rows for doctrines that exist in the catalog.
                        if (
                            !DoctrinesCatalog.TryGetDoctrine(doctrineId, out DoctrineDefinition def)
                            || def == null
                        )
                            continue;

                        any = true;
                        header.AddRow(new DoctrinesListRowVM(header, doctrineId));
                    }
                }

                if (!any)
                    continue;

                headers.Add(header);
                header.UpdateRowCount();
                header.UpdateState();
            }

            list.SetHeaders(headers);
        }
    }
}
