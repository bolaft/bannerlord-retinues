using Retinues.Model.Characters;
using Retinues.Model.Equipments;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Editor
{
    [SafeClass]
    public class State
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static State _instance;
        public static State Instance => _instance ??= new State();

        public State()
        {
            Log.Info("Initializing new editor state");

            // Set the singleton instance.
            _instance = this;

            // Defaults
            Culture = WHero.Get(Hero.MainHero).Culture;
            Clan = WHero.Get(Hero.MainHero).Clan;
            Faction = Clan;

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.Faction, EventScope.Global);
                EventManager.Fire(UIEvent.Character, EventScope.Global);
            });
        }

        public static void Reset() => _instance = new State();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          State                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Culture ━━━━━━━ */

        private WCulture _culture;

        /// <summary>
        /// Current editor culture.
        /// </summary>
        public WCulture Culture
        {
            get => _culture;
            set
            {
                if (ReferenceEquals(value, _culture))
                    return;

                if (value == null)
                    return;

                // Set the culture.
                _culture = value;

                // Notify listeners.
                EventManager.Fire(UIEvent.CultureFaction, EventScope.Global);
            }
        }

        /* ━━━━━━━━━ Clan ━━━━━━━━━ */

        private WClan _clan;

        /// <summary>
        /// Current editor culture.
        /// </summary>
        public WClan Clan
        {
            get => _clan;
            set
            {
                if (ReferenceEquals(value, _clan))
                    return;

                if (value == null)
                    return;

                // Set the culture.
                _clan = value;

                // Notify listeners.
                EventManager.Fire(UIEvent.ClanFaction, EventScope.Global);
            }
        }

        /* ━━━━━━━━ Faction ━━━━━━━ */

        private IBaseFaction _faction;

        /// <summary>
        /// Current editor faction.
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
                foreach (var troop in _faction.Troops)
                {
                    if (troop != null)
                    {
                        Character = troop;
                        break;
                    }
                }

                // Notify listeners.
                EventManager.Fire(UIEvent.Faction, EventScope.Global);
            }
        }

        /* ━━━━━━━ Character ━━━━━━ */

        private WCharacter _character;

        /// <summary>
        /// Current editor character.
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

                // Pick the first equipment.
                Equipment = _character.EquipmentRoster.Get(0);

                // Notify listeners.
                EventManager.Fire(UIEvent.Character, EventScope.Global);
            }
        }

        /* ━━━━━━━ Equipment ━━━━━━ */

        private MEquipment _equipment;

        /// <summary>
        /// Current editor equipment.
        /// </summary>
        public MEquipment Equipment
        {
            get => _equipment;
            set
            {
                if (ReferenceEquals(value, _equipment))
                    return;

                if (value == null)
                    return;

                // Set the equipment.
                _equipment = value;

                // Notify listeners.
                EventManager.Fire(UIEvent.Equipment, EventScope.Global);
            }
        }

        /* ━━━━━━━━━ Slot ━━━━━━━━━ */

        private EquipmentIndex _slot = EquipmentIndex.Weapon0;

        /// <summary>
        /// Current editor equipment slot.
        /// </summary>
        public EquipmentIndex Slot
        {
            get => _slot;
            set
            {
                if (value == _slot)
                    return;

                // Set the slot.
                _slot = value;

                // Notify listeners.
                EventManager.Fire(UIEvent.Slot, EventScope.Global);
            }
        }
    }
}
