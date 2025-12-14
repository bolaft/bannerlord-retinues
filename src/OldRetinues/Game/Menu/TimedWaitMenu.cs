using System;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
# if BL12
using TaleWorlds.CampaignSystem.Overlay;
# endif

namespace OldRetinues.Game.Menu
{
    /// <summary>
    /// Generic "wait N in-game hours" game-menu flow.
    /// </summary>
    [SafeClass]
    public static class TimedWaitMenu
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         State                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string _menuId;
        private static string _returnMenuId;
        private static float _targetHours;
        private static float _progressHours;
        private static float _nextWholeHour = 1f;
        private static int _runSeq = 0;
        private static Action _onCompleted;
        private static Action _onAborted;
        private static Action<float> _onWholeHour;
        private static bool _kickUnpauseOnce;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Start a timed wait from the current game menu.
        /// </summary>
        public static void Start(
            CampaignGameStarter starter,
            string idSuffix,
            string title,
            float durationHours,
            Action onCompleted,
            Action onAborted,
# if BL13
            GameMenu.MenuOverlayType overlay = GameMenu.MenuOverlayType.None,
# else
            GameOverlays.MenuOverlayType overlay = GameOverlays.MenuOverlayType.None,
# endif
            Action<float> onWholeHour = null
        )
        {
            if (starter is null)
                throw new ArgumentNullException(nameof(starter));
            if (durationHours <= 0f)
                durationHours = 0.01f;

            Log.Debug(
                $"TimedWaitMenu.Start called: {idSuffix}, {durationHours:0.##}h, title='{title}'"
            );

            _menuId = $"ret_wait_{idSuffix}_{++_runSeq}";
            _targetHours = durationHours;
            _progressHours = 0f;
            _onCompleted = onCompleted;
            _onAborted = onAborted;
            _onWholeHour = onWholeHour;
            _kickUnpauseOnce = true;

            // Capture where we were BEFORE switching into our wait menu.
            _returnMenuId =
                Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? GuessFallbackMenu();

            AddOrReplaceWaitMenu(starter, title, overlay);
            OpenWaitMenu();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Menu Registration                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void AddOrReplaceWaitMenu(
            CampaignGameStarter starter,
            string title,
# if BL13
            GameMenu.MenuOverlayType overlay
# else
            GameOverlays.MenuOverlayType overlay
# endif
        )
        {
            Log.Debug($"TimedWaitMenu.AddOrReplaceWaitMenu called: {_menuId}, title='{title}'");
            // Vanilla-style wait menu with a per-tick callback and targeted hours.
            starter.AddWaitGameMenu(
                _menuId,
                title,
                WaitMenu_OnInit,
                WaitMenu_OnCondition,
                WaitMenu_OnConsequence,
                WaitMenu_OnTick,
                GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption,
                overlay,
                _targetHours
            );

            // Cancel ("Stop waiting").
            starter.AddGameMenuOption(
                _menuId,
                "ret_wait_cancel",
                L.S("cancel", "Cancel"),
                WaitMenu_Cancel_OnCondition,
                WaitMenu_Cancel_OnConsequence,
                isLeave: true,
                index: 0 // ensures it appears just before the Leave entry);
            );
        }

        private static void OpenWaitMenu()
        {
            Log.Debug($"TimedWaitMenu.OpenWaitMenu called: {_menuId}");
            GameMenu.SwitchToMenu(_menuId);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Menu Flow (Handlers)               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void WaitMenu_OnInit(MenuCallbackArgs args)
        {
            Log.Debug($"TimedWaitMenu.WaitMenu_OnInit called: {_menuId}");

            if (PlayerEncounter.Current != null)
                PlayerEncounter.Current.IsPlayerWaiting = true;

            _nextWholeHour = 1f;
        }

        private static bool WaitMenu_OnCondition(MenuCallbackArgs args) => true;

        private static void WaitMenu_OnTick(MenuCallbackArgs args, CampaignTime dt)
        {
            // Ensure unpaused state on next tick after starting wait.
            if (_kickUnpauseOnce)
            {
                _kickUnpauseOnce = false;
                EnsureUnpaused(); // one-time nudge so the new wait doesn’t start paused
            }

            _progressHours += (float)dt.ToHours;

            // Fire whole-hour ticks: 1.0, 2.0, 3.0, ...
            while (
                _onWholeHour != null
                && _progressHours >= _nextWholeHour
                && _nextWholeHour <= _targetHours
            )
            {
                _onWholeHour(_nextWholeHour); // e.g. 1, 2, 3 ...
                _nextWholeHour += 1f;
            }

            // Update progress bar (0.0 to 1.0)
            float progress =
                _targetHours > 0f ? (float)Math.Min(1.0, _progressHours / _targetHours) : 1f;
            args.MenuContext?.GameMenu?.SetProgressOfWaitingInMenu(progress);

            // Check for completion
            if (_progressHours >= _targetHours)
                Finish(completed: true);
        }

        private static void WaitMenu_OnConsequence(MenuCallbackArgs args)
        {
            Log.Debug($"TimedWaitMenu.WaitMenu_OnConsequence called: {_menuId}");
            // No-op; we finish from Tick (above) when time elapses.
        }

        private static bool WaitMenu_Cancel_OnCondition(MenuCallbackArgs args) => true;

        private static void WaitMenu_Cancel_OnConsequence(MenuCallbackArgs args)
        {
            Log.Debug($"TimedWaitMenu.WaitMenu_Cancel_OnConsequence called: {_menuId}");
            Finish(completed: false);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Finalization                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void Finish(bool completed)
        {
            Log.Debug($"TimedWaitMenu.Finish called: {_menuId}, completed={completed}");

            // Snapshot current run to detect a restart inside the callback.
            var prevRunSeq = _runSeq;
            var prevMenuId = _menuId;
            var fallback = string.IsNullOrEmpty(_returnMenuId)
                ? GuessFallbackMenu()
                : _returnMenuId;

            try
            {
                // 1) Fire the caller's callback FIRST (it may call TimedWaitMenu.Start again).
                if (completed)
                {
                    _onCompleted?.Invoke();
                }
                else
                {
                    _onAborted?.Invoke();
                }

                // 2) If a new wait started during the callback, _runSeq changed.
                bool restarted = _runSeq != prevRunSeq || _menuId != prevMenuId;
                if (restarted)
                {
                    // A new wait has already opened its own menu and set all static state.
                    // Do NOT switch away, and do NOT wipe the new state.
                    return;
                }

                // 3) No restart → we can safely end waiting and switch back.
                if (PlayerEncounter.Current != null)
                    PlayerEncounter.Current.IsPlayerWaiting = false;

                if (!string.IsNullOrEmpty(fallback))
                    GameMenu.SwitchToMenu(fallback);
            }
            finally
            {
                // 4) Cleanup ONLY if we did not restart (static state still belongs to this run).
                if (_runSeq == prevRunSeq && _menuId == prevMenuId)
                {
                    _menuId = _returnMenuId = null;
                    _onCompleted = _onAborted = null;
                    _targetHours = _progressHours = 0f;
                    _onWholeHour = null;
                    _nextWholeHour = 1f;
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Utilities                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string GuessFallbackMenu()
        {
            Log.Debug("TimedWaitMenu.GuessFallbackMenu called.");
            var s = Settlement.CurrentSettlement;
            if (s != null)
            {
                if (s.IsTown)
                    return "town";
                if (s.IsCastle)
                    return "castle";
                if (s.IsVillage)
                    return "village";
                if (s.IsHideout)
                    return "hideout_place";
            }
            return "town";
        }

        private static void EnsureUnpaused()
        {
            var c = Campaign.Current;
            if (c == null)
                return;

            // If paused or in a stop-like state, kick back into waiting fast-forward.
            if (
                c.TimeControlMode == CampaignTimeControlMode.Stop
                || c.TimeControlMode == CampaignTimeControlMode.FastForwardStop
            )
            {
                c.TimeControlMode = CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime;
            }
        }
    }
}
