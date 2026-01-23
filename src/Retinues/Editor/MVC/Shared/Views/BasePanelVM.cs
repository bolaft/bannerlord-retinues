using Retinues.Editor.Events;
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
    }
}
