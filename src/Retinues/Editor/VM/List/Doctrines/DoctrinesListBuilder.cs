using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Editor.Controllers.Doctrines;
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

            var categories = DoctrinesController.GetCategories();
            if (categories == null || categories.Count == 0)
            {
                list.SetHeaders(headers);
                return;
            }

            foreach (var category in categories)
            {
                if (category == null)
                    continue;

                var header = new DoctrinesListHeader(list, category.Id, category.Name)
                {
                    IsExpanded = true,
                };

                var any = false;

                if (category.Doctrines != null)
                {
                    foreach (var doctrine in category.Doctrines)
                    {
                        if (doctrine == null)
                            continue;

                        any = true;
                        header.AddRow(new DoctrinesListRowVM(header, doctrine.Id));
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
