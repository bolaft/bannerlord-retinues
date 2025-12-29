using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Editor.Events;
using Retinues.Framework.Runtime;
using TaleWorlds.Library;

namespace Retinues.UI.VM
{
    /// <summary>
    /// Small helper VM that adapts an EditorAction into a single bindable button model:
    /// - IsEnabled: enabled state
    /// - Tooltip: enabled or blocking reason (EditorAction semantics)
    /// - Execute(): runs the action if allowed
    ///
    /// Optional visuals:
    /// - Label: text shown by text buttons
    /// - Sprite + Color: shown by sprite buttons
    ///
    /// The VM recomputes its cached values when one of the specified UI events fires.
    /// This avoids duplicating CanX/TooltipX/ExecuteX triplets across VMs.
    /// </summary>
    [SafeClass]
    public sealed class Button<TArg> : EventListenerVM
    {
        private readonly EditorAction<TArg> _action;
        private readonly Func<TArg> _arg;

        private readonly UIEvent[] _refreshEvents;

        private readonly string _label;
        private readonly Func<string> _labelFactory;

        private readonly string _sprite;
        private readonly Func<string> _spriteFactory;

        private readonly string _color;
        private readonly Func<string> _colorFactory;

        private readonly Func<bool> _allowGate;
        private readonly Tooltip _allowGateTooltip;

        private readonly Func<bool> _visibilityGate;

        private readonly Func<bool> _shouldRefresh;

        private bool _isEnabled;
        private Tooltip _tooltip;
        private bool _isVisible = true;

        private string _resolvedLabel;
        private string _resolvedSprite;
        private string _resolvedColor;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Button(
            EditorAction<TArg> action,
            Func<TArg> arg,
            UIEvent refresh,
            string label = null
        )
            : this(
                action: action,
                arg: arg,
                refresh: [refresh],
                label: label,
                labelFactory: null,
                sprite: null,
                spriteFactory: null,
                color: null,
                colorFactory: null,
                allowGate: null,
                allowGateTooltip: null,
                visibilityGate: null,
                shouldRefresh: null
            ) { }

        public Button(
            EditorAction<TArg> action,
            Func<TArg> arg,
            UIEvent[] refresh,
            string label = null,
            Func<string> labelFactory = null,
            string sprite = null,
            Func<string> spriteFactory = null,
            string color = null,
            Func<string> colorFactory = null,
            Func<bool> allowGate = null,
            Tooltip allowGateTooltip = null,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null
        )
        {
            _action = action;
            _arg = arg ?? (() => default);

            _refreshEvents = refresh ?? [];

            _label = label;
            _labelFactory = labelFactory;

            _sprite = sprite;
            _spriteFactory = spriteFactory;

            _color = color;
            _colorFactory = colorFactory;

            _allowGate = allowGate;
            _allowGateTooltip = allowGateTooltip;

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
        public bool IsEnabled => _isEnabled;

        [DataSourceProperty]
        public string Label => _resolvedLabel;

        [DataSourceProperty]
        public string Sprite => _resolvedSprite;

        [DataSourceProperty]
        public string Color => _resolvedColor;

        [DataSourceProperty]
        public Tooltip Tooltip => _tooltip;

        [DataSourceMethod]
        public void Execute()
        {
            if (!_isEnabled)
            {
                return;
            }

            _action?.Execute(_arg());
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Event Wiring                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal override void __OnGlobalEvent(EventManager.Context context, UIEvent e)
        {
            base.__OnGlobalEvent(context, e);

            if (context == null)
            {
                return;
            }

            if (!WantsEvent(e))
            {
                return;
            }

            if (_shouldRefresh != null && !_shouldRefresh())
            {
                return;
            }

            Recompute();

            context.RequestNotify(this, nameof(IsVisible));
            context.RequestNotify(this, nameof(IsEnabled));
            context.RequestNotify(this, nameof(Tooltip));

            if (_labelFactory != null)
            {
                context.RequestNotify(this, nameof(Label));
            }

            if (_spriteFactory != null)
            {
                context.RequestNotify(this, nameof(Sprite));
            }

            if (_colorFactory != null)
            {
                context.RequestNotify(this, nameof(Color));
            }
        }

        private bool WantsEvent(UIEvent e)
        {
            for (int i = 0; i < _refreshEvents.Length; i++)
            {
                if (_refreshEvents[i] == e)
                {
                    return true;
                }
            }

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void Recompute()
        {
            _isVisible = _visibilityGate == null || _visibilityGate();

            _resolvedLabel = _labelFactory != null ? _labelFactory() : _label;
            _resolvedSprite = _spriteFactory != null ? _spriteFactory() : _sprite;
            _resolvedColor = _colorFactory != null ? _colorFactory() : _color;

            if (!string.IsNullOrEmpty(_resolvedColor) && _resolvedColor[0] != '#')
                _resolvedColor = "#" + _resolvedColor;

            var gateOk = _allowGate == null || _allowGate();

            if (!_isVisible || !gateOk || _action == null)
            {
                _isEnabled = false;
                _tooltip = !gateOk ? _allowGateTooltip : null;
                return;
            }

            var arg = _arg();

            _isEnabled = _action.Allow(arg);
            _tooltip = _action.Tooltip(arg);
        }
    }
}
