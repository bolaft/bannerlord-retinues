using System;
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Events;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Shared.Views
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
                    return;

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
                    return;

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));

                // Filtering flips a lot of rows. Do not ask headers to recompute each time.
                if (List == null || !List.IsRowVisibilityBatchActive)
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

        /// <summary>
        /// Called by the owning list after it updates AutoScrollVersion/Enabled.
        /// This avoids ordering issues between list and row event listeners.
        /// </summary>
        internal void NotifyAutoScrollChanged()
        {
            OnPropertyChanged(nameof(AutoScrollVersion));
            OnPropertyChanged(nameof(AutoScrollEnabled));
            OnPropertyChanged(nameof(AutoScrollScope));
        }

        /// <summary>
        /// Called by the owning list after it updates selection state.
        /// </summary>
        internal void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(IsSelected));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal abstract IComparable GetSortValue(ListSortKey sortKey);

        internal virtual bool TryGetPinnedSortProgress(out int progress)
        {
            progress = 0;
            return false;
        }

        internal virtual bool MatchesFilter(string filter) => true;

        internal virtual bool IsTreeNode => false;

        internal virtual IEnumerable<string> GetTreeParentIds() => null;

        internal virtual IEnumerable<string> GetTreeChildIds() => null;
    }
}
