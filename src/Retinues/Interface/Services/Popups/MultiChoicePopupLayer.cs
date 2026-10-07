using System;
using Retinues.Framework.Runtime;
using Retinues.Framework.Modules.Versions;
using Retinues.Utilities;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ScreenSystem;
#if !BL13 && !BL14
using TaleWorlds.GauntletUI.Data;
#endif

namespace Retinues.Interface.Services.Popups
{
    internal static class MultiChoicePopupLayer
    {
#if BL13 || BL14
        private static GauntletMovieIdentifier _movie;
#else
        private static IGauntletMovie _movie;
#endif

        private static GauntletLayer _layer;
        private static ScreenBase _owner;

        [StaticClearAction]
        public static void Close()
        {
            var layer = _layer;
            var movie = _movie;
            var owner = _owner;
            _layer = null;
            _movie = null;
            _owner = null;
            if (layer == null)
                return;

            try
            {
                layer.IsFocusLayer = false;
                layer.InputRestrictions.ResetInputRestrictions();
                ScreenManager.TryLoseFocus(layer);
                if (movie != null)
                    layer.ReleaseMovie(movie);
            }
            catch (Exception e)
            {
                Log.Exception(e, "MultiChoicePopupLayer.Close failed.");
            }

            finally
            {
                // A mission or another screen may have become the top screen since Show.
                try { owner?.RemoveLayer(layer); }
                catch (Exception e) { Log.Exception(e, "MultiChoicePopupLayer.RemoveLayer failed."); }
            }
        }

        internal static void Show(MultiChoicePopupVM vm)
        {
            Close();
            var screen = ScreenManager.TopScreen;
            if (screen == null)
                return;

#if BL13 || BL14
            _layer = new GauntletLayer("RetinuesMultiChoicePopup", 500, shouldClear: false);
#else
            _layer = new GauntletLayer(500, "RetinuesMultiChoicePopup", shouldClear: false);
#endif
            _owner = screen;
            try
            {
                _layer.InputRestrictions.SetInputRestrictions();
                _layer.IsFocusLayer = true;
                screen.AddLayer(_layer);
                var movieName = GameVersion.IsAtLeast14() ? "MultiChoicePopup" : "MultiChoicePopup_BL13";
                _movie = _layer.LoadMovie(movieName, vm);
                ScreenManager.TrySetFocus(_layer);
            }
            catch
            {
                Close();
                throw;
            }
        }
    }
}
