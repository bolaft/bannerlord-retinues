using System;
using Retinues.Framework.Modules.Versions;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
#if BL12
using TaleWorlds.GauntletUI.Data;
#endif

namespace Retinues.Interface.Services.Popups
{
    internal static class BugReportPopupLayer
    {
#if BL13 || BL14
        private static GauntletMovieIdentifier _movie;
#else
        private static IGauntletMovie _movie;
#endif
        private static GauntletLayer _layer;
        private static ScreenBase _owner;
        private static GameStateManager _stateManager;
        private static BugReportPopupVM _vm;
        private static readonly object PauseOwner = new();
        private static int _escapeSuppressionFrames;
        internal static bool IsOpen => _layer != null;
        internal static bool SuppressEditorShortcuts => IsOpen || _escapeSuppressionFrames > 0;

        internal static void Show(Func<string, string> submit)
        {
            if (IsOpen) return;
            var screen = ScreenManager.TopScreen;
            if (screen == null) throw new InvalidOperationException("No screen is available for the bug report dialog.");
            _owner = screen;
            _vm = new BugReportPopupVM(submit, Close);
            try
            {
#if BL13 || BL14
                _layer = new GauntletLayer("RetinuesBugReportPopup", 500, shouldClear: false);
#else
                _layer = new GauntletLayer(500, "RetinuesBugReportPopup", shouldClear: false);
#endif
                _stateManager = Game.Current?.GameStateManager;
                _stateManager?.RegisterActiveStateDisableRequest(PauseOwner);
                _layer.InputRestrictions.SetInputRestrictions();
                _layer.IsFocusLayer = true;
                screen.AddLayer(_layer);
#if BL12
                const string movie = "BugReportPopup";
#else
                var movie = GameVersion.IsAtLeast14() ? "BugReportPopup" : "BugReportPopup_BL13";
#endif
                _movie = _layer.LoadMovie(movie, _vm);
                ScreenManager.TrySetFocus(_layer);
            }
            catch { Close(); throw; }
        }

        internal static void Tick()
        {
            if (_escapeSuppressionFrames > 0) _escapeSuppressionFrames--;
            if (!IsOpen) return;
            if (_owner != ScreenManager.TopScreen) { Close(); return; }
            if (Input.IsKeyReleased(InputKey.Escape))
            {
                // The parent editor must not consume the same Escape and close as well.
                _escapeSuppressionFrames = 2;
                _vm.ExecuteCancel();
            }
        }

        [StaticClearAction]
        public static void Close()
        {
            var layer = _layer; var movie = _movie; var owner = _owner; var vm = _vm;
            var manager = _stateManager;
            _layer = null; _movie = null; _owner = null; _vm = null; _stateManager = null;
            try
            {
                if (layer != null)
                {
                    layer.IsFocusLayer = false;
                    layer.InputRestrictions.ResetInputRestrictions();
                    ScreenManager.TryLoseFocus(layer);
                    if (movie != null) layer.ReleaseMovie(movie);
                }
            }
            catch (Exception e) { Log.Exception(e, "BugReportPopupLayer.Close failed."); }
            finally
            {
                try { if (layer != null) owner?.RemoveLayer(layer); }
                catch (Exception e) { Log.Exception(e, "BugReportPopupLayer.RemoveLayer failed."); }
                finally
                {
                    manager?.UnregisterActiveStateDisableRequest(PauseOwner);
                    vm?.OnFinalize();
                }
            }
        }
    }
}
