using System.Linq;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    public class ListHeaderVM : ViewModel
    {
        private readonly ListVM _list;

        private string _id;
        private string _name;
        private bool _isExpanded;
        private MBBindingList<ListElementVM> _elements;

        public ListHeaderVM(ListVM list, string id, string name)
        {
            _list = list;

            _id = id;
            _name = name;
            _isExpanded = true;

            _elements = new MBBindingList<ListElementVM>();
        }

        internal ListVM List => _list;

        [DataSourceProperty]
        public string Id
        {
            get => _id;
            set
            {
                if (value == _id)
                    return;
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name)
                    return;
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        [DataSourceProperty]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value == _isExpanded)
                    return;
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        [DataSourceProperty]
        public MBBindingList<ListElementVM> Elements
        {
            get => _elements;
            set
            {
                if (value == _elements)
                    return;
                _elements = value;
                OnPropertyChanged(nameof(Elements));
                OnPropertyChanged(nameof(ElementCountText));
            }
        }

        // "(N)" where N is number of elements
        [DataSourceProperty]
        public string ElementCountText
        {
            get
            {
                var count = _elements?.Count ?? 0;
                return $"({count})";
            }
        }

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
                    element.IsSelected = false;
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
                element.RefreshValues();

            OnPropertyChanged(nameof(ElementCountText));
        }

        public void SortElementsByLabel(bool ascending)
        {
            if (_elements == null || _elements.Count <= 1)
                return;

            var sorted = ascending
                ? _elements.OrderBy(e => e.Label)
                : _elements.OrderByDescending(e => e.Label);

            _elements = [.. sorted];
            OnPropertyChanged(nameof(Elements));
            OnPropertyChanged(nameof(ElementCountText));
        }

        // Called by the toggle button
        public void ExecuteToggle()
        {
            IsExpanded = !IsExpanded;
        }
    }
}
