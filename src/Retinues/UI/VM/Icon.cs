using System;
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
    ///
    /// The VM recomputes when one of the specified UI events fires.
    /// </summary>
    [SafeClass]
    public sealed class Icon : EventListenerVM
    {
        private readonly UIEvent[] _refreshEvents;

        private readonly Tooltip _tooltip;
        private readonly Func<Tooltip> _tooltipFactory;
        private readonly string _sprite;
        private readonly Func<string> _spriteFactory;

        private readonly Func<bool> _visibilityGate;

        private readonly Func<bool> _shouldRefresh;

        private bool _isVisible = true;
        private Tooltip _resolvedTooltip;
        private string _resolvedSprite;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Icon(
            Tooltip tooltip,
            UIEvent refresh,
            string sprite = null,
            Func<bool> visibilityGate = null
        )
            : this(
                tooltip: tooltip,
                tooltipFactory: null,
                refresh: [refresh],
                sprite: sprite,
                spriteFactory: null,
                visibilityGate: visibilityGate,
                shouldRefresh: null
            ) { }

        public Icon(
            Func<Tooltip> tooltipFactory,
            UIEvent refresh,
            string sprite = null,
            Func<bool> visibilityGate = null
        )
            : this(
                tooltip: null,
                tooltipFactory: tooltipFactory,
                refresh: [refresh],
                sprite: sprite,
                spriteFactory: null,
                visibilityGate: visibilityGate,
                shouldRefresh: null
            ) { }

        // overloads that take a sprite factory
        public Icon(
            Tooltip tooltip,
            UIEvent refresh,
            Func<string> spriteFactory,
            Func<bool> visibilityGate = null
        )
            : this(
                tooltip: tooltip,
                tooltipFactory: null,
                refresh: [refresh],
                sprite: null,
                spriteFactory: spriteFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: null
            ) { }

        public Icon(
            Func<Tooltip> tooltipFactory,
            UIEvent refresh,
            Func<string> spriteFactory,
            Func<bool> visibilityGate = null
        )
            : this(
                tooltip: null,
                tooltipFactory: tooltipFactory,
                refresh: [refresh],
                sprite: null,
                spriteFactory: spriteFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: null
            ) { }

        public Icon(
            Tooltip tooltip,
            UIEvent[] refresh,
            string sprite = null,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null
        )
            : this(
                tooltip: tooltip,
                tooltipFactory: null,
                refresh: refresh,
                sprite: sprite,
                spriteFactory: null,
                visibilityGate: visibilityGate,
                shouldRefresh: shouldRefresh
            ) { }

        public Icon(
            Func<Tooltip> tooltipFactory,
            UIEvent[] refresh,
            string sprite = null,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null
        )
            : this(
                tooltip: null,
                tooltipFactory: tooltipFactory,
                refresh: refresh,
                sprite: sprite,
                spriteFactory: null,
                visibilityGate: visibilityGate,
                shouldRefresh: shouldRefresh
            ) { }

        // array-based overloads that take a sprite factory
        public Icon(
            Tooltip tooltip,
            UIEvent[] refresh,
            Func<string> spriteFactory,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null
        )
            : this(
                tooltip: tooltip,
                tooltipFactory: null,
                refresh: refresh,
                sprite: null,
                spriteFactory: spriteFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: shouldRefresh
            ) { }

        public Icon(
            Func<Tooltip> tooltipFactory,
            UIEvent[] refresh,
            Func<string> spriteFactory,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null
        )
            : this(
                tooltip: null,
                tooltipFactory: tooltipFactory,
                refresh: refresh,
                sprite: null,
                spriteFactory: spriteFactory,
                visibilityGate: visibilityGate,
                shouldRefresh: shouldRefresh
            ) { }

        private Icon(
            Tooltip tooltip,
            Func<Tooltip> tooltipFactory,
            UIEvent[] refresh,
            string sprite,
            Func<string> spriteFactory,
            Func<bool> visibilityGate,
            Func<bool> shouldRefresh
        )
        {
            _tooltip = tooltip;
            _tooltipFactory = tooltipFactory;
            _sprite = sprite;
            _spriteFactory = spriteFactory;

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
        public Tooltip Tooltip => _resolvedTooltip;

        [DataSourceProperty]
        public string Sprite => _resolvedSprite;

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
                return;
            }

            _resolvedTooltip = _tooltipFactory != null ? _tooltipFactory() : _tooltip;
            _resolvedSprite = _spriteFactory != null ? _spriteFactory() : _sprite;
        }
    }
}
