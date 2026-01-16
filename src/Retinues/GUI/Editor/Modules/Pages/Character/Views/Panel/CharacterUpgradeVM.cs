using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
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

        [DataSourceProperty]
        public bool IsEnabled => _character.IsFactionTroop || Settings.EnableUniversalEditor;

        [DataSourceMethod]
        public void ExecuteSelect()
        {
            if (!IsEnabled)
                return;

            // Upgrade targets can cross mode boundaries:
            // - Custom tree troops must open in Player mode
            // - Non-custom troops must open in Universal mode
            var desiredMode = _character.IsFactionTroop ? EditorMode.Player : EditorMode.Universal;

            // Fast path: same mode + same selected faction -> just select.
            if (desiredMode == State.Mode)
            {
                foreach (var character in State.Faction.Troops)
                {
                    if (character == _character)
                    {
                        State.Character = character;
                        return;
                    }
                }
            }

            // Otherwise we must relaunch, potentially switching editor mode.
            if (desiredMode == EditorMode.Player)
            {
                var faction = _character.AssignedMapFaction;

                EditorLauncher.Launch(
                    EditorLaunchArgs.Player(faction: faction, character: _character)
                );
                return;
            }

            EditorLauncher.Launch(
                EditorLaunchArgs.Universal(faction: _character.Culture, character: _character)
            );
        }
    }
}
