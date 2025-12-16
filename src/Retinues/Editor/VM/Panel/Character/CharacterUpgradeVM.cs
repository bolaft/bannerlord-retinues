using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Screen;
using Retinues.Model.Characters;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Character
{
    /// <summary>
    /// Character upgrade card.
    /// </summary>
    public class CharacterUpgradeVM(WCharacter character) : BaseVM
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
            EditorLauncher.Launch(_character);
        }
    }
}
