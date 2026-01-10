using System;
using System.Collections.Generic;
using Retinues.Editor.Events;
using Retinues.Framework.Runtime;
using Retinues.UI.VM;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers
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
    public sealed class EditorAction<TArg>(string name)
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

        private sealed class CacheEntry
        {
            public TextObject Reason;
            public Tooltip Tooltip;
            public bool Computed;
        }

        private int _cacheBurstId = int.MinValue;
        private readonly Dictionary<TArg, CacheEntry> _burstCache = new();
        private readonly CacheEntry _nullBurstCache = new(); // for arg == null

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
        //                         Setup                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorAction<TArg> ExecuteWith(Action<TArg> execute)
        {
            _execute = execute;
            return this;
        }

        public EditorAction<TArg> AddCondition(Func<TArg, bool> test, TextObject reason)
        {
            _baseConditions.Add(new Condition(null, test, reason));
            return this;
        }

        public EditorAction<TArg> AddCondition(
            Func<TArg, bool> test,
            Func<TArg, TextObject> reasonFactory
        )
        {
            _baseConditions.Add(new Condition(null, test, reasonFactory));
            return this;
        }

        public EditorAction<TArg> AddCondition(
            Func<EditorState, bool> applies,
            Func<TArg, bool> test,
            TextObject reason
        )
        {
            _baseConditions.Add(new Condition(applies, test, reason));
            return this;
        }

        public EditorAction<TArg> AddCondition(
            Func<EditorState, bool> applies,
            Func<TArg, bool> test,
            Func<TArg, TextObject> reasonFactory
        )
        {
            _baseConditions.Add(new Condition(applies, test, reasonFactory));
            return this;
        }

        public EditorAction<TArg> AddCondition(
            Func<TArg, bool> test,
            Func<TextObject> reasonFactory
        )
        {
            _baseConditions.Add(new Condition(null, test, _ => reasonFactory()));
            return this;
        }

        public EditorAction<TArg> AddCondition(Func<TArg, bool> test, Func<string> reasonFactory)
        {
            _baseConditions.Add(new Condition(null, test, _ => new TextObject(reasonFactory())));
            return this;
        }

        public EditorAction<TArg> AddCondition(
            Func<EditorState, bool> applies,
            Func<TArg, bool> test,
            Func<TextObject> reasonFactory
        )
        {
            _baseConditions.Add(new Condition(applies, test, _ => reasonFactory()));
            return this;
        }

        public EditorAction<TArg> AddCondition(
            Func<EditorState, bool> applies,
            Func<TArg, bool> test,
            Func<string> reasonFactory
        )
        {
            _baseConditions.Add(new Condition(applies, test, _ => new TextObject(reasonFactory())));
            return this;
        }

        /// <summary>
        /// Default tooltip shown when the action is enabled.
        /// If the action is disabled, the blocking reason tooltip is shown instead.
        /// </summary>
        public EditorAction<TArg> DefaultTooltip(TextObject tooltip)
        {
            _defaultTooltip = tooltip;
            _defaultTooltipFactory = null;
            return this;
        }

        /// <summary>
        /// Default tooltip shown when the action is enabled, computed from the argument.
        /// If the action is disabled, the blocking reason tooltip is shown instead.
        /// </summary>
        public EditorAction<TArg> DefaultTooltip(Func<TArg, TextObject> tooltipFactory)
        {
            _defaultTooltip = null;
            _defaultTooltipFactory = tooltipFactory;
            return this;
        }

        public EditorAction<TArg> PreExecute(Action<TArg> pre)
        {
            _pre += pre;
            return this;
        }

        public EditorAction<TArg> PostExecute(Action<TArg> post)
        {
            _post += post;
            return this;
        }

        public EditorAction<TArg> Fire(UIEvent e)
        {
            _fireEvent = e;
            return this;
        }

        public EditorAction<TArg> WhenMode(EditorMode mode, Action<ActionModeSpec> spec)
        {
            if (!_modeOverrides.TryGetValue(mode, out var ov))
            {
                ov = new ModeOverrides();
                _modeOverrides[mode] = ov;
            }

            spec?.Invoke(new ActionModeSpec(ov));
            return this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Query                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        public TextObject Reason(TArg arg)
        {
            if (EventManager.IsInBurst)
            {
                return GetOrCompute(arg).Reason;
            }

            return ComputeReason(arg);
        }

        /// <summary>
        /// Tooltip for the action.
        /// - If disabled, returns the blocking reason.
        /// - If enabled, returns the optional default tooltip (if configured).
        /// - Otherwise returns null.
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

        public bool Execute(TArg arg)
        {
            var reason = Reason(arg);
            if (reason != null)
            {
                return false;
            }

            if (_execute == null)
            {
                Log.Warn($"EditorAction '{_name}' has no execute delegate.");
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

        public event Action<TArg> Executed;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

            public ActionModeSpec AddCondition(Func<TArg, bool> test, TextObject reason)
            {
                _ov.Conditions.Add(new Condition(null, test, reason));
                return this;
            }

            public ActionModeSpec AddCondition(
                Func<TArg, bool> test,
                Func<TArg, TextObject> reasonFactory
            )
            {
                _ov.Conditions.Add(new Condition(null, test, reasonFactory));
                return this;
            }

            public ActionModeSpec DefaultTooltip(TextObject tooltip)
            {
                _ov.DefaultTooltip = tooltip;
                _ov.DefaultTooltipFactory = null;
                return this;
            }

            public ActionModeSpec DefaultTooltip(Func<TArg, TextObject> tooltipFactory)
            {
                _ov.DefaultTooltip = null;
                _ov.DefaultTooltipFactory = tooltipFactory;
                return this;
            }

            public ActionModeSpec PreExecute(Action<TArg> pre)
            {
                _ov.Pre += pre;
                return this;
            }

            public ActionModeSpec PostExecute(Action<TArg> post)
            {
                _ov.Post += post;
                return this;
            }

            public ActionModeSpec OnExecuted(Action<TArg> executed)
            {
                _ov.Executed += executed;
                return this;
            }
        }
    }
}
