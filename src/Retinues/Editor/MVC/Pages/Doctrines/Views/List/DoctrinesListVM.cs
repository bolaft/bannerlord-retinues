using System.Collections.Generic;
using Retinues.Behaviors.Doctrines;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Services;
using Retinues.Settings;

namespace Retinues.Editor.MVC.Pages.Doctrines.Views.List
{
    /// <summary>
    /// Doctrines list ViewModel.
    /// </summary>
    public sealed class DoctrinesListVM : BaseListVM
    {
        protected override EditorPage Page => EditorPage.Doctrines;

        /// <summary>
        /// Builds the doctrines list.
        /// </summary>
        public override void Build()
        {
            BuildSortButtons();
            BuildSections();
            RecomputeHeaderStates();
        }

        /// <summary>
        /// Builds the sort buttons for the doctrines list.
        /// </summary>
        private void BuildSortButtons()
        {
            SortButtons.Clear();

            SortButtons.Add(
                new ListSortButtonVM(this, ListSortKey.Name, L.S("sort_by_name", "Name"), 3)
            );

            RecomputeSortButtonProperties();
        }

        /// <summary>
        /// Builds the sections for the doctrines list.
        /// </summary>
        private void BuildSections()
        {
            var headers = new List<ListHeaderVM>();

            if (!Configuration.EnableDoctrines)
            {
                SetHeaders(headers);
                return;
            }

            var categories = DoctrinesRegistry.GetCategories();
            if (categories == null || categories.Count == 0)
            {
                SetHeaders(headers);
                return;
            }

            foreach (var category in categories)
            {
                bool any = category.Doctrines.Count > 0;

                var header = new ListHeaderVM(this, category.Id, category.Name.ToString())
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

            SetHeaders(headers);
        }
    }
}
