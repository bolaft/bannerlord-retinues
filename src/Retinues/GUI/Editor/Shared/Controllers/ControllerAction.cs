using System;
using System.Collections.Generic;
using Retinues.Framework.Runtime;
using Retinues.GUI.Components;
using Retinues.GUI.Editor.Events;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Shared.Controllers
{
    /// <summary>
    /// Reusable UI action: conditions, tooltip, execution, and post-success signaling.
    /// Supports mode-specific rules via State.Mode.
    ///
    /// Performance: while EventManager is inside a burst, Allow/Reason/Tooltip
    /// are cached so the same action+arg is only evaluated once per burst.
    /// Outside bursts, no caching is performed.
    /// </summary>
    [SafeClass]
    public sealed class ControllerAction<TArg>(string name)
    {
        private readonly string _name = name ?? "";
        private readonly List<Condition> _baseConditions = [];
        private readonly Dictionary<EditorMode, ModeOverrides> _modeOverrides = [];

        private Action<TArg> _execute;
        private UIEvent? _fireEvent;

        private Action<TArg> _pre;
        private Action<TArg> _post;

        private TextObject _defaultTooltip;
        private Func<TArg, TextObject> _defaultTooltipFactory;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cache                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Cache entry for a specific (burst, arg) pair.
        /// </summary>
        private sealed class CacheEntry
        {
            public TextObject Reason;
            public Tooltip Tooltip;
            public bool Computed;
        }

        private int _cacheBurstId = int.MinValue;
        private readonly Dictionary<TArg, CacheEntry> _burstCache = [];
        private readonly CacheEntry _nullBurstCache = new(); // for arg == null

        /// <summary>
        /// Ensure the burst cache is initialized for the current burst.
        /// </summary>
        private void EnsureBurstCache()
        {
            if (!EventManager.IsInBurst)
            {
                return;
            }

            var burstId = EventManager.CurrentBurstId;
            if (burstId == _cacheBurstId)
            {
                return;
            }

            _cacheBurstId = burstId;
            _burstCache.Clear();
            _nullBurstCache.Computed = false;
            _nullBurstCache.Reason = null;
            _nullBurstCache.Tooltip = null;
        }

        /// <summary>
        /// Get or compute the cache entry for the given argument in the current burst.
        /// </summary>
        private CacheEntry GetOrCompute(TArg arg)
        {
            // Only cache during bursts.
            if (!EventManager.IsInBurst)
            {
                return ComputeNoCache(arg);
            }

            EnsureBurstCache();

            CacheEntry entry;
            if (arg is null)
            {
                entry = _nullBurstCache;
            }
            else
            {
                if (!_burstCache.TryGetValue(arg, out entry))
                {
                    entry = new CacheEntry();
                    _burstCache[arg] = entry;
                }
            }

            if (entry.Computed)
            {
                return entry;
            }

            // Compute once for this (burst, arg)
            var reason = ComputeReason(arg);
            var tooltip = ComputeTooltipFromReason(arg, reason);

            entry.Reason = reason;
            entry.Tooltip = tooltip;
            entry.Computed = true;

            return entry;
        }

        /// <summary>
        /// Compute a cache entry without storing it (for non-burst calls).
        /// </summary>
        private static CacheEntry ComputeNoCache(TArg arg)
        {
            // no caching outside a burst; compute new entry each call
            return new CacheEntry
            {
                Computed = false,
                Reason = null,
                Tooltip = null,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Execution                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Set the execution delegate for this action.
        /// </summary>
        public ControllerAction<TArg> ExecuteWith(Action<TArg> execute)
        {
            _execute = execute;
            return this;
        }

        /// <summary>
        /// Add a pre-execute handler invoked before the main execute.
        /// </summary>
        public ControllerAction<TArg> PreExecute(Action<TArg> pre)
        {
            _pre += pre;
            return this;
        }

        /// <summary>
        /// Add a post-execute handler invoked after the main execute.
        /// </summary>
        public ControllerAction<TArg> PostExecute(Action<TArg> post)
        {
            _post += post;
            return this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Condition                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Configure mode-specific overrides for this action.
        /// </summary>
        public ControllerAction<TArg> WhenMode(EditorMode mode, Action<ActionModeSpec> spec)
        {
            if (!_modeOverrides.TryGetValue(mode, out var ov))
            {
                ov = new ModeOverrides();
                _modeOverrides[mode] = ov;
            }

            spec?.Invoke(new ActionModeSpec(ov));
            return this;
        }

        /// <summary>
        /// Core AddCondition implementation. All overloads delegate here to
        /// centralize construction of the Condition instance.
        /// </summary>
        private ControllerAction<TArg> AddConditionCore(
            Func<EditorState, bool> applies,
            Func<TArg, bool> test,
            Func<TArg, TextObject> reasonFactory
        )
        {
            _baseConditions.Add(new Condition(applies, test, reasonFactory));
            return this;
        }

        /// <summary>
        /// Add a condition that must pass for the action to be allowed.
        /// </summary>
        public ControllerAction<TArg> AddCondition(Func<TArg, bool> test, TextObject reason) =>
            AddConditionCore(null, test, _ => reason);

        /// <summary>
        /// Add a condition that must pass for the action to be allowed.
        /// </summary>
        public ControllerAction<TArg> AddCondition(
            Func<TArg, bool> test,
            Func<TArg, TextObject> reasonFactory
        ) => AddConditionCore(null, test, reasonFactory);

        /// <summary>
        /// Add a condition that applies only when the provided state predicate is true.
        /// </summary>
        public ControllerAction<TArg> AddCondition(
            Func<EditorState, bool> applies,
            Func<TArg, bool> test,
            TextObject reason
        ) => AddConditionCore(applies, test, _ => reason);

        /// <summary>
        /// Add a condition with a reason factory that applies only when the state predicate is true.
        /// </summary>
        public ControllerAction<TArg> AddCondition(
            Func<EditorState, bool> applies,
            Func<TArg, bool> test,
            Func<TArg, TextObject> reasonFactory
        ) => AddConditionCore(applies, test, reasonFactory);

        /// <summary>
        /// Add a condition using a parameterless reason factory.
        /// </summary>
        public ControllerAction<TArg> AddCondition(
            Func<TArg, bool> test,
            Func<TextObject> reasonFactory
        ) => AddConditionCore(null, test, _ => reasonFactory());

        /// <summary>
        /// Add a condition using a string-producing reason factory.
        /// </summary>
        public ControllerAction<TArg> AddCondition(
            Func<TArg, bool> test,
            Func<string> reasonFactory
        ) => AddConditionCore(null, test, _ => new TextObject(reasonFactory()));

        /// <summary>
        /// Add a state-specific condition using a parameterless reason factory.
        /// </summary>
        public ControllerAction<TArg> AddCondition(
            Func<EditorState, bool> applies,
            Func<TArg, bool> test,
            Func<TextObject> reasonFactory
        ) => AddConditionCore(applies, test, _ => reasonFactory());

        /// <summary>
        /// Add a state-specific condition using a string-producing reason factory.
        /// </summary>
        public ControllerAction<TArg> AddCondition(
            Func<EditorState, bool> applies,
            Func<TArg, bool> test,
            Func<string> reasonFactory
        ) => AddConditionCore(applies, test, _ => new TextObject(reasonFactory()));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Default Tooltip                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Default tooltip shown when the action is enabled.
        /// If the action is disabled, the blocking reason tooltip is shown instead.
        /// </summary>
        public ControllerAction<TArg> DefaultTooltip(TextObject tooltip)
        {
            _defaultTooltip = tooltip;
            _defaultTooltipFactory = null;
            return this;
        }

        /// <summary>
        /// Default tooltip shown when the action is enabled, computed from the argument.
        /// If the action is disabled, the blocking reason tooltip is shown instead.
        /// </summary>
        public ControllerAction<TArg> DefaultTooltip(Func<TArg, TextObject> tooltipFactory)
        {
            _defaultTooltip = null;
            _defaultTooltipFactory = tooltipFactory;
            return this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Event                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Configure an event to fire after successful execution.
        /// </summary>
        public ControllerAction<TArg> Fire(UIEvent e)
        {
            _fireEvent = e;
            return this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Reasons                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the action is allowed for the given argument.
        /// </summary>
        public bool Allow(TArg arg)
        {
            // Cached during burst
            if (EventManager.IsInBurst)
            {
                return GetOrCompute(arg).Reason == null;
            }

            // No cache
            return ComputeReason(arg) == null;
        }

        /// <summary>
        /// Returns the blocking reason if the action is not allowed, otherwise null.
        /// </summary>
        public TextObject Reason(TArg arg)
        {
            if (EventManager.IsInBurst)
            {
                return GetOrCompute(arg).Reason;
            }

            return ComputeReason(arg);
        }

        /// <summary>
        /// Returns the tooltip for the action based on enablement and defaults.
        /// </summary>
        public Tooltip Tooltip(TArg arg)
        {
            if (EventManager.IsInBurst)
            {
                return GetOrCompute(arg).Tooltip;
            }

            var reason = ComputeReason(arg);
            return ComputeTooltipFromReason(arg, reason);
        }

        /// <summary>
        /// Compute the blocking reason for the action, or null if allowed.
        /// </summary>
        private TextObject ComputeReason(TArg arg)
        {
            // 1) Baseline conditions
            var reason = Evaluate(_baseConditions, arg);
            if (reason != null)
                return reason;

            // 2) Mode-specific conditions
            if (_modeOverrides.TryGetValue(BaseController.State.Mode, out var ov))
            {
                reason = Evaluate(ov.Conditions, arg);
                if (reason != null)
                    return reason;
            }

            return null;
        }

        /// <summary>
        /// Compute the tooltip based on the reason and defaults.
        /// </summary>
        private Tooltip ComputeTooltipFromReason(TArg arg, TextObject reason)
        {
            if (reason != null)
            {
                return new Tooltip(reason);
            }

            var mode = BaseController.State.Mode;

            if (_modeOverrides.TryGetValue(mode, out var ov))
            {
                var t =
                    ov.DefaultTooltipFactory != null
                        ? ov.DefaultTooltipFactory(arg)
                        : ov.DefaultTooltip;
                if (t != null)
                    return new Tooltip(t);
            }

            var baseTip =
                _defaultTooltipFactory != null ? _defaultTooltipFactory(arg) : _defaultTooltip;
            return baseTip != null ? new Tooltip(baseTip) : null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Execute                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Execute the action if allowed; returns true on success.
        /// </summary>
        public bool Execute(TArg arg)
        {
            var reason = Reason(arg);
            if (reason != null)
            {
                return false;
            }

            if (_execute == null)
            {
                Log.Warning($"ControllerAction '{_name}' has no execute delegate.");
                return false;
            }

            var mode = BaseController.State.Mode;

            _pre?.Invoke(arg);
            if (_modeOverrides.TryGetValue(mode, out var ov))
            {
                ov.Pre?.Invoke(arg);
            }

            _execute(arg);

            if (_modeOverrides.TryGetValue(mode, out ov))
            {
                ov.Post?.Invoke(arg);
            }
            _post?.Invoke(arg);

            if (_fireEvent != null)
            {
                EventManager.Fire(_fireEvent.Value);
            }

            ov?.Executed?.Invoke(arg);
            Executed?.Invoke(arg);

            return true;
        }

        /// <summary>
        /// Fired when the action has been successfully executed.
        /// </summary>
        public event Action<TArg> Executed;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Evaluate the given conditions against the argument, returning the first blocking reason found.
        /// </summary>
        private static TextObject Evaluate(List<Condition> conditions, TArg arg)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];

                if (c.Applies != null && !c.Applies(BaseController.State))
                    continue;

                if (!c.Test(arg))
                    return c.GetReason(arg);
            }

            return null;
        }

        /// <summary>
        /// Condition for action allowance.
        /// </summary>
        public readonly struct Condition
        {
            public readonly Func<EditorState, bool> Applies;
            public readonly Func<TArg, bool> Test;
            public readonly TextObject Reason;
            public readonly Func<TArg, TextObject> ReasonFactory;

            public Condition(
                Func<EditorState, bool> applies,
                Func<TArg, bool> test,
                TextObject reason
            )
            {
                Applies = applies;
                Test = test;
                Reason = reason;
                ReasonFactory = null;
            }

            public Condition(
                Func<EditorState, bool> applies,
                Func<TArg, bool> test,
                Func<TArg, TextObject> reasonFactory
            )
            {
                Applies = applies;
                Test = test;
                Reason = null;
                ReasonFactory = reasonFactory;
            }

            public TextObject GetReason(TArg arg) =>
                ReasonFactory != null ? ReasonFactory(arg) : Reason;
        }

        /// <summary>
        /// Mode-specific overrides for an action.
        /// </summary>
        public sealed class ModeOverrides
        {
            public readonly List<Condition> Conditions = [];
            public Action<TArg> Pre;
            public Action<TArg> Post;
            public Action<TArg> Executed;

            public TextObject DefaultTooltip;
            public Func<TArg, TextObject> DefaultTooltipFactory;
        }

        /// <summary>
        /// Mode-specific action customization.
        /// </summary>
        public sealed class ActionModeSpec(ModeOverrides ov)
        {
            private readonly ModeOverrides _ov = ov;

            private ActionModeSpec AddConditionCore(
                Func<EditorState, bool> applies,
                Func<TArg, bool> test,
                Func<TArg, TextObject> reasonFactory
            )
            {
                _ov.Conditions.Add(new Condition(applies, test, reasonFactory));
                return this;
            }

            /// <summary>
            /// Add a condition to the mode-specific overrides.
            /// </summary>
            public ActionModeSpec AddCondition(Func<TArg, bool> test, TextObject reason) =>
                AddConditionCore(null, test, _ => reason);

            /// <summary>
            /// Add a condition with a reason factory to the mode-specific overrides.
            /// </summary>
            public ActionModeSpec AddCondition(
                Func<TArg, bool> test,
                Func<TArg, TextObject> reasonFactory
            ) => AddConditionCore(null, test, reasonFactory);

            /// <summary>
            /// Set the default tooltip for this action in the current mode.
            /// </summary>
            public ActionModeSpec DefaultTooltip(TextObject tooltip)
            {
                _ov.DefaultTooltip = tooltip;
                _ov.DefaultTooltipFactory = null;
                return this;
            }

            /// <summary>
            /// Set the default tooltip factory for this action in the current mode.
            /// </summary>
            public ActionModeSpec DefaultTooltip(Func<TArg, TextObject> tooltipFactory)
            {
                _ov.DefaultTooltip = null;
                _ov.DefaultTooltipFactory = tooltipFactory;
                return this;
            }

            /// <summary>
            /// Add a pre-execute handler for this mode.
            /// </summary>
            public ActionModeSpec PreExecute(Action<TArg> pre)
            {
                _ov.Pre += pre;
                return this;
            }

            /// <summary>
            /// Add a post-execute handler for this mode.
            /// </summary>
            public ActionModeSpec PostExecute(Action<TArg> post)
            {
                _ov.Post += post;
                return this;
            }

            /// <summary>
            /// Add a handler invoked when the action is executed in this mode.
            /// </summary>
            public ActionModeSpec OnExecuted(Action<TArg> executed)
            {
                _ov.Executed += executed;
                return this;
            }
        }
    }
}
