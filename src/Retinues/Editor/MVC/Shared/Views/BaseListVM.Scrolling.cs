using System.Threading;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Shared.Views
{
    /// <summary>
    /// Partial class for base list ViewModel handling auto-scrolling.
    /// </summary>
    public abstract partial class BaseListVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Scroll Buttons                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Tooltip ScrollToTopTooltip => new(L.S("scroll_to_top_tooltip", "Scroll to top"));

        [DataSourceProperty]
        public Tooltip ScrollToBottomTooltip =>
            new(L.S("scroll_to_bottom_tooltip", "Scroll to bottom"));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Auto Scroll                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _autoScrollRowsEnabled = true;

        [DataSourceProperty]
        public bool AutoScrollRowsEnabled
        {
            get => _autoScrollRowsEnabled;
            protected set
            {
                if (value == _autoScrollRowsEnabled)
                    return;

                _autoScrollRowsEnabled = value;
                OnPropertyChanged(nameof(AutoScrollRowsEnabled));

                // Important: rows bind AutoScrollEnabled through their own property.
                NotifyRowsAutoScrollChanged();
            }
        }

        private int _autoScrollVersion;
        private static int _autoScrollScopeCounter;
        private readonly int _autoScrollScopeId = Interlocked.Increment(
            ref _autoScrollScopeCounter
        );

        [DataSourceProperty]
        public int AutoScrollVersion
        {
            get => _autoScrollVersion;
            protected set
            {
                if (value == _autoScrollVersion)
                    return;

                _autoScrollVersion = value;
                OnPropertyChanged(nameof(AutoScrollVersion));

                // Important: rows bind AutoScrollVersion through their own property.
                // We must notify AFTER updating the list value to avoid "previous item" scroll.
                NotifyRowsAutoScrollChanged();
            }
        }

        [DataSourceProperty]
        public string AutoScrollScope => $"EditorList_{_autoScrollScopeId}";

        /// <summary>
        /// Notify all rows that their auto-scroll bindings should be re-evaluated.
        /// This is intentionally list-driven to avoid row/list event ordering issues.
        /// </summary>
        protected void NotifyRowsAutoScrollChanged()
        {
            if (_headers == null || _headers.Count == 0)
                return;

            for (int i = 0; i < _headers.Count; i++)
            {
                var header = _headers[i];
                if (header == null)
                    continue;

                var rows = header.Rows;
                if (rows == null || rows.Count == 0)
                    continue;

                for (int r = 0; r < rows.Count; r++)
                    rows[r]?.NotifyAutoScrollChanged();
            }
        }
    }
}
