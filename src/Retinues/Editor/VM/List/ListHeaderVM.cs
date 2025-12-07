using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.VM.List.Rows;
using Retinues.Wrappers.Characters;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    /// <summary>
    /// Collapsible header that groups list rows.
    /// </summary>
    public class ListHeaderVM(ListVM list, string id, string name) : BaseStatefulVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ListVM _list = list;

        private string _id = id;
        private string _name = name;
        private bool _isExpanded = false;

        private MBBindingList<ListRowVM> _elements = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal ListVM List => _list;

        [DataSourceProperty]
        public string Id
        {
            get => _id;
            private set
            {
                if (value == _id)
                {
                    return;
                }

                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            private set
            {
                if (value == _name)
                {
                    return;
                }

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
                {
                    return;
                }

                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(MarginBottom));
            }
        }

        [DataSourceProperty]
        public int MarginBottom => _isExpanded ? 0 : 3;

        [DataSourceProperty]
        public MBBindingList<ListRowVM> Elements
        {
            get => _elements;
            private set
            {
                if (ReferenceEquals(value, _elements))
                {
                    return;
                }

                _elements = value;
                OnPropertyChanged(nameof(Elements));
                OnPropertyChanged(nameof(ElementCountText));
                UpdateIsEnabledState();
            }
        }

        [DataSourceProperty]
        public string ElementCountText => $"({GetVisibleElementCount()})";

        /// <summary>
        /// Bound by the header toggle to control enabled/disabled visuals.
        /// </summary>
        [DataSourceProperty]
        public bool IsEnabled => GetVisibleElementCount() > 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Rows                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public CharacterRowVM AddCharacterRow(WCharacter character, bool civilian = false)
        {
            var wasEmpty = _elements.Count == 0;

            var row = new CharacterRowVM(this, character, civilian);
            _elements.Add(row);

            OnPropertyChanged(nameof(Elements));
            OnPropertyChanged(nameof(ElementCountText));
            UpdateIsEnabledState();

            if (wasEmpty)
            {
                IsExpanded = true;
            }

            return row;
        }

        internal void ClearSelectionExcept(ListRowVM selected)
        {
            foreach (var element in _elements)
            {
                element.IsSelected = ReferenceEquals(element, selected);
            }
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

        // Called by rows when their visibility changes (filtering).
        internal void OnRowVisibilityChanged()
        {
            OnPropertyChanged(nameof(ElementCountText));
            UpdateIsEnabledState();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteToggle()
        {
            IsExpanded = !IsExpanded;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void UpdateIsEnabledState()
        {
            OnPropertyChanged(nameof(IsEnabled));

            if (!IsEnabled)
            {
                IsExpanded = false;
            }
        }

        private int GetVisibleElementCount()
        {
            if (_elements == null || _elements.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _elements.Count; i++)
            {
                var row = _elements[i];
                if (row != null && row.IsVisible)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
