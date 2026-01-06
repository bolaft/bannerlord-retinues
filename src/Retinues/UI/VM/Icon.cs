using System;
using System.Drawing;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Events;
using Retinues.Framework.Runtime;
using TaleWorlds.Library;

namespace Retinues.UI.VM
{
    /// <summary>
    /// Small helper VM for "static" UI icons that need:
    /// - Tooltip: dynamic tooltip model
    /// - IsVisible: dynamic visibility
    /// - Sprite or Brush: icon rendering source
    ///
    /// The VM recomputes when one of the specified UI events fires.
    /// </summary>
    [SafeClass]
    public sealed class Icon : EventListenerVM
    {
        private readonly UIEvent[] _refreshEvents;

        private readonly Tooltip _tooltip;
        private readonly Func<Tooltip> _tooltipFactory;

        private readonly int _size;

        private readonly string _sprite;
        private readonly Func<string> _spriteFactory;

        private readonly string _brush;
        private readonly Func<string> _brushFactory;

        private readonly Func<bool> _visibilityGate;

        private readonly Func<bool> _shouldRefresh;

        private bool _isVisible = true;
        private Tooltip _resolvedTooltip;
        private string _resolvedSprite;
        private string _resolvedBrush;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Icon(
            Tooltip tooltip,
            UIEvent refresh,
            int size = 24,
            string sprite = null,
            Func<bool> visibilityGate = null,
            string brush = null,
            Func<string> brushFactory = null
        )
            : this(
                tooltip: tooltip,
                tooltipFactory: null,
                size: size,
                refresh: [refresh],
                sprite: sprite,
                spriteFactory: null,
                brush: brush,
                brushFactory: brushFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: null
            ) { }

        public Icon(
            Func<Tooltip> tooltipFactory,
            UIEvent refresh,
            string sprite = null,
            int size = 24,
            Func<bool> visibilityGate = null,
            string brush = null,
            Func<string> brushFactory = null
        )
            : this(
                tooltip: null,
                tooltipFactory: tooltipFactory,
                size: size,
                refresh: [refresh],
                sprite: sprite,
                spriteFactory: null,
                brush: brush,
                brushFactory: brushFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: null
            ) { }

        // overloads that take a sprite factory
        public Icon(
            Tooltip tooltip,
            UIEvent refresh,
            Func<string> spriteFactory,
            int size = 24,
            Func<bool> visibilityGate = null,
            string brush = null,
            Func<string> brushFactory = null
        )
            : this(
                tooltip: tooltip,
                tooltipFactory: null,
                size: size,
                refresh: [refresh],
                sprite: null,
                spriteFactory: spriteFactory,
                brush: brush,
                brushFactory: brushFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: null
            ) { }

        public Icon(
            Func<Tooltip> tooltipFactory,
            UIEvent refresh,
            Func<string> spriteFactory,
            int size = 24,
            Func<bool> visibilityGate = null,
            string brush = null,
            Func<string> brushFactory = null
        )
            : this(
                tooltip: null,
                tooltipFactory: tooltipFactory,
                size: size,
                refresh: [refresh],
                sprite: null,
                spriteFactory: spriteFactory,
                brush: brush,
                brushFactory: brushFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: null
            ) { }

        public Icon(
            Tooltip tooltip,
            UIEvent[] refresh,
            string sprite = null,
            int size = 24,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null,
            string brush = null,
            Func<string> brushFactory = null
        )
            : this(
                tooltip: tooltip,
                tooltipFactory: null,
                size: size,
                refresh: refresh,
                spriteFactory: null,
                sprite: sprite,
                brush: brush,
                brushFactory: brushFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: shouldRefresh
            ) { }

        public Icon(
            Func<Tooltip> tooltipFactory,
            UIEvent[] refresh,
            string sprite = null,
            int size = 24,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null,
            string brush = null,
            Func<string> brushFactory = null
        )
            : this(
                tooltip: null,
                tooltipFactory: tooltipFactory,
                size: size,
                refresh: refresh,
                sprite: sprite,
                spriteFactory: null,
                brush: brush,
                brushFactory: brushFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: shouldRefresh
            ) { }

        // array-based overloads that take a sprite factory
        public Icon(
            Tooltip tooltip,
            UIEvent[] refresh,
            Func<string> spriteFactory,
            int size = 24,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null,
            string brush = null,
            Func<string> brushFactory = null
        )
            : this(
                tooltip: tooltip,
                tooltipFactory: null,
                size: size,
                refresh: refresh,
                sprite: null,
                spriteFactory: spriteFactory,
                brush: brush,
                brushFactory: brushFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: shouldRefresh
            ) { }

        public Icon(
            Func<Tooltip> tooltipFactory,
            UIEvent[] refresh,
            Func<string> spriteFactory,
            int size = 24,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null,
            string brush = null,
            Func<string> brushFactory = null
        )
            : this(
                tooltip: null,
                tooltipFactory: tooltipFactory,
                size: size,
                refresh: refresh,
                sprite: null,
                spriteFactory: spriteFactory,
                brush: brush,
                brushFactory: brushFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: shouldRefresh
            ) { }

        private Icon(
            Tooltip tooltip,
            Func<Tooltip> tooltipFactory,
            int size,
            UIEvent[] refresh,
            string sprite,
            Func<string> spriteFactory,
            string brush,
            Func<string> brushFactory,
            Func<bool> visibilityGate,
            Func<bool> shouldRefresh
        )
        {
            _tooltip = tooltip;
            _tooltipFactory = tooltipFactory;

            _size = size;

            _sprite = sprite;
            _spriteFactory = spriteFactory;

            _brush = brush;
            _brushFactory = brushFactory;

            _refreshEvents = refresh ?? Array.Empty<UIEvent>();

            _visibilityGate = visibilityGate;

            _shouldRefresh = shouldRefresh;

            Recompute();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IsVisible => _isVisible;

        [DataSourceProperty]
        public int Size => _size;

        [DataSourceProperty]
        public Tooltip Tooltip => _resolvedTooltip;

        [DataSourceProperty]
        public string Sprite => _resolvedSprite;

        [DataSourceProperty]
        public string Brush => _resolvedBrush;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Event Wiring                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal override void __OnGlobalEvent(EventManager.Context context, UIEvent e)
        {
            base.__OnGlobalEvent(context, e);

            if (context == null)
                return;

            if (!WantsEvent(e))
                return;

            if (_shouldRefresh != null && !_shouldRefresh())
                return;

            Recompute();

            context.RequestNotify(this, nameof(IsVisible));
            context.RequestNotify(this, nameof(Tooltip));
            context.RequestNotify(this, nameof(Sprite));
            context.RequestNotify(this, nameof(Brush));
        }

        private bool WantsEvent(UIEvent e)
        {
            for (int i = 0; i < _refreshEvents.Length; i++)
            {
                if (_refreshEvents[i] == e)
                    return true;
            }

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void Recompute()
        {
            _isVisible = _visibilityGate == null || _visibilityGate();

            if (!_isVisible)
            {
                _resolvedTooltip = null;
                _resolvedSprite = null;
                _resolvedBrush = null;
                return;
            }

            _resolvedTooltip = _tooltipFactory != null ? _tooltipFactory() : _tooltip;

            _resolvedSprite = _spriteFactory != null ? _spriteFactory() : _sprite;
            _resolvedBrush = _brushFactory != null ? _brushFactory() : _brush;
        }
    }
}
