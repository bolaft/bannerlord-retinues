using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    public abstract class ListRowVM : ViewModel
    {
        private readonly ListHeaderVM _header;

        private string _id;
        private string _label;
        private bool _isSelected;

        protected ListRowVM(ListHeaderVM header, string id, string label)
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
                if (value == _id)
                    return;
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        [DataSourceProperty]
        public string Label
        {
            get => _label;
            set
            {
                if (value == _label)
                    return;
                _label = value;
                OnPropertyChanged(nameof(Label));
            }
        }

        [DataSourceProperty]
        public virtual bool IsEnabled => true;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected)
                    return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // -------- Type flags (default false) --------

        [DataSourceProperty]
        public virtual bool IsCharacter => false;

        // --------------------------------------------

        [DataSourceMethod]
        public virtual void ExecuteSelect()
        {
            Log.Info($"ListElementVM: Selecting element '{Id}'");
            IsSelected = true;
            _header.List.OnElementSelected(this);
        }

        // Optional hook for headers to call
        public override void RefreshValues()
        {
            // Default: just push Label change; subclasses can override
            OnPropertyChanged(nameof(Label));
        }
    }
}
