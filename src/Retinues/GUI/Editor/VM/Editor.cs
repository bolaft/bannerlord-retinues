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

        public static EditorVM Instance { get; private set; }

        public EditorVM()
        {
            // Singleton instance
            Instance = this;

            // Ensure retinue troops exist for player factions
            foreach (var f in new[] { Player.Clan, Player.Kingdom })
                if (f != null)
                    TroopBuilder.EnsureTroopsExist(f);

            // Subscribe to troop change events
            TroopScreen.TroopChanged += _ => OnTroopChanged(_);

            // Show troop screen
            TroopScreen.Show();
        }

        private void OnTroopChanged(WCharacter _)
        {
            TroopScreen.TroopPanel.RefreshOnTroopChange();
            OnPropertyChanged(nameof(Model));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Faction                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WFaction _faction = Player.Clan;
        public WFaction Faction
        {
            get => _faction;
            set
            {
                if (_faction == value || value == null)
                    return;
                _faction = value;

                // Build troop screen
                TroopScreen = new TroopScreenVM();
                OnPropertyChanged(nameof(TroopScreen));

                // Update faction button text
                OnPropertyChanged(nameof(FactionButtonText));

                // Update 3D model
                OnPropertyChanged(nameof(Model));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Editor Mode                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Screen _screen = Screen.Troop;
        public Screen Screen
        {
            get => _screen;
            set
            {
                if (_screen == value)
                    return;
                _screen = value;

                // Notify screen change
                OnPropertyChanged(nameof(InTroopScreen));
                OnPropertyChanged(nameof(InEquipmentScreen));
                OnPropertyChanged(nameof(InDoctrineScreen));

                // Update buttons
                OnPropertyChanged(nameof(EquipmentButtonText));
                OnPropertyChanged(nameof(DoctrinesButtonText));
                OnPropertyChanged(nameof(ShowFactionButton));
                OnPropertyChanged(nameof(ShowDoctrinesButton));
                OnPropertyChanged(nameof(ShowEquipmentButton));

                // Create/Show/Hide screens
                switch (value)
                {
                    case Screen.Troop:
                        EquipmentScreen.Hide();
                        DoctrineScreen.Hide();
                        TroopScreen ??= new TroopScreenVM();
                        TroopScreen.Show();
                        break;
                    case Screen.Equipment:
                        TroopScreen.Hide();
                        DoctrineScreen.Hide();
                        EquipmentScreen ??= new EquipmentScreenVM();
                        EquipmentScreen.Show();
                        break;
                    case Screen.Doctrine:
                        TroopScreen.Hide();
                        EquipmentScreen.Hide();
                        DoctrineScreen ??= new DoctrineScreenVM();
                        DoctrineScreen.Show();
                        break;
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private TroopScreenVM _troopScreen = new();

        [DataSourceProperty]
        public TroopScreenVM TroopScreen
        {
            get => _troopScreen;
            set
            {
                if (_troopScreen == value || value == null)
                    return;
                _troopScreen.TroopChanged -= OnTroopChanged; // unhook old event
                _troopScreen = value;
                _troopScreen.TroopChanged += OnTroopChanged; // hook new event
                OnPropertyChanged(nameof(TroopScreen));
            }
        }

        private EquipmentScreenVM _equipmentScreen = new();

        [DataSourceProperty]
        public EquipmentScreenVM EquipmentScreen
        {
            get => _equipmentScreen;
            set
            {
                if (_equipmentScreen == value || value == null)
                    return;
                _equipmentScreen = value;
                OnPropertyChanged(nameof(EquipmentScreen));
            }
        }

        private DoctrineScreenVM _doctrineScreen = new();

        [DataSourceProperty]
        public DoctrineScreenVM DoctrineScreen
        {
            get => _doctrineScreen;
            set
            {
                if (_doctrineScreen == value || value == null)
                    return;
                _doctrineScreen = value;
                OnPropertyChanged(nameof(DoctrineScreen));
            }
        }

        /* ━━━━━━━ 3D Model ━━━━━━━ */

        private CharacterViewModel _model;
        private WCharacter _lastTroop;
        private int _lastIndex;

        [DataSourceProperty]
        public CharacterViewModel Model
        {
            get
            {
                var index = EquipmentScreen?.Equipment?.Index ?? 0;

                if (_model == null || _lastTroop != SelectedTroop || _lastIndex != index)
                {
                    _model = SelectedTroop.GetModel(index);
                    _lastTroop = SelectedTroop;
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
        public bool ShowDoctrinesButton => Screen != Screen.Equipment && Config.EnableDoctrines;

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
            Screen = Screen == Screen.Equipment ? Screen.Troop : Screen.Equipment;

        [DataSourceMethod]
        public void ExecuteToggleDoctrines() =>
            Screen = Screen == Screen.Doctrine ? Screen.Troop : Screen.Doctrine;

        /* ━━━━ Faction Switch ━━━━ */

        [DataSourceMethod]
        public void ExecuteSwitchFaction()
        {
            if (Player.Kingdom == null)
                Faction = Player.Clan;
            else
                Faction = Faction == Player.Clan ? Player.Kingdom : Player.Clan;
        }
    }
}
