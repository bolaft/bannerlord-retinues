using Retinues.Wrappers.Characters;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    public class ListHeaderVM(ListVM list, string id, string name) : ViewModel
    {
        private readonly ListVM _list = list;

        private string _id = id;
        private string _name = name;
        private bool _isExpanded = false;
        private MBBindingList<ListElementVM> _elements = [];

        // New: enabled state derived from element count
        [DataSourceProperty]
        public bool IsEnabled => _elements != null && _elements.Count > 0;

        // Notify IsEnabled and collapse the header if it is disabled
        private void UpdateIsEnabledState()
        {
            OnPropertyChanged(nameof(IsEnabled));

            if (!IsEnabled)
                IsExpanded = false;
        }

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
                if (value == _isExpanded)
                    return;
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(MarginBottom));
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
                    UpdateIsEnabledState();
                }
            }
        }

        [DataSourceProperty]
        public string ElementCountText => $"({_elements?.Count ?? 0})";

        public CharacterListElementVM AddCharacter(WCharacter character)
        {
            var wasEmpty = _elements.Count == 0;

            var element = new CharacterListElementVM(this, character);
            _elements.Add(element);

            if (wasEmpty)
                IsExpanded = true;

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
            UpdateIsEnabledState();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            foreach (var element in _elements)
            {
                element.RefreshValues();
            }

            OnPropertyChanged(nameof(ElementCountText));
            UpdateIsEnabledState();
        }

        // Called by the toggle button
        public void ExecuteToggle()
        {
            IsExpanded = !IsExpanded;
        }
    }
}
