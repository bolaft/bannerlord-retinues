using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor
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
