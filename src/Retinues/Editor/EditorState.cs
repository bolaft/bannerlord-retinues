using System;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Framework.Model.Exports;
using Retinues.Framework.Runtime;
using Retinues.Game;
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
    /// </summary>
    public sealed class EditorLaunchArgs
    {
        public EditorMode Mode { get; } = EditorMode.Universal;

        public IBaseFaction Faction { get; }
        public WCharacter Character { get; }
        public WHero Hero { get; }

        private EditorLaunchArgs(
            EditorMode mode,
            IBaseFaction faction = null,
            WCharacter character = null,
            WHero hero = null
        )
        {
            Mode = mode;
            Faction = faction;
            Character = character;
            Hero = hero;
        }

        public static EditorLaunchArgs ForMode(EditorMode mode) => new(mode);

        public static EditorLaunchArgs Universal(
            IBaseFaction faction = null,
            WCharacter character = null,
            WHero hero = null
        ) => new(EditorMode.Universal, faction, character, hero);

        public static EditorLaunchArgs Player(
            IBaseFaction faction = null,
            WCharacter character = null,
            WHero hero = null
        ) => new(EditorMode.Player, faction, character, hero);

        public bool IsEmpty => Faction == null && Character == null && Hero == null;
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

        private void ApplyLaunchArgs(EditorLaunchArgs args)
        {
            Mode = args?.Mode ?? EditorMode.Universal;
            Log.Info($"Launch Mode: {Mode}.");

            var resolved = ResolveLaunch(args);

            LeftBannerFaction = resolved.LeftBanner;
            RightBannerFaction = resolved.RightBanner;

            // Setting Faction will also pick the first troop automatically.
            Faction = resolved.Faction;

            // If a specific character was requested, apply it after the faction selection.
            if (resolved.Character != null)
                Character = resolved.Character;

            Equipment = PickFirstEquipment(Character);
            Slot = EquipmentIndex.Weapon0;
        }

        private sealed class ResolvedLaunch
        {
            public IBaseFaction LeftBanner { get; set; }
            public IBaseFaction RightBanner { get; set; }
            public IBaseFaction Faction { get; set; }
            public WCharacter Character { get; set; }
        }

        private ResolvedLaunch ResolveLaunch(EditorLaunchArgs args)
        {
            if (args == null || args.IsEmpty)
                return ResolveDefault();

            // Prefer explicit faction when provided.
            var faction = args.Faction;

            // Prefer explicit character; if hero is provided, focus the hero's CharacterObject.
            var character = args.Character ?? args.Hero?.Character;

            if (faction == null && character != null)
            {
                if (Mode == EditorMode.Player && character.InCustomTree)
                    faction = character.AssignedMapFaction;

                if (Mode == EditorMode.Universal)
                    faction = character.Culture;
            }

            if (faction == null && args.Hero != null)
            {
                faction =
                    Mode == EditorMode.Universal ? (IBaseFaction)args.Hero.Clan : args.Hero.Clan;
            }

            if (faction == null)
                return ResolveDefault(character);

            return Mode == EditorMode.Player
                ? ResolvePlayer(faction, character)
                : ResolveUniversal(faction, character);
        }

        private ResolvedLaunch ResolveDefault(WCharacter focus = null)
        {
            if (Mode == EditorMode.Player)
            {
                // Same default as before: start on clan.
                return new ResolvedLaunch
                {
                    LeftBanner = Player.Clan,
                    RightBanner = Player.IsRuler ? Player.Kingdom : null,
                    Faction = Player.Clan,
                    Character = focus,
                };
            }

            var culture = Player.Culture;

            return new ResolvedLaunch
            {
                LeftBanner = culture,
                RightBanner = null,
                Faction = culture,
                Character = focus,
            };
        }

        private ResolvedLaunch ResolveUniversal(IBaseFaction faction, WCharacter focus)
        {
            // Universal UI is: Left = Culture, Right = Clan (optional), Selected = faction (culture or clan).
            if (faction is WClan clan)
            {
                return new ResolvedLaunch
                {
                    LeftBanner = clan.Culture,
                    RightBanner = clan,
                    Faction = clan,
                    Character = focus,
                };
            }

            if (faction is WCulture culture)
            {
                return new ResolvedLaunch
                {
                    LeftBanner = culture,
                    RightBanner = null,
                    Faction = culture,
                    Character = focus,
                };
            }

            // Fallback: treat unknown as culture-based.
            return ResolveDefault(focus);
        }

        private ResolvedLaunch ResolvePlayer(IBaseFaction faction, WCharacter focus)
        {
            // Player UI is: Left = Clan, Right = Kingdom (only visible when ruler), Selected can be Clan or Kingdom.

            var right =
                faction is WKingdom k ? k
                : Player.IsRuler ? Player.Kingdom
                : null;

            var left = faction is WClan c ? c : Player.Clan;

            return new ResolvedLaunch
            {
                LeftBanner = left,
                RightBanner = right,
                Faction = faction,
                Character = focus,
            };
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
