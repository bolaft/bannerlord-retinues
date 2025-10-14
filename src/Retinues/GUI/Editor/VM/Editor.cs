using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Doctrines;
using Retinues.GUI.Editor.VM.Equipment;
using Retinues.GUI.Editor.VM.Troop;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM
{
    public enum EditorMode
    {
        Troop = 0,
        Equipment = 1,
        Doctrine = 2,
    }

    /// <summary>
    /// ViewModel for the troop editor screen. Handles mode switching, faction switching, selection, and refresh logic.
    /// </summary>
    [SafeClass]
    public class EditorVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorVM Instance { get; private set; }

        public EditorVM(WFaction faction)
        {
            Instance = this;

            // Will update everything else
            Faction = faction;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Troop                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WCharacter _troop;
        public WCharacter Troop
        {
            get => _troop;
            set
            {
                if (_troop == value || value == null)
                    return;
                _troop = value;

                // New troop panel
                TroopPanel = new TroopPanelVM(value, Faction);
                OnPropertyChanged(nameof(TroopPanel));

                // Ensure model refresh
                OnPropertyChanged(nameof(Model));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Faction                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WFaction _faction;
        public WFaction Faction
        {
            get => _faction;
            set
            {
                if (_faction == value || value == null)
                    return;
                _faction = value;

                // Build troop list, default selection will trigger panel build
                TroopList = new TroopListVM(_faction);
                OnPropertyChanged(nameof(TroopList));

                // Update faction button text
                OnPropertyChanged(nameof(FactionButtonText));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Editor Mode                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private EditorMode _mode;
        public EditorMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value)
                    return;
                _mode = value;

                OnPropertyChanged(nameof(DefaultMode));
                OnPropertyChanged(nameof(EquipmentMode));
                OnPropertyChanged(nameof(DoctrinesMode));

                // Hide all panels/lists not in this mode
                if (value != EditorMode.Troop)
                {
                    TroopList?.Hide();
                    TroopPanel?.Hide();
                }
                if (value != EditorMode.Equipment)
                {
                    EquipmentList?.Hide();
                    EquipmentPanel?.Hide();
                }
                if (value != EditorMode.Doctrine)
                {
                    DoctrineScreen?.Hide();
                }

                // Create / Show relevant panels/lists
                switch (value)
                {
                    case EditorMode.Troop:
                        TroopList.Show();
                        TroopPanel.Show();
                        break;
                    case EditorMode.Equipment:
                        if (EquipmentPanel == null || Troop != EquipmentPanel.Troop)
                            EquipmentPanel = new EquipmentPanelVM(Troop);

                        EquipmentPanel.Show();
                        EquipmentList.Show();
                        break;
                    case EditorMode.Doctrine:
                        DoctrineScreen ??= new DoctrineScreenVM();
                        DoctrineScreen.Show();
                        break;
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━━ VMs ━━━━━━━━━ */

        private TroopPanelVM _troopPanel;

        [DataSourceProperty]
        public TroopPanelVM TroopPanel
        {
            get => _troopPanel;
            set
            {
                if (_troopPanel == value)
                    return;
                _troopPanel = value;
                OnPropertyChanged(nameof(TroopPanel));
            }
        }

        private TroopListVM _troopList;

        [DataSourceProperty]
        public TroopListVM TroopList
        {
            get => _troopList;
            set
            {
                if (_troopList == value)
                    return;
                _troopList = value;
                OnPropertyChanged(nameof(TroopList));
            }
        }

        private EquipmentPanelVM _equipmentPanel;

        [DataSourceProperty]
        public EquipmentPanelVM EquipmentPanel
        {
            get => _equipmentPanel;
            set
            {
                if (_equipmentPanel == value)
                    return;
                _equipmentPanel = value;
                OnPropertyChanged(nameof(EquipmentPanel));
            }
        }

        private EquipmentListVM _equipmentList;

        [DataSourceProperty]
        public EquipmentListVM EquipmentList
        {
            get => _equipmentList;
            set
            {
                if (_equipmentList == value)
                    return;
                _equipmentList = value;
                OnPropertyChanged(nameof(EquipmentList));
            }
        }

        private DoctrineScreenVM _doctrineScreen;

        [DataSourceProperty]
        public DoctrineScreenVM DoctrineScreen
        {
            get => _doctrineScreen;
            set
            {
                if (_doctrineScreen == value)
                    return;
                _doctrineScreen = value;
                OnPropertyChanged(nameof(DoctrineScreen));
            }
        }

        /* ━━━━━━━ 3D Model ━━━━━━━ */

        private CharacterViewModel _model;
        private WCharacter _lastTroop;
        private int _lastCategory;
        private int _lastIndex;

        [DataSourceProperty]
        public CharacterViewModel Model
        {
            get
            {
                var category = EquipmentPanel.EquipmentCategory;
                var index = EquipmentPanel.Index;
                if (
                    _model == null
                    || _lastTroop != Troop
                    || _lastCategory != (int)category
                    || _lastIndex != index
                )
                {
                    _model = Troop?.GetModel(category, index);
                    _lastTroop = Troop;
                    _lastCategory = (int)category;
                    _lastIndex = index;
                }
                return _model;
            }
        }

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string EquipmentButtonText =>
            Mode == EditorMode.Equipment
                ? L.S("close_equipment_button_text", "Back")
                : L.S("equipment_button_text", "Equipment");

        [DataSourceProperty]
        public string DoctrinesButtonText =>
            Mode == EditorMode.Doctrine
                ? L.S("close_doctrines_button_text", "Back")
                : L.S("doctrines_button_text", "Doctrines");

        [DataSourceProperty]
        public string FactionButtonText =>
            Faction == Player.Clan
                ? L.S("switch_to_kingdom_troops", "Switch to\nKingdom Troops")
                : L.S("switch_to_clan_troops", "Switch to\nClan Troops");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowFactionButton => Mode != EditorMode.Doctrine && Player.Kingdom != null;

        [DataSourceProperty]
        public bool ShowDoctrinesButton => Config.EnableDoctrines;

        /* ━━━━━━━━━ Modes ━━━━━━━━ */

        [DataSourceProperty]
        public bool DefaultMode => Mode == EditorMode.Troop;

        [DataSourceProperty]
        public bool EquipmentMode => Mode == EditorMode.Equipment;

        [DataSourceProperty]
        public bool DoctrinesMode => Mode == EditorMode.Doctrine;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Mode Switch ━━━━━ */

        [DataSourceMethod]
        public void ExecuteSwitchToDefault() => Mode = EditorMode.Troop;

        [DataSourceMethod]
        public void ExecuteSwitchToEquipment() => Mode = EditorMode.Equipment;

        [DataSourceMethod]
        public void ExecuteSwitchToDoctrines() => Mode = EditorMode.Doctrine;

        /* ━━━━ Faction Switch ━━━━ */

        [DataSourceMethod]
        public void ExecuteSwitchFaction() =>
            Faction = Faction == Player.Clan ? Player.Kingdom : Player.Clan;
    }
}
