using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    public abstract class ListRowVM : ViewModel
    {
        private readonly ListHeaderVM _header;

        private string _id;
        private bool _isSelected;

        protected ListRowVM(ListHeaderVM header, string id)
        {
            _header = header;
            _id = id;
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
    }
}
