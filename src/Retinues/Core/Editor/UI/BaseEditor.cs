using Retinues.Core.Editor.UI.VM;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI
{
    /// <summary>
    /// Base class for editor view models. Provides access to the editor screen and selected troop.
    /// </summary>
    public abstract class BaseEditor<TSelf>(EditorScreenVM screen) : ViewModel
        where TSelf : BaseEditor<TSelf>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected readonly EditorScreenVM _screen = screen;

        public EditorScreenVM Screen => _screen;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter SelectedTroop => _screen.SelectedTroop;
    }
}
