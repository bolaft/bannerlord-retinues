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
        public Tooltip ScrollToTopTooltip =>
            new(L.S("scroll_to_top_tooltip", "Scroll to top"));

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
            }
        }

        [DataSourceProperty]
        public string AutoScrollScope => $"EditorList_{_autoScrollScopeId}";
    }
}
