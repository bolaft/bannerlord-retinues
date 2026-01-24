using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Settings;
using TaleWorlds.Core;

namespace Retinues.Editor
{
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

        /// <summary>
        /// Create launch args for the given mode.
        /// </summary>
        public static EditorLaunchArgs ForMode(EditorMode mode) => new(mode);

        /// <summary>
        /// Create universal-mode launch args.
        /// </summary>
        public static EditorLaunchArgs Universal(
            IBaseFaction faction = null,
            WCharacter character = null,
            WHero hero = null
        ) => new(EditorMode.Universal, faction, character, hero);

        /// <summary>
        /// Create player-mode launch args.
        /// </summary>
        public static EditorLaunchArgs Player(
            IBaseFaction faction = null,
            WCharacter character = null,
            WHero hero = null
        ) => new(EditorMode.Player, faction, character, hero);

        public bool IsEmpty => Faction == null && Character == null && Hero == null;
    }

    /// <summary>
    /// Partial class for editor state launch handling.
    /// </summary>
    public partial class EditorState
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Launch                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Apply launch arguments to the editor state.
        /// </summary>
        private void ApplyLaunchArgs(EditorLaunchArgs args)
        {
            Mode = args?.Mode ?? EditorMode.Universal;

            if (!Configuration.EnableUniversalEditor && Mode == EditorMode.Universal)
                Mode = EditorMode.Player;

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

            // Fire mode change event on launch.
            EventManager.Fire(UIEvent.Mode);
        }

        /// <summary>
        /// Resolved launch parameters.
        /// </summary>
        private sealed class ResolvedLaunch
        {
            public IBaseFaction LeftBanner { get; set; }
            public IBaseFaction RightBanner { get; set; }
            public IBaseFaction Faction { get; set; }
            public WCharacter Character { get; set; }
        }

        /// <summary>
        /// Resolve launch parameters into concrete factions and character.
        /// </summary>
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
                if (Mode == EditorMode.Player && character.IsFactionTroop)
                    faction = character.AssignedMapFaction;

                if (Mode == EditorMode.Universal)
                    faction = character.Culture;
            }

            if (faction == null && args.Hero != null)
            {
                faction = Mode == EditorMode.Universal ? args.Hero.Clan : args.Hero.Clan;
            }

            if (faction == null)
                return ResolveDefault(character);

            return Mode == EditorMode.Player
                ? ResolvePlayer(faction, character)
                : ResolveUniversal(faction, character);
        }

        /// <summary>
        /// Default launch resolution.
        /// </summary>
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

        /// <summary>
        /// Universal mode launch resolution.
        /// </summary>
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

        /// <summary>
        /// Player mode launch resolution.
        /// </summary>
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

        /// <summary>
        /// Pick the first suitable troop for the given faction.
        /// </summary>
        private static WCharacter PickFirstTroop(IBaseFaction faction, EditorMode mode)
        {
            if (faction?.Troops == null)
                return null;

            bool isClan = faction is WClan;

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

                    if (!troop.IsFactionTroop)
                        continue;
                }
                else
                {
                    // Universal:
                    // - Clan selection: allow heroes (they are map-faction troops).
                    // - Otherwise (culture): exclude map-faction troops.
                    if (isClan)
                    {
                        if (!troop.IsHero)
                            continue;
                    }
                    else
                    {
                        if (troop.IsFactionTroop)
                            continue;
                    }
                }

                return troop;
            }

            return null;
        }

        /// <summary>
        /// Pick the first suitable equipment for the given character.
        /// </summary>
        private static MEquipment PickFirstEquipment(WCharacter character)
        {
            var equipments = character?.Equipments;

            if (equipments == null || equipments.Count == 0)
                return null;

            foreach (var equipment in equipments)
            {
                if (equipment != null && equipment.IsCivilian == character.IsCivilian)
                    return equipment;
            }

            return null;
        }
    }
}
