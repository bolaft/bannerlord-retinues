using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    public class ListHeaderVM(ListVM list, string id, string name) : ViewModel
    {
        private readonly ListVM _list = list;

        private string _id = id;
        private string _name = name;
        private bool _isExpanded = true;
        private MBBindingList<ListElementVM> _elements = [];

        internal ListVM List => _list;

        [DataSourceProperty]
        public string Id
        {
            get => _id;
            set
            {
                if (value != _id)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [DataSourceProperty]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                    OnPropertyChanged(nameof(MarginBottom));
                }
            }
        }

        [DataSourceProperty]
        public int MarginBottom => _isExpanded ? 0 : 3;

        [DataSourceProperty]
        public MBBindingList<ListElementVM> Elements
        {
            get => _elements;
            set
            {
                if (value != _elements)
                {
                    _elements = value;
                    OnPropertyChanged(nameof(Elements));
                    OnPropertyChanged(nameof(ElementCountText));
                }
            }
        }

        [DataSourceProperty]
        public string ElementCountText => $"({_elements?.Count ?? 0})";

        public ListElementVM AddElement(string id, string label)
        {
            var element = new ListElementVM(this, id, label);
            _elements.Add(element);

            OnPropertyChanged(nameof(Elements));
            OnPropertyChanged(nameof(ElementCountText));

            return element;
        }

        public void ClearSelectionExcept(ListElementVM keep)
        {
            foreach (var element in _elements)
            {
                if (!ReferenceEquals(element, keep))
                {
                    element.IsSelected = false;
                }
            }
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Elements));
            OnPropertyChanged(nameof(ElementCountText));
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            foreach (var element in _elements)
            {
                element.RefreshValues();
            }

            OnPropertyChanged(nameof(ElementCountText));
        }

        // Called by the toggle button
        public void ExecuteToggle()
        {
            IsExpanded = !IsExpanded;
        }
    }
}
