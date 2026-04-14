using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Framework.Runtime;
using TaleWorlds.Library;

namespace Retinues.Interface.Components
{
    [SafeClass]
    /// <summary>
    /// A UI button that binds to a controller action with optional dynamic label, sprite and gates.
    /// </summary>
    public sealed class Button<TArg> : EventListenerVM
    {
        private readonly ControllerAction<TArg> _action;
        private readonly Func<TArg> _arg;

        private readonly UIEvent[] _refreshEvents;

        private readonly string _label;
        private readonly Func<string> _labelFactory;

        private readonly string _sprite;
        private readonly Func<string> _spriteFactory;

        private readonly string _color;
        private readonly Func<string> _colorFactory;

        private readonly string _hoverColor;

        private readonly Func<string> _brushFactory;

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
        private string _resolvedBrush;
        private string _baseColorResolved;

        private bool _isHovered;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Construct a button with a single refresh event and optional static label.
        /// </summary>
        public Button(
            ControllerAction<TArg> action,
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
                hoverColor: null,
                allowGate: null,
                allowGateTooltip: null,
                visibilityGate: null,
                shouldRefresh: null
            ) { }

        /// <summary>
        /// Construct a button with detailed configuration including factories and gates.
        /// </summary>
        public Button(
            ControllerAction<TArg> action,
            Func<TArg> arg,
            UIEvent[] refresh,
            string label = null,
            Func<string> labelFactory = null,
            string sprite = null,
            Func<string> spriteFactory = null,
            string color = null,
            Func<string> colorFactory = null,
            string hoverColor = null,
            Func<bool> allowGate = null,
            Tooltip allowGateTooltip = null,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null,
            Func<string> brushFactory = null
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

            _hoverColor = hoverColor;

            _allowGate = allowGate;
            _allowGateTooltip = allowGateTooltip;

            _visibilityGate = visibilityGate;

            _shouldRefresh = shouldRefresh;

            _brushFactory = brushFactory;

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
        public string Brush => _resolvedBrush;

        [DataSourceProperty]
        public Tooltip Tooltip => _tooltip;

        /// <summary>
        /// Execute the bound action if the button is enabled.
        /// </summary>
        [DataSourceMethod]
        public void Execute()
        {
            if (!_isEnabled)
            {
                return;
            }

            _action?.Execute(_arg());
        }

        /// <summary>
        /// Begin hover state and apply hover color if applicable.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteHoverBegin()
        {
            if (_isHovered)
                return;
            _isHovered = true;

            ApplyHoverColorIfNeeded();
            OnPropertyChanged(nameof(Color));
        }

        /// <summary>
        /// End hover state and restore base color if applicable.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteHoverEnd()
        {
            if (!_isHovered)
                return;
            _isHovered = false;

            ApplyHoverColorIfNeeded();
            OnPropertyChanged(nameof(Color));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Event Wiring                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Handle global UI events and request property notifications when needed.
        /// </summary>
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

            if (_brushFactory != null)
            {
                context.RequestNotify(this, nameof(Brush));
            }

            if (
                _colorFactory != null
                || !string.IsNullOrEmpty(_color)
                || !string.IsNullOrEmpty(_hoverColor)
            )
            {
                context.RequestNotify(this, nameof(Color));
            }
        }

        /// <summary>
        /// Determine whether this button cares about the given UI event.
        /// </summary>
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

        /// <summary>
        /// Recompute visibility, enablement, tooltip and resolved visuals.
        /// </summary>
        private void Recompute()
        {
            _isVisible = _visibilityGate == null || _visibilityGate();

            _resolvedLabel = _labelFactory != null ? _labelFactory() : _label;
            _resolvedSprite = _spriteFactory != null ? _spriteFactory() : _sprite;
            _baseColorResolved = _colorFactory != null ? _colorFactory() : _color;
            _resolvedColor = _baseColorResolved;
            _resolvedBrush = _brushFactory?.Invoke() ?? string.Empty;

            if (!_isVisible)
            {
                _isEnabled = false;
                _tooltip = null;
                ApplyHoverColorIfNeeded();
                return;
            }

            var gateOk = _allowGate == null || _allowGate();

            if (!gateOk || _action == null)
            {
                _isEnabled = false;
                _tooltip = !gateOk ? _allowGateTooltip : null;
                ApplyHoverColorIfNeeded();
                return;
            }

            var arg = _arg();

            _isEnabled = _action.Allow(arg);
            _tooltip = _action.Tooltip(arg);

            ApplyHoverColorIfNeeded();
        }

        /// <summary>
        /// Apply hover color to the resolved color when appropriate.
        /// </summary>
        private void ApplyHoverColorIfNeeded()
        {
            var color = _baseColorResolved;

            if (!string.IsNullOrEmpty(_hoverColor) && _isHovered && _isEnabled)
                color = _hoverColor;

            if (!string.IsNullOrEmpty(color) && color[0] != '#')
                color = "#" + color;

            _resolvedColor = color;
        }
    }
}
