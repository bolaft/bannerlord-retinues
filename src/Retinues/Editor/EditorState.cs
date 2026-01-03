using System;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Framework.Model.Exports;
using Retinues.Framework.Runtime;
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

        public EditorLaunchArgs(EditorMode mode = EditorMode.Universal)
        {
            Mode = mode;
        }

        public EditorLaunchArgs(WCharacter character, EditorMode mode = EditorMode.Universal)
        {
            Character = character;
            Mode = mode;
        }

        public EditorLaunchArgs(WHero hero, EditorMode mode = EditorMode.Universal)
        {
            Hero = hero;
            Mode = mode;
        }

        public EditorLaunchArgs(WClan clan, EditorMode mode = EditorMode.Universal)
        {
            Clan = clan;
            Mode = mode;
        }

        public EditorLaunchArgs(WCulture culture, EditorMode mode = EditorMode.Universal)
        {
            Culture = culture;
            Mode = mode;
        }

        public bool IsEmpty => Character == null && Hero == null && Clan == null && Culture == null;
    }

    [SafeClass]
    public class EditorState
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The singleton instance of the editor state.
        /// </summary>
        private static EditorState _instance;
        public static EditorState Instance => _instance ??= new EditorState();

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
            EventManager.Fire(e);
        }

        /// <summary>
        /// Constructs a new editor state without launch arguments.
        /// </summary>
        public EditorState()
            : this(null) { }

        /// <summary>
        /// Constructs a new editor state with the given launch arguments.
        /// </summary>
        public EditorState(EditorLaunchArgs args)
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
                EventManager.Fire(UIEvent.CultureFaction);
                EventManager.Fire(UIEvent.ClanFaction);
                EventManager.Fire(UIEvent.Faction);
                EventManager.Fire(UIEvent.Character);
                EventManager.Fire(UIEvent.Equipment);
                EventManager.Fire(UIEvent.Slot);
            });
        }

        /// <summary>
        /// Resets the singleton instance with optional launch arguments.
        /// </summary>
        public static void Reset(EditorLaunchArgs args = null) => _instance = new EditorState(args);

        /// <summary>
        /// Applies the given launch arguments to the state.
        /// </summary>
        private void ApplyLaunchArgs(EditorLaunchArgs args)
        {
            // Set mode first.
            Mode = args?.Mode ?? EditorMode.Universal;

            Log.Info($"Launch Mode: {Mode}.");

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

            if (Mode == EditorMode.Player)
            {
                var clan = hero?.Clan;
                var kingdom = hero?.Kingdom;

                // Player mode: Left banner is Clan.
                LeftBannerFaction = clan;

                if (IsPlayerKingdomRuler(hero, kingdom))
                {
                    // Player is a kingdom ruler: Right banner is Kingdom.
                    RightBannerFaction = kingdom;
                }
                else
                {
                    // Not a ruler: no right banner.
                    RightBannerFaction = null;
                }

                // Start on clan (not kingdom) by default.
                Faction = clan;
            }
            else
            {
                // Universal mode: Left banner is Culture, right banner is Clan (optional).
                var culture = hero?.Culture;

                LeftBannerFaction = culture;
                RightBannerFaction = null;
                Faction = culture;
            }

            Character = PickFirstTroop(Faction, Mode);
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private void ApplyHero(WHero hero)
        {
            if (hero == null)
            {
                ApplyDefault();
                return;
            }

            if (Mode == EditorMode.Player)
            {
                var clan = hero.Clan;
                var kingdom = hero.Kingdom;

                LeftBannerFaction = clan;
                RightBannerFaction = IsPlayerKingdomRuler(hero, kingdom) ? kingdom : null;

                Faction = clan;
            }
            else
            {
                LeftBannerFaction = hero.Culture;
                RightBannerFaction = hero.Clan;

                Faction = hero.Clan;
            }

            Character = PickFirstTroop(Faction, Mode);
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private void ApplyCharacter(WCharacter character)
        {
            if (character == null)
            {
                ApplyDefault();
                return;
            }

            // Character launch: keep universal semantics.
            LeftBannerFaction = character.Culture;
            RightBannerFaction = null;
            Faction = LeftBannerFaction;

            Character = character;
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private void ApplyClan(WClan clan)
        {
            if (clan == null)
            {
                ApplyDefault();
                return;
            }

            if (Mode == EditorMode.Player)
            {
                // Player mode rules:
                // - If not a kingdom ruler, force the player clan and hide the kingdom banner.
                // - If a kingdom ruler, allow selecting clans within that kingdom.
                var hero = WHero.Get(Hero.MainHero);
                var playerClan = hero?.Clan;
                var playerKingdom = hero?.Kingdom;

                if (!IsPlayerKingdomRuler(hero, playerKingdom))
                {
                    LeftBannerFaction = playerClan;
                    RightBannerFaction = null;
                    Faction = playerClan;
                }
                else
                {
                    // Clamp to the player's kingdom.
                    if (playerKingdom != null && clan.Base.Kingdom != playerKingdom.Base)
                        clan = playerClan;

                    LeftBannerFaction = clan;
                    RightBannerFaction = playerKingdom;
                    Faction = clan;
                }
            }
            else
            {
                // Universal: left is culture, right is clan, latest selection is faction.
                LeftBannerFaction = clan.Culture;
                RightBannerFaction = clan;
                Faction = clan;
            }

            Character = PickFirstTroop(Faction, Mode);
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private void ApplyCulture(WCulture culture)
        {
            if (culture == null)
            {
                ApplyDefault();
                return;
            }

            // Universal: left is culture, right is cleared, latest selection is faction.
            LeftBannerFaction = culture;
            RightBannerFaction = null;
            Faction = culture;

            Character = PickFirstTroop(Faction, Mode);
            Equipment = PickFirstEquipment(Character);

            Slot = EquipmentIndex.Weapon0;
        }

        private static WCharacter PickFirstTroop(IBaseFaction faction, EditorMode mode)
        {
            if (faction?.Troops == null)
                return null;

            foreach (var troop in faction.Troops)
            {
                if (troop == null)
                    continue;

                if (troop.IsHero && troop.Hero.IsDead)
                    continue; // Skip dead heroes.

                if (mode == EditorMode.Player)
                {
                    if (troop.IsHero)
                        continue;

                    if (!troop.InCustomTree)
                        continue;
                }
                else
                {
                    // Universal: no custom.
                    if (troop.InCustomTree)
                        continue;
                }

                return troop;
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

        /* ━━━━━━ Left Banner ━━━━━ */

        private IBaseFaction _leftBannerFaction;

        /// <summary>
        /// The faction displayed on the left banner.
        /// Universal: Culture. Player: Clan.
        /// </summary>
        public IBaseFaction LeftBannerFaction
        {
            get => _leftBannerFaction;
            set
            {
                if (value == _leftBannerFaction)
                    return;

                // Allow null (some launch modes intentionally clear banners).
                _leftBannerFaction = value;
                Fire(UIEvent.CultureFaction);
            }
        }

        /* ━━━━━ Right Banner ━━━━━ */

        private IBaseFaction _rightBannerFaction;

        /// <summary>
        /// The faction displayed on the right banner.
        /// Universal: Clan. Player: Kingdom (only when player is a kingdom ruler).
        /// </summary>
        public IBaseFaction RightBannerFaction
        {
            get => _rightBannerFaction;
            set
            {
                if (value == _rightBannerFaction)
                    return;

                // Allow null (player mode can intentionally hide right banner).
                _rightBannerFaction = value;
                Fire(UIEvent.ClanFaction);
            }
        }

        private static bool IsPlayerKingdomRuler(WHero hero, WKingdom kingdom)
        {
            if (hero == null || kingdom == null)
                return false;

            // Kingdom rulership is defined by the kingdom leader being the hero.
            return ReferenceEquals(kingdom.Leader?.Base, hero.Base);
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

                Character = PickFirstTroop(_faction, Mode);

                Fire(UIEvent.Faction);
            }
        }

        /* ━━━━━━━ Character ━━━━━━ */

        private WCharacter _character;

        public WCharacter Character
        {
            get => _character ??= PickFirstTroop(_faction, Mode);
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Refresh                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void ForceRefreshSelection()
        {
            try
            {
                if (_character == null)
                    return;

                var c = _character;
                var slot = _slot;

                // Break reference equality so setters run again.
                _character = null;
                _equipment = null;

                // Re-apply the same selection via setters (will repick equipment too).
                Character = c;

                // Restore slot and force Item refresh chain.
                Slot = slot;

                EventManager.FireBatch(() =>
                {
                    EventManager.Fire(UIEvent.Character);
                    EventManager.Fire(UIEvent.Equipment);
                    EventManager.Fire(UIEvent.Slot);
                });
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "State.ForceRefreshSelection failed.");
            }
        }
    }
}
