using System;
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.GUI.Editor.Events;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Shared.Views
{
    /// <summary>
    /// Base row ViewModel used in list headers.
    /// </summary>
    public abstract class BaseListRowVM(ListHeaderVM header, string id) : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ListHeaderVM _header = header;
        internal ListHeaderVM Header => _header;
        internal BaseListVM List => Header.List;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Category Flags                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public virtual bool IsCharacter => false;

        [DataSourceProperty]
        public virtual bool IsHero => false;

        [DataSourceProperty]
        public virtual bool IsEquipment => false;

        [DataSourceProperty]
        public virtual bool IsDoctrine => false;

        [DataSourceProperty]
        public virtual bool IsLibraryItem => false;

        [DataSourceProperty]
        public virtual bool IsLibraryFaction => false;

        [DataSourceProperty]
        public virtual bool IsLibraryCharacter => false;

        [DataSourceProperty]
        public virtual bool IsRetinueUnlockProgress => false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Identifier                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string _id = id;

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public abstract bool IsSelected { get; }

        [DataSourceMethod]
        public abstract void ExecuteSelect();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _isVisible = true;

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible)
                {
                    return;
                }

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));

                _header?.OnRowVisibilityChanged();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Enabled                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public virtual bool IsEnabled => true;

        [DataSourceProperty]
        public virtual string Brush
        {
            get
            {
                if (!IsEnabled)
                    return "Clan.Management.LeftTuple";

                return "Clan.Item.Tuple";
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Auto Scroll                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public int AutoScrollVersion => Header.List.AutoScrollVersion;

        [DataSourceProperty]
        public string AutoScrollScope => Header.List.AutoScrollScope;

        [DataSourceProperty]
        public bool AutoScrollEnabled => Header.List.AutoScrollRowsEnabled;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal abstract IComparable GetSortValue(ListSortKey sortKey);

        /// <summary>
        /// Returns true for rows that should stay pinned to the end of the header when sorting.
        /// Used for special rows like partial unlock progress.
        /// </summary>
        internal virtual bool TryGetPinnedSortProgress(out int progress)
        {
            progress = 0;
            return false;
        }

        /// <summary>
        /// True when this row participates in tree sorting and tree filtering (ancestor visibility).
        /// </summary>
        internal virtual bool IsTreeNode => false;

        /// <summary>
        /// Parent ids for this row when used as a tree node.
        /// </summary>
        internal virtual IEnumerable<string> GetTreeParentIds() => null;

        /// <summary>
        /// Child ids for this row when used as a tree node.
        /// </summary>
        internal virtual IEnumerable<string> GetTreeChildIds() => null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Filtering                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines if this row matches the given filter string.
        /// </summary>
        internal virtual bool MatchesFilter(string filter) => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Management                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines if property change notifications should be sent for the given event.
        /// </summary>
        protected override bool __ShouldNotifyProperty(
            EventManager.Context context,
            UIEvent e,
            string propertyName,
            bool globalListener
        )
        {
            // For list rows, only the selected row refreshes by default.
            // If the listener is marked Global=true, refresh even when not selected.
            if (IsSelected)
            {
                return true;
            }

            return globalListener;
        }
    }
}
