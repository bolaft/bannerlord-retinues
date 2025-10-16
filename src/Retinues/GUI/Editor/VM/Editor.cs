using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Doctrines;
using Retinues.GUI.Editor.VM.Equipment;
using Retinues.GUI.Editor.VM.Troop;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM
{
    public enum Screen
    {
        Troop = 0,
        Equipment = 1,
        Doctrine = 2,
    }

    [SafeClass]
    public class EditorVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorVM()
        {
            Log.Info("Building EditorVM...");

            // Ensure retinue troops exist for player factions
            foreach (var f in new[] { Player.Clan, Player.Kingdom })
                if (f != null)
                    TroopBuilder.EnsureTroopsExist(f);

            // Components
            TroopScreen = new TroopScreenVM(this);
            EquipmentScreen = new EquipmentScreenVM(this);
            DoctrineScreen = new DoctrineScreenVM();
        }

        public void Initialize()
        {
            Log.Info("Initializing EditorVM...");

            // Initialize components
            TroopScreen.Initialize();
            EquipmentScreen.Initialize();

            // Subscribe to events
            EventManager.ScreenChange.RegisterProperties(
                this,
                nameof(InTroopScreen),
                nameof(InEquipmentScreen),
                nameof(InDoctrineScreen),
                nameof(EquipmentButtonText),
                nameof(DoctrinesButtonText),
                nameof(ShowFactionButton),
                nameof(ShowDoctrinesButton),
                nameof(ShowEquipmentButton)
            );
            EventManager.FactionChange.RegisterProperties(this, nameof(FactionButtonText));
            EventManager.TroopChange.RegisterProperties(this, nameof(Model));
            EventManager.GenderChange.RegisterProperties(this, nameof(Model));
            EventManager.EquipmentChange.RegisterProperties(this, nameof(Model));
            EventManager.EquipmentItemChange.RegisterProperties(this, nameof(Model));

            // Initial state
            SwitchScreen(Screen.Troop);
            SwitchFaction(Player.Clan);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Faction                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WFaction Faction { get; private set; }

        public void SwitchFaction(WFaction faction)
        {
            if (faction == null || faction == Faction)
                return;

            Log.Info($"Switching faction to {faction?.Name ?? "null"}.");

            Faction = faction;

            EventManager.FactionChange.Fire();
            EventManager.TroopListChange.Fire();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Editor Mode                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Screen Screen { get; private set; }

        public void SwitchScreen(Screen value)
        {
            if (Screen == value)
                return;

            Log.Info($"Switching screen from {Screen} to {value}");

            Screen = value;

            // Toggle screen visibility
            TroopScreen.IsVisible = Screen == Screen.Troop;
            EquipmentScreen.IsVisible = Screen == Screen.Equipment;
            DoctrineScreen.IsVisible = Screen == Screen.Doctrine;

            EventManager.ScreenChange.Fire(); // Notify other systems
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Components ━━━━━━ */

        [DataSourceProperty]
        public TroopScreenVM TroopScreen { get; private set; }

        [DataSourceProperty]
        public EquipmentScreenVM EquipmentScreen { get; private set; }

        [DataSourceProperty]
        public DoctrineScreenVM DoctrineScreen { get; private set; }

        /* ━━━━━━━ 3D Model ━━━━━━━ */

        private CharacterViewModel _model;
        private WCharacter _lastTroop;
        private int _lastIndex;

        [DataSourceProperty]
        public CharacterViewModel Model
        {
            get
            {
                Log.Info("Getting CharacterViewModel.");

                var troop = TroopScreen?.TroopList?.Selection?.Troop;
                if (troop == null)
                    return null;

                var index = EquipmentScreen?.Equipment?.Index ?? 0;

                if (_model == null || _lastTroop != troop || _lastIndex != index)
                {
                    _model = troop?.GetModel(index);
                    _lastTroop = troop;
                    _lastIndex = index;
                }

                return _model;
            }
        }

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string EquipmentButtonText =>
            Screen == Screen.Equipment
                ? L.S("close_equipment_button_text", "Back")
                : L.S("equipment_button_text", "Equipment");

        [DataSourceProperty]
        public string DoctrinesButtonText =>
            Screen == Screen.Doctrine
                ? L.S("close_doctrines_button_text", "Back")
                : L.S("doctrines_button_text", "Doctrines");

        [DataSourceProperty]
        public string FactionButtonText =>
            Faction == Player.Clan
                ? L.S("switch_to_kingdom_troops", "Switch to\nKingdom Troops")
                : L.S("switch_to_clan_troops", "Switch to\nClan Troops");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowFactionButton => Screen != Screen.Doctrine && Player.Kingdom != null;

        [DataSourceProperty]
        public bool ShowDoctrinesButton =>
            Screen != Screen.Equipment && (Config.EnableDoctrines ?? false);

        [DataSourceProperty]
        public bool ShowEquipmentButton => Screen != Screen.Doctrine;

        /* ━━━━━━━━ Brushes ━━━━━━━ */

        [DataSourceProperty]
        public string EquipmentButtonBrush =>
            Screen == Screen.Equipment ? "ButtonBrush3" : "ButtonBrush1";

        /* ━━━━━━━━ Screens ━━━━━━━ */

        [DataSourceProperty]
        public bool InTroopScreen => Screen == Screen.Troop;

        [DataSourceProperty]
        public bool InEquipmentScreen => Screen == Screen.Equipment;

        [DataSourceProperty]
        public bool InDoctrineScreen => Screen == Screen.Doctrine;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Mode Switch ━━━━━ */

        [DataSourceMethod]
        public void ExecuteToggleEquipment() =>
            SwitchScreen(Screen == Screen.Equipment ? Screen.Troop : Screen.Equipment);

        [DataSourceMethod]
        public void ExecuteToggleDoctrines() =>
            SwitchScreen(Screen == Screen.Doctrine ? Screen.Troop : Screen.Doctrine);

        /* ━━━━ Faction Switch ━━━━ */

        [DataSourceMethod]
        public void ExecuteSwitchFaction() =>
            SwitchFaction(Faction == Player.Clan ? Player.Kingdom : Player.Clan);
    }
}
