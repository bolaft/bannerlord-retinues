using System.Linq;
using Retinues.Behaviors.Doctrines;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Factions;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Library.Services;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor
{
    /// <summary>
    /// Editor modes.
    /// </summary>
    public enum EditorMode
    {
        Universal = 0,
        Player = 1,
    }

    /// <summary>
    /// Editor pages.
    /// </summary>
    public enum EditorPage
    {
        Character = 0,
        Equipment = 1,
        Doctrines = 2,
        Library = 3,
    }

    /// <summary>
    /// Holds the state of the editor.
    /// </summary>
    [SafeClass]
    public partial class EditorState
    {
        /// <summary>
        /// The current editor mode.
        /// </summary>
        public EditorMode Mode { get; private set; } = EditorMode.Universal;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Instance                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The singleton instance of the editor state.
        /// </summary>
        private static EditorState _instance;
        public static EditorState Instance => _instance ??= new EditorState();

        [StaticClearAction]
        public static void ClearInstance() => _instance = null;

        /// <summary>
        /// Resets the singleton instance with optional launch arguments.
        /// </summary>
        public static void Reset(EditorLaunchArgs args = null) => _instance = new EditorState(args);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Initialization                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly bool _isInitializing;

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
            Log.Debug("Initializing new editor state");

            // Set the singleton instance.
            _instance = this;

            // Apply launch arguments.
            _isInitializing = true;
            ApplyLaunchArgs(args);
            _isInitializing = false;

            // Page defaults to character editing.
            SetPage(EditorPage.Character);

            // Notify listeners.
            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.Faction);
                EventManager.Fire(UIEvent.Character);
                EventManager.Fire(UIEvent.Equipment);
                EventManager.Fire(UIEvent.Slot);
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Fires a UI event if not initializing.
        /// </summary>
        private void Fire(UIEvent e)
        {
            if (_isInitializing)
                return;

            EventManager.Fire(e);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Page                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorPage Page { get; private set; } = EditorPage.Character;

        EditorPage _lastEditorSubPage = EditorPage.Character;

        /// <summary>
        /// Sets the current editor page to the last editor sub-page.
        /// </summary>
        public void SetPage() => SetPage(_lastEditorSubPage);

        /// <summary>
        /// Sets the current editor page.
        /// </summary>
        public void SetPage(EditorPage page)
        {
            if (Page == page)
                return;

            Page = page;

            // Keep "Editor" tab sticky to the last real editor sub-page.
            if (page == EditorPage.Character || page == EditorPage.Equipment)
                _lastEditorSubPage = page;

            // Notify any listeners that page changed (columns, buttons, etc.).
            EventManager.Fire(UIEvent.Page);
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
                Fire(UIEvent.Faction);
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
                Fire(UIEvent.Faction);
            }
        }

        /* ━━━━━━━━ Faction ━━━━━━━ */

        private IBaseFaction _faction;

        /// <summary>
        /// The currently selected faction.
        /// </summary>
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

        /// <summary>
        /// The currently selected character.
        /// </summary>
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

        /// <summary>
        /// The currently selected equipment.
        /// </summary>
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

        /// <summary>
        /// The currently selected equipment slot.
        /// </summary>
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

        private ExportLibrary.Entry _libraryItem;

        /// <summary>
        /// The currently selected library item.
        /// </summary>
        public ExportLibrary.Entry LibraryItem
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

        /* ━━━━━━━ Doctrine ━━━━━━━ */

        private Doctrine _doctrine = DoctrinesRegistry.GetDoctrines().FirstOrDefault();

        /// <summary>
        /// The currently selected doctrine.
        /// </summary>
        public Doctrine Doctrine
        {
            get => _doctrine;
            set
            {
                if (value == _doctrine)
                    return;

                _doctrine = value;
                Fire(UIEvent.Doctrine);
            }
        }

        /* ━━━━━━━━ Crafted ━━━━━━━ */

        private bool _showCrafted;

        /// <summary>
        /// Whether to show crafted items in the equipment list.
        /// </summary>
        public bool ShowCrafted
        {
            get => _showCrafted;
            set
            {
                if (value == _showCrafted)
                    return;

                _showCrafted = value;
                Fire(UIEvent.Crafted);
            }
        }
    }
}
