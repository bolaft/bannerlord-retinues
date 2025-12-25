using Retinues.Model;
using Retinues.Model.Characters;
using Retinues.Model.Equipments;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Editor
{
    /// <summary>
    /// Editor modes..
    /// </summary>
    public enum EditorMode
    {
        Universal = 0,
        Player = 1,
    }

    /// <summary>
    /// Optional launch parameters for the editor.
    /// Only one of Character/Clan/Culture should be set.
    /// </summary>
    public sealed class EditorLaunchArgs
    {
        public EditorMode Mode { get; } = EditorMode.Universal;
        public WCharacter Character { get; }
        public WHero Hero { get; }
        public WClan Clan { get; }
        public WCulture Culture { get; }

        public EditorLaunchArgs(WCharacter character) => Character = character;

        public EditorLaunchArgs(WHero hero) => Hero = hero;

        public EditorLaunchArgs(WClan clan) => Clan = clan;

        public EditorLaunchArgs(WCulture culture) => Culture = culture;

        public bool IsEmpty => Character == null && Hero == null && Clan == null && Culture == null;
    }

    [SafeClass]
    public class State
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The singleton instance of the editor state.
        /// </summary>
        private static State _instance;
        public static State Instance => _instance ??= new State();

        [StaticClearAction]
        public static void ClearInstance() => _instance = null;

        /// <summary>
        /// The current editor mode.
        /// </summary>
        public EditorMode Mode { get; private set; } = EditorMode.Universal;

        /// <summary>
        /// Whether the state is currently initializing.
        /// </summary>
        private readonly bool _isInitializing;

        /// <summary>
        /// Fires a UI event if not initializing.
        /// </summary>
        private void Fire(UIEvent e)
        {
            if (_isInitializing)
                return;
            EventManager.Fire(e, EventScope.Global);
        }

        /// <summary>
        /// Constructs a new editor state without launch arguments.
        /// </summary>
        public State()
            : this(null) { }

        /// <summary>
        /// Constructs a new editor state with the given launch arguments.
        /// </summary>
        public State(EditorLaunchArgs args)
        {
            Log.Info("Initializing new editor state");

            // Set the singleton instance.
            _instance = this;

            // Apply launch arguments.
            _isInitializing = true;
            ApplyLaunchArgs(args);
            _isInitializing = false;

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

        /// <summary>
        /// Resets the singleton instance with optional launch arguments.
        /// </summary>
        public static void Reset(EditorLaunchArgs args = null) => _instance = new State(args);

        /// <summary>
        /// Applies the given launch arguments to the state.
        /// </summary>
        private void ApplyLaunchArgs(EditorLaunchArgs args)
        {
            if (args == null || args.IsEmpty)
            {
                ApplyDefault();
                return;
            }

            // Set mode first.
            Mode = args.Mode;

            if (args.Character != null)
            {
                ApplyCharacter(args.Character);
                return;
            }

            if (args.Hero != null)
            {
                ApplyHero(args.Hero);
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

            Culture = hero.Culture;
            Clan = null;
            Faction = hero.Culture;

            Character = PickFirstTroop(Faction);
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private void ApplyHero(WHero hero)
        {
            Culture = hero.Culture;
            Clan = hero.Clan;
            Faction = hero.Clan;

            Character = PickFirstTroop(Faction);
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private void ApplyCharacter(WCharacter character)
        {
            Culture = character.Culture;
            Clan = null;
            Faction = Culture;

            Character = character;
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private void ApplyClan(WClan clan)
        {
            Culture = clan.Culture;
            Clan = clan;
            Faction = clan;

            Character = PickFirstTroop(Faction);
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private void ApplyCulture(WCulture culture)
        {
            Culture = culture;
            Clan = null;
            Faction = culture;

            Character = PickFirstTroop(Faction);
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private static WCharacter PickFirstTroop(IBaseFaction faction)
        {
            if (faction?.Troops == null)
                return null;

            foreach (var troop in faction.Troops)
            {
                if (troop != null)
                {
                    if (troop.IsHero && troop.Hero.IsDead)
                        continue; // Skip dead heroes.

                    return troop;
                }
            }

            return null;
        }

        private static MEquipment PickFirstEquipment(WCharacter character)
        {
            var equipments = character?.Editable?.Equipments;

            if (equipments == null || equipments.Count == 0)
                return null;

            foreach (var equipment in equipments)
            {
                if (equipment != null && equipment.IsCivilian == character.IsCivilian)
                    return equipment;
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
                if (value == _culture)
                    return;

                if (value == null)
                    return;

                _culture = value;
                Fire(UIEvent.CultureFaction);
            }
        }

        /* ━━━━━━━━━ Clan ━━━━━━━━━ */

        private WClan _clan;

        public WClan Clan
        {
            get => _clan;
            set
            {
                if (value == _clan)
                    return;

                // Allow null (launch modes can intentionally clear clan).
                _clan = value;
                Fire(UIEvent.ClanFaction);
            }
        }

        /* ━━━━━━━━ Faction ━━━━━━━ */

        private IBaseFaction _faction;

        public IBaseFaction Faction
        {
            get => _faction;
            set
            {
                if (value == _faction)
                    return;

                if (value == null)
                    return;

                _faction = value;

                Character = PickFirstTroop(_faction);

                Fire(UIEvent.Faction);
            }
        }

        /* ━━━━━━━ Character ━━━━━━ */

        private WCharacter _character;

        public WCharacter Character
        {
            get => _character ??= PickFirstTroop(_faction);
            set
            {
                if (value == _character)
                    return;

                if (value == null)
                    return;

                _character = value;

                Equipment = PickFirstEquipment(_character);

                Fire(UIEvent.Character);
            }
        }

        /* ━━━━━━━ Equipment ━━━━━━ */

        private MEquipment _equipment;

        public MEquipment Equipment
        {
            get => _equipment;
            set
            {
                if (value == _equipment)
                    return;

                if (value == null)
                    return;

                _equipment = value;
                Fire(UIEvent.Equipment);
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
                Fire(UIEvent.Slot);
            }
        }

        /* ━━━━━━━ Library ━━━━━━━ */

        private MLibrary.Item _libraryItem;

        public MLibrary.Item LibraryItem
        {
            get => _libraryItem;
            set
            {
                if (ReferenceEquals(value, _libraryItem))
                    return;

                _libraryItem = value;
                Fire(UIEvent.Library);
            }
        }
    }
}
