using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Game.Doctrines;
using Retinues.GUI.Editor.Shared.Views;
using Retinues.GUI.Services;

namespace Retinues.GUI.Editor.Modules.Pages.Doctrines.Views.List
{
    /// <summary>
    /// Doctrines list ViewModel.
    /// </summary>
    public sealed class DoctrinesListVM : BaseListVM
    {
        protected override EditorPage Page => EditorPage.Doctrines;

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

            RecomputeSortButtonProperties();
        }

        private void BuildSections()
        {
            var headers = new List<ListHeaderVM>();

            if (!Settings.EnableDoctrines)
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

                var header = new DoctrinesListHeader(this, category.Id, category.Name.ToString())
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
