using System;
using Retinues.Wrappers.Characters;
using Retinues.Wrappers.Factions;
using TaleWorlds.Library;

namespace Retinues.Editor
{
    /// <summary>
    /// Base ViewModel with shared editor state (faction and character).
    /// </summary>
    public abstract class BaseStatefulVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Global State                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static IBaseFaction _faction;
        private static WCharacter _character;

        protected static event Action<IBaseFaction> FactionChanged;
        protected static event Action<WCharacter> CharacterChanged;

        /// <summary>
        /// Gets or sets the current editor faction.
        /// Setting this property raises FactionChanged and clears the character.
        /// </summary>
        protected static IBaseFaction StateFaction
        {
            get => _faction;
            set
            {
                if (ReferenceEquals(value, _faction))
                {
                    return;
                }

                _faction = value;
                _character = null;

                FactionChanged?.Invoke(_faction);
                CharacterChanged?.Invoke(_character);
            }
        }

        /// <summary>
        /// Gets or sets the current editor character.
        /// Setting this property raises CharacterChanged.
        /// </summary>
        protected static WCharacter StateCharacter
        {
            get => _character;
            set
            {
                if (ReferenceEquals(value, _character))
                {
                    return;
                }

                _character = value;
                CharacterChanged?.Invoke(_character);
            }
        }

        /// <summary>
        /// Clears faction and character state and notifies listeners.
        /// </summary>
        protected static void ResetState()
        {
            _faction = null;
            _character = null;

            FactionChanged?.Invoke(_faction);
            CharacterChanged?.Invoke(_character);
        }
    }
}
