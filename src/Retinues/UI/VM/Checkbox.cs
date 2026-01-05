using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Editor.Events;
using Retinues.Framework.Runtime;
using TaleWorlds.Library;

namespace Retinues.UI.VM
{
    /// <summary>
    /// Small helper VM that adapts an EditorAction<bool> into a single bindable checkbox model:
    /// - IsSelected: current checked state (from a getter)
    /// - IsEnabled: whether toggling to the next state is allowed
    /// - Tooltip: enabled tooltip or blocking reason (EditorAction semantics)
    /// - IsVisible: optional visibility gate
    ///
    /// Supports optional allow/tooltip overrides for cases where the VM owns extra UI rules
    /// (e.g. per-equipment constraints) without having to duplicate boilerplate in the owner VM.
    /// </summary>
    [SafeClass]
    public sealed class Checkbox : EventListenerVM
    {
        private readonly EditorAction<bool> _action;
        private readonly Func<bool> _getSelected;

        private readonly UIEvent[] _refreshEvents;

        private readonly Func<bool> _allowGate;
        private readonly Tooltip _allowGateTooltip;

        private readonly Func<bool> _visibilityGate;

        private readonly Func<bool> _shouldRefresh;

        private readonly Func<bool, bool> _allowOverride;
        private readonly Func<bool, Tooltip> _tooltipOverride;

        private bool _isVisible = true;
        private bool _isSelected;
        private bool _isEnabled;
        private Tooltip _tooltip;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Checkbox(EditorAction<bool> action, Func<bool> getSelected, UIEvent refresh)
            : this(
                action: action,
                getSelected: getSelected,
                refresh: [refresh],
                allowGate: null,
                allowGateTooltip: null,
                visibilityGate: null,
                shouldRefresh: null,
                allowOverride: null,
                tooltipOverride: null
            ) { }

        public Checkbox(
            EditorAction<bool> action,
            Func<bool> getSelected,
            UIEvent[] refresh,
            Func<bool> allowGate = null,
            Tooltip allowGateTooltip = null,
            Func<bool> visibilityGate = null,
            Func<bool> shouldRefresh = null,
            Func<bool, bool> allowOverride = null,
            Func<bool, Tooltip> tooltipOverride = null
        )
        {
            _action = action;
            _getSelected = getSelected ?? (() => false);

            _refreshEvents = refresh ?? [];

            _allowGate = allowGate;
            _allowGateTooltip = allowGateTooltip;

            _visibilityGate = visibilityGate;

            _shouldRefresh = shouldRefresh;

            _allowOverride = allowOverride;
            _tooltipOverride = tooltipOverride;

            Recompute();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IsVisible => _isVisible;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected)
                    return;

                if (!_isVisible)
                    return;

                if (!GateAllows())
                {
                    RecomputeAndNotify();
                    return;
                }

                if (!IsAllowed(value))
                {
                    RecomputeAndNotify();
                    return;
                }

                // Execute is authoritative (re-checks conditions internally).
                _action?.Execute(value);

                RecomputeAndNotify();
            }
        }

        [DataSourceProperty]
        public bool IsEnabled => _isEnabled;

        [DataSourceProperty]
        public Tooltip Tooltip => _tooltip;

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
            context.RequestNotify(this, nameof(IsSelected));
            context.RequestNotify(this, nameof(IsEnabled));
            context.RequestNotify(this, nameof(Tooltip));
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

        private void RecomputeAndNotify()
        {
            Recompute();

            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(Tooltip));
        }

        private bool GateAllows() => _allowGate == null || _allowGate();

        private bool IsAllowed(bool targetValue)
        {
            var ok = true;

            if (_action != null)
                ok &= _action.Allow(targetValue);

            if (_allowOverride != null)
                ok &= _allowOverride(targetValue);

            return ok;
        }

        private Tooltip ComputeTooltip(
            bool targetValue,
            bool enabledByAction,
            bool enabledByOverride
        )
        {
            // Gate failure: always show the gate tooltip (if any).
            if (!GateAllows())
                return _allowGateTooltip;

            // If our override blocks, prefer the override tooltip.
            if (!enabledByOverride)
            {
                var t = _tooltipOverride?.Invoke(targetValue);
                if (t != null)
                    return t;
            }

            // Prefer action tooltip (reason or default tooltip).
            var actionTip = _action?.Tooltip(targetValue);
            if (actionTip != null)
                return actionTip;

            // Fallback to override tooltip when enabled but action has no tooltip.
            return _tooltipOverride?.Invoke(targetValue);
        }

        private void Recompute()
        {
            _isVisible = _visibilityGate == null || _visibilityGate();

            _isSelected = _getSelected();

            if (!_isVisible)
            {
                _isEnabled = false;
                _tooltip = null;
                return;
            }

            if (!GateAllows())
            {
                _isEnabled = false;
                _tooltip = _allowGateTooltip;
                return;
            }

            var next = !_isSelected;

            var enabledByAction = _action == null || _action.Allow(next);
            var enabledByOverride = _allowOverride == null || _allowOverride(next);

            _isEnabled = enabledByAction && enabledByOverride;
            _tooltip = ComputeTooltip(next, enabledByAction, enabledByOverride);
        }
    }
}
