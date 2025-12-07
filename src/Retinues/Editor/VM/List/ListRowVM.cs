using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    /// <summary>
    /// Base row ViewModel used in list headers.
    /// </summary>
    public abstract class ListRowVM(ListHeaderVM header, string id) : BaseStatefulVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ListHeaderVM _header = header;

        private string _id = id;
        private bool _isSelected;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal ListHeaderVM Header => _header;

        internal ListVM List => _header?.List;

        [DataSourceProperty]
        public string Id
        {
            get => _id;
            set
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
        public virtual bool IsEnabled => true;

        // Type flags; templates use these to choose row layout.
        [DataSourceProperty]
        public virtual bool IsCharacter => false;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected)
                {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Commands                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public virtual void ExecuteSelect()
        {
            Log.Info($"ListElementVM: Selecting element '{Id}'");
            IsSelected = true;
            List?.OnElementSelected(this);
        }
    }
}
