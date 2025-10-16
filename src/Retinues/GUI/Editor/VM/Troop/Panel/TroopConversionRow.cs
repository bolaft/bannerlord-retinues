using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    [SafeClass]
    public sealed class TroopConversionRowVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly TroopScreenVM Screen;
        public readonly WCharacter Source;

        public TroopConversionRowVM(TroopScreenVM screen, WCharacter source)
        {
            Log.Info("Building TroopConversionRowVM...");

            Screen = screen;
            Source = source;
        }

        public void Initialize()
        {
            Log.Info("Initializing TroopConversionRowVM...");

            // Subscribe to events
            void Refresh()
            {
                OnPropertyChanged(nameof(HasPendingConversions));
                OnPropertyChanged(nameof(ConversionCost));
                OnPropertyChanged(nameof(CanRecruit));
                OnPropertyChanged(nameof(CanRelease));
                OnPropertyChanged(nameof(TargetDisplay));
                OnPropertyChanged(nameof(OriginDisplay));
            }
            EventManager.ConversionChange.Register(Refresh);
            EventManager.TroopChange.Register(Refresh);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private TroopPanelVM Panel => Screen?.TroopPanel;
        private WCharacter SelectedTroop => Screen?.TroopList?.Selection?.Troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Helper
        private int PendingAmount => Panel?.GetStagedConversions(Source, SelectedTroop) ?? 0;

        [DataSourceProperty]
        public int ConversionCost =>
            PendingAmount * TroopRules.ConversionCostPerUnit(SelectedTroop);

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string ButtonApplyConversionsText =>
            L.S("ret_apply_conversions_button_text", "Convert");

        [DataSourceProperty]
        public string ButtonClearConversionsText =>
            L.S("ret_clear_conversions_button_text", "Clear");

        [DataSourceProperty]
        public string OriginDisplay =>
            $"{Format.Crop(Source?.Name, 40)} ({Panel?.GetVirtualCount(Source) ?? 0})";

        [DataSourceProperty]
        public string TargetDisplay =>
            $"{Format.Crop(SelectedTroop?.Name, 40)} ({Panel?.GetVirtualCount(SelectedTroop) ?? 0}/{Panel?.RetinueCap ?? 0})";

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool CanRecruit => (Panel?.GetMaxStageable(Source, SelectedTroop) ?? 0) > 0;

        [DataSourceProperty]
        public bool CanRelease => (Panel?.GetVirtualCount(SelectedTroop) ?? 0) > 0;

        [DataSourceProperty]
        public bool HasPendingConversions => PendingAmount > 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteRecruit()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(Source, L.S("action_convert", "convert"))
                == false
            )
                return; // Conversion not allowed in current context

            Panel.StageConversion(Source, SelectedTroop, BatchInput());
        }

        [DataSourceMethod]
        public void ExecuteRelease()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(Source, L.S("action_convert", "convert"))
                == false
            )
                return; // Conversion not allowed in current context

            Panel.StageConversion(SelectedTroop, Source, BatchInput());
        }
    }
}
