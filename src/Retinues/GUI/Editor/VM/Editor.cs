using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment;
using Retinues.GUI.Editor.VM.Troop;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM
{
    /// <summary>
    /// ViewModel for the troop editor screen. Handles mode switching, faction switching, selection, and refresh logic.
    /// </summary>
    [SafeClass]
    public class EditorVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Singleton instance for easy access
        public static EditorVM Instance { get; private set; }

        // Constructor (called once for singleton pattern)
        public EditorVM(WFaction faction)
        {
            Instance = this;

            // Will update everything else
            Faction = faction;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
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

                // Always recreate to reset state
                TroopPanel = new TroopPanelVM(value);

                // Ensure default mode
                Mode = EditorMode.Default;

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

                Troop = _faction.RetinueElite;

                // Update faction button text
                OnPropertyChanged(nameof(FactionButtonText));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Editor Mode                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public enum EditorMode
        {
            Default = 0,
            Equipment = 1,
            Doctrines = 2,
        }

        private EditorMode _mode = EditorMode.Default;
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

                if (value == EditorMode.Equipment)
                {
                    // If troop since last time in equipment mode, recreate
                    if (EquipmentPanel == null || EquipmentPanel.Troop != Troop)
                    {
                        EquipmentPanel = new EquipmentPanelVM(Troop);
                        OnPropertyChanged(nameof(EquipmentPanel));
                    }
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
                if (_troopPanel == value) return;
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
                if (_troopList == value) return;
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
                if (_equipmentPanel == value) return;
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
                if (_equipmentList == value) return;
                _equipmentList = value;
                OnPropertyChanged(nameof(EquipmentList));
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
                var category = EquipmentPanel.LoadoutCategory;
                var index = EquipmentPanel.LoadoutIndex;
                if (_model == null || _lastTroop != Troop || _lastCategory != (int)category || _lastIndex != index)
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
            Mode == EditorMode.Doctrines
                ? L.S("close_doctrines_button_text", "Back")
                : L.S("doctrines_button_text", "Doctrines");

        [DataSourceProperty]
        public string FactionButtonText =>
            Faction == Player.Clan
                ? L.S("switch_to_kingdom_troops", "Switch to\nKingdom Troops")
                : L.S("switch_to_clan_troops", "Switch to\nClan Troops");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowFactionButton => Mode != EditorMode.Doctrines && Player.Kingdom != null;

        [DataSourceProperty]
        public bool ShowDoctrinesButton => Config.GetOption<bool>("EnableDoctrines");

        /* ━━━━━━━━━ Modes ━━━━━━━━ */

        [DataSourceProperty]
        public bool DefaultMode => Mode == EditorMode.Default;

        [DataSourceProperty]
        public bool EquipmentMode => Mode == EditorMode.Equipment;

        [DataSourceProperty]
        public bool DoctrinesMode => Mode == EditorMode.Doctrines;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Mode Switch ━━━━━ */

        [DataSourceMethod]
        public void ExecuteSwitchToDefault() => Mode = EditorMode.Default;

        [DataSourceMethod]
        public void ExecuteSwitchToEquipment() => Mode = EditorMode.Equipment;

        [DataSourceMethod]
        public void ExecuteSwitchToDoctrines() => Mode = EditorMode.Doctrines;

        /* ━━━━ Faction Switch ━━━━ */

        [DataSourceMethod]
        public void ExecuteSwitchFaction() =>
            Faction = Faction == Player.Clan ? Player.Kingdom : Player.Clan;
    }
}
