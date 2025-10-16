using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor
{
    public abstract class BaseRow<TList, TRow> : BaseComponent
        where TList : BaseList<TList, TRow>
        where TRow : BaseRow<TList, TRow>
    {
        // Owned list for selection management
        public abstract TList List { get; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _isSelected;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;

                // Trigger selection/deselection hooks
                if (value)
                    OnSelect();
                else
                    OnUnselect();

                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private bool _isEnabled = true;

        [DataSourceProperty]
        public virtual bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteSelect() => Select();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void Select()
        {
            // No-op if already selected
            if (IsSelected)
                return;

            if (!IsEnabled)
                return; // Cannot select disabled rows

            // Safe due to self-referential generic constraint
            List.Select((TRow)this);
        }

        public void UpdateIsVisible(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                IsVisible = true;
            else
                IsVisible = FilterMatch(filter);
        }

        public abstract bool FilterMatch(string filter);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Hooks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected virtual void OnSelect() { }

        protected virtual void OnUnselect() { }
    }
}
