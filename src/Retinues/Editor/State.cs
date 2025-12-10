using System.Collections.Generic;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Editor
{
    [SafeClass]
    public class State
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static State Instance;

        public State()
        {
            Instance = this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          State                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Faction ━━━━━━━ */

        private IBaseFaction _faction;

        /// <summary>
        /// Current editor faction. Setting this clears the character and
        /// fires a global Faction event.
        /// </summary>
        public IBaseFaction Faction
        {
            get => _faction;
            set
            {
                if (ReferenceEquals(value, _faction))
                    return;

                if (value == null)
                    return;

                // Set the faction.
                _faction = value;

                // Reset the character to the first available troop.
                Character = GetFirstAvailableTroop(value);

                // Notify listeners.
                EventManager.Fire(UIEvent.Faction, EventScope.Global);
            }
        }

        /* ━━━━━━━ Character ━━━━━━ */

        private WCharacter _character;

        /// <summary>
        /// Current editor character. Setting this fires a local Troop event
        /// so only the selected row and dependent VMs update.
        /// </summary>
        public WCharacter Character
        {
            get => _character;
            set
            {
                if (ReferenceEquals(value, _character))
                    return;

                if (value == null)
                    return;

                // Set the character.
                _character = value;

                // Notify listeners.
                EventManager.Fire(UIEvent.Troop, EventScope.Local);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Reset                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clears faction and character state and notifies listeners.
        /// </summary>
        public void Reset()
        {
            _faction = null;
            _character = null;

            // Default faction: use the main hero's culture if available.
            IBaseFaction faction = null;

            var hero = Hero.MainHero;
            if (hero?.CharacterObject != null)
            {
                var wrappedHero = WCharacter.Get(hero.CharacterObject);
                faction = wrappedHero?.Culture;
            }

            Faction = faction;

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.Faction, EventScope.Global);
                EventManager.Fire(UIEvent.Troop, EventScope.Global);
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the first available troop from the faction rosters.
        /// </summary>
        WCharacter GetFirstAvailableTroop(IBaseFaction faction)
        {
            var rosters = new List<List<WCharacter>>
            {
                faction.RosterRetinues,
                faction.RosterElite,
                faction.RosterBasic,
                faction.RosterMilitia,
                faction.RosterCaravan,
                faction.RosterVillager,
                faction.RosterBandit,
                faction.RosterCivilian,
            };

            foreach (var roster in rosters)
            {
                if (roster == null)
                    continue;

                foreach (var troop in roster)
                {
                    if (troop != null)
                        return troop;
                }
            }

            return null;
        }
    }
}
