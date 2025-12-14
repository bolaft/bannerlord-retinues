using Retinues.Model.Characters;
using Retinues.Model.Equipments;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Editor
{
    /// <summary>
    /// Optional launch parameters for the editor.
    /// Only one of Character/Clan/Culture should be set.
    /// </summary>
    public sealed class EditorLaunchArgs
    {
        public WCharacter Character { get; }
        public WClan Clan { get; }
        public WCulture Culture { get; }

        public EditorLaunchArgs(WCharacter character) => Character = character;

        public EditorLaunchArgs(WClan clan) => Clan = clan;

        public EditorLaunchArgs(WCulture culture) => Culture = culture;

        public bool IsEmpty => Character == null && Clan == null && Culture == null;
    }

    [SafeClass]
    public class State
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static State _instance;
        public static State Instance => _instance ??= new State();

        public State()
            : this(null) { }

        public State(EditorLaunchArgs args)
        {
            Log.Info("Initializing new editor state");

            // Set the singleton instance.
            _instance = this;

            ApplyLaunchArgs(args);

            // Notify listeners.
            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.CultureFaction, EventScope.Global);
                EventManager.Fire(UIEvent.ClanFaction, EventScope.Global);
                EventManager.Fire(UIEvent.Faction, EventScope.Global);
                EventManager.Fire(UIEvent.Character, EventScope.Global);
                EventManager.Fire(UIEvent.Equipment, EventScope.Global);
                EventManager.Fire(UIEvent.Slot, EventScope.Global);
            });
        }

        public static void Reset(EditorLaunchArgs args = null) => _instance = new State(args);

        private void ApplyLaunchArgs(EditorLaunchArgs args)
        {
            if (args == null || args.IsEmpty)
            {
                ApplyDefault();
                return;
            }

            if (args.Character != null)
            {
                ApplyCharacter(args.Character);
                return;
            }

            if (args.Clan != null)
            {
                ApplyClan(args.Clan);
                return;
            }

            if (args.Culture != null)
            {
                ApplyCulture(args.Culture);
                return;
            }

            ApplyDefault();
        }

        private void ApplyDefault()
        {
            var hero = WHero.Get(Hero.MainHero);

            _culture = hero.Culture;
            _clan = hero.Clan;
            _faction = hero.Clan;

            _character = PickFirstTroop(_faction);
            _equipment = _character?.EquipmentRoster?.Get(0);

            _slot = EquipmentIndex.Weapon0;
        }

        private void ApplyCharacter(WCharacter character)
        {
            _character = character;

            _culture = character.Culture;
            _clan = null;
            _faction = _culture;

            _equipment = _character?.EquipmentRoster?.Get(0);

            _slot = EquipmentIndex.Weapon0;
        }

        private void ApplyClan(WClan clan)
        {
            _clan = clan;
            _faction = clan;

            // In Bannerlord, a clan is an IFaction, so it has a culture.
            // Your wrapper should expose it; if not, adjust this line to your API.
            _culture = clan.Culture;

            _character = PickFirstTroop(_faction);
            _equipment = _character?.EquipmentRoster?.Get(0);

            _slot = EquipmentIndex.Weapon0;
        }

        private void ApplyCulture(WCulture culture)
        {
            _culture = culture;
            _clan = null;
            _faction = culture;

            _character = PickFirstTroop(_faction);
            _equipment = _character?.EquipmentRoster?.Get(0);

            _slot = EquipmentIndex.Weapon0;
        }

        private static WCharacter PickFirstTroop(IBaseFaction faction)
        {
            if (faction?.Troops == null)
                return null;

            foreach (var troop in faction.Troops)
            {
                if (troop != null)
                    return troop;
            }

            return null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          State                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Culture ━━━━━━━ */

        private WCulture _culture;

        public WCulture Culture
        {
            get => _culture;
            set
            {
                if (ReferenceEquals(value, _culture))
                    return;

                if (value == null)
                    return;

                _culture = value;
                EventManager.Fire(UIEvent.CultureFaction, EventScope.Global);
            }
        }

        /* ━━━━━━━━━ Clan ━━━━━━━━━ */

        private WClan _clan;

        public WClan Clan
        {
            get => _clan;
            set
            {
                if (ReferenceEquals(value, _clan))
                    return;

                // Allow null (launch modes can intentionally clear clan).
                _clan = value;
                EventManager.Fire(UIEvent.ClanFaction, EventScope.Global);
            }
        }

        /* ━━━━━━━━ Faction ━━━━━━━ */

        private IBaseFaction _faction;

        public IBaseFaction Faction
        {
            get => _faction;
            set
            {
                if (ReferenceEquals(value, _faction))
                    return;

                if (value == null)
                    return;

                _faction = value;

                foreach (var troop in _faction.Troops)
                {
                    if (troop != null)
                    {
                        Character = troop;
                        break;
                    }
                }

                EventManager.Fire(UIEvent.Faction, EventScope.Global);
            }
        }

        /* ━━━━━━━ Character ━━━━━━ */

        private WCharacter _character;

        public WCharacter Character
        {
            get => _character;
            set
            {
                if (ReferenceEquals(value, _character))
                    return;

                if (value == null)
                    return;

                _character = value;

                Equipment = _character.EquipmentRoster.Get(0);

                EventManager.Fire(UIEvent.Character, EventScope.Global);
            }
        }

        /* ━━━━━━━ Equipment ━━━━━━ */

        private MEquipment _equipment;

        public MEquipment Equipment
        {
            get => _equipment;
            set
            {
                if (ReferenceEquals(value, _equipment))
                    return;

                if (value == null)
                    return;

                _equipment = value;
                EventManager.Fire(UIEvent.Equipment, EventScope.Global);
            }
        }

        /* ━━━━━━━━━ Slot ━━━━━━━━━ */

        private EquipmentIndex _slot = EquipmentIndex.Weapon0;

        public EquipmentIndex Slot
        {
            get => _slot;
            set
            {
                if (value == _slot)
                    return;

                _slot = value;
                EventManager.Fire(UIEvent.Slot, EventScope.Global);
            }
        }
    }
}
