using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Encyclopedia.Manual;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Shared.Views
{
    /// <summary>
    /// Base panel ViewModel used in the editor GUI.
    /// </summary>
    public abstract class BasePanelVM : EventListenerVM
    {
        [DataSourceProperty]
        public virtual int Width => 820;

        [DataSourceProperty]
        public virtual HorizontalAlignment HorizontalAlignment => HorizontalAlignment.Right;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Manual                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly ControllerAction<bool> _openManualAction =
            new ControllerAction<bool>("OpenManual").ExecuteWith(_ => ManualLink.Open());

        [DataSourceProperty]
        public Button<bool> ShowManualButton { get; } =
            new(
                action: _openManualAction,
                arg: () => true,
                refresh: [],
                label: L.S("manual_button_label", "Open Manual")
            );
    }
}
