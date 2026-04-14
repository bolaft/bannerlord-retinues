using System;
using Retinues.Framework.Runtime;
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

        [StaticClearAction]
        public static void Close()
        {
            if (_layer == null)
                return;

            try
            {
                if (_movie != null)
                {
                    _layer.ReleaseMovie(_movie);
                    _movie = null;
                }

                ScreenManager.TopScreen?.RemoveLayer(_layer);
            }
            catch (Exception e)
            {
                Log.Exception(e, "MultiChoicePopupLayer.Close failed.");
            }

            _layer = null;
        }

        internal static void Show(MultiChoicePopupVM vm)
        {
            var screen = ScreenManager.TopScreen;
            if (screen == null)
                return;

#if BL13 || BL14
            _layer = new GauntletLayer("RetinuesMultiChoicePopup", 500, shouldClear: false);
#else
            _layer = new GauntletLayer(500, "RetinuesMultiChoicePopup", shouldClear: false);
#endif

            _layer.InputRestrictions.SetInputRestrictions();
            _layer.IsFocusLayer = true;
            screen.AddLayer(_layer);
            ScreenManager.TrySetFocus(_layer);
            _movie = _layer.LoadMovie("MultiChoicePopup", vm);
        }
    }
}
