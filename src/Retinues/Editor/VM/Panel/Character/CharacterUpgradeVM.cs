using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Character
{
    /// <summary>
    /// Character upgrade card.
    /// </summary>
    public class CharacterUpgradeVM(WCharacter character) : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WCharacter _character = character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Main                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => _character.Name;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteSelect()
        {
            foreach (var character in State.Faction.Troops)
            {
                if (character == _character)
                {
                    State.Character = character;
                    return; // Is of the same faction, no need to change further.
                }
            }

            // Different faction, launch editor for the new character.
            var faction =
                State.Mode == EditorMode.Player && _character.InCustomTree
                    ? _character.AssignedMapFaction
                    : _character.Culture;

            EditorLauncher.Launch(
                State.Mode == EditorMode.Player
                    ? EditorLaunchArgs.Player(faction: faction, character: _character)
                    : EditorLaunchArgs.Universal(faction: faction, character: _character)
            );
        }
    }
}
