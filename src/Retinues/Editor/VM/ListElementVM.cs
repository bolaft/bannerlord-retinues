using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    public class ListElementVM : ViewModel
    {
        private readonly ListHeaderVM _header;

        private string _id;
        private string _label;
        private bool _isSelected;

        public ListElementVM(ListHeaderVM header, string id, string label)
        {
            _header = header;

            _id = id;
            _label = label;
        }

        internal ListHeaderVM Header => _header;

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
        public string Label
        {
            get => _label;
            set
            {
                if (value != _label)
                {
                    _label = value;
                    OnPropertyChanged(nameof(Label));
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        // Called by the row button
        public void ExecuteSelect()
        {
            IsSelected = true;
            _header.List.OnElementSelected(this);
        }
    }
}
