using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor
{
    /// <summary>
    /// Base class for editor row view models. Handles selection logic, bindings, and row list access.
    /// </summary>
    public abstract class BaseRow<TList, TRow>(TList list) : BaseComponent
        where TList : BaseList<TList, TRow>
        where TRow : BaseRow<TList, TRow>
    {
        // Owned list for selection management
        private readonly TList _list = list;

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
                if (_isSelected != value)
                {
                    _isSelected = value;

                    // Trigger selection/deselection hooks
                    if (value)
                        OnSelect();
                    else
                        OnUnselect();

                    OnPropertyChanged(nameof(IsSelected));
                }
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

        public TList List => _list;

        /// <summary>
        /// Selects this row in its parent list.
        /// </summary>
        public void Select()
        {
            // No-op if already selected
            if (IsSelected)
                return;

            if (!IsEnabled)
                return; // Cannot select disabled rows

            // Safe due to self-referential generic constraint
            _list.Select((TRow)this);
        }

        /// <summary>
        /// Updates the visibility of the row based on the given filter text.
        /// </summary>
        public void UpdateIsVisible(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                IsVisible = true;
            else
                IsVisible = FilterMatch(filter);
        }

        /// <summary>
        /// Determines if the row matches the given filter text.
        /// </summary>
        public abstract bool FilterMatch(string filter);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Hooks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called when the row is selected.
        /// </summary>
        protected virtual void OnSelect() { }

        /// <summary>
        /// Called when the row is unselected.
        /// </summary>
        protected virtual void OnUnselect() { }
    }
}
