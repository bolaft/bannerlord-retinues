using System.Collections.Generic;
using Retinues.Configuration;
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

            var categories = DoctrinesRegistry.GetCategories();
            if (categories == null || categories.Count == 0)
            {
                list.SetHeaders(headers);
                return;
            }

            foreach (var category in categories)
            {
                bool any = category.Doctrines.Count > 0;

                var header = new DoctrinesListHeader(list, category.Id, category.Name.ToString())
                {
                    IsExpanded = any,
                };

                if (!any)
                    continue;

                foreach (var doctrine in category.Doctrines)
                    header.AddRow(new DoctrinesListRowVM(header, doctrine));

                headers.Add(header);
                header.UpdateRowCount();
                header.UpdateState();
            }

            list.SetHeaders(headers);
        }
    }
}
