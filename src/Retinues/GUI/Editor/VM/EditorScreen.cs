using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.Mixins;
using Retinues.GUI.Editor.VM.Doctrines;
using Retinues.GUI.Editor.VM.Equipment;
using Retinues.GUI.Editor.VM.Troop;
using Retinues.Troops;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM
{
    /// <summary>
    /// ViewModel for the troop editor screen. Handles mode switching, faction switching, selection, and refresh logic.
    /// </summary>
    [SafeClass]
    public class EditorScreenVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ClanTroopScreen _screen;

        private EditorMode _editorMode = EditorMode.Default;

        private WFaction _faction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorScreenVM(WFaction faction, ClanTroopScreen screen)
        {
            try
            {
                Log.Debug("Initializing Troop Screen VM.");

                _screen = screen;

                SwitchFaction(faction);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Enums                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public enum EditorMode
        {
            Default = 0,
            Equipment = 1,
            Doctrines = 2,
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━━ VMs ━━━━━━━━━ */

        [DataSourceProperty]
        public TroopEditorVM TroopEditor { get; private set; }

        [DataSourceProperty]
        public TroopListVM TroopList { get; private set; }

        [DataSourceProperty]
        public EquipmentEditorVM EquipmentEditor { get; private set; }

        [DataSourceProperty]
        public EquipmentListVM EquipmentList { get; private set; }

        [DataSourceProperty]
        public CharacterViewModel Model =>
            EquipmentEditor is null
                ? null
                : SelectedTroop?.GetModel(
                    EquipmentEditor.LoadoutCategory,
                    EquipmentEditor.LoadoutIndex
                );

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string TroopsTabText => L.S("troops_tab_text", "Troops");

        [DataSourceProperty]
        public string EquipmentButtonText => L.S("equipment_button_text", "Equipment");

        [DataSourceProperty]
        public string CloseEquipmentButtonText => L.S("close_equipment_button_text", "Back");

        [DataSourceProperty]
        public string DoctrinesButtonText => L.S("doctrines_button_text", "Doctrines");

        [DataSourceProperty]
        public string CloseDoctrinesButtonText => L.S("close_doctrines_button_text", "Back");

        [DataSourceProperty]
        public string FactionSwitchText =>
            Faction == Player.Clan
                ? L.S("switch_to_kingdom_troops", "Switch to\nKingdom Troops")
                : L.S("switch_to_clan_troops", "Switch to\nClan Troops");

        /* ━━━━━━━━━ Mode ━━━━━━━━━ */

        [DataSourceProperty]
        public bool IsDefaultMode
        {
            get => _editorMode == EditorMode.Default;
            private set
            {
                if (value && _editorMode != EditorMode.Default)
                    SwitchMode(EditorMode.Default);
                OnPropertyChanged(nameof(IsDefaultMode));
                OnPropertyChanged(nameof(IsEquipmentMode));
                OnPropertyChanged(nameof(IsDoctrinesMode));
            }
        }

        [DataSourceProperty]
        public bool IsNotDefaultMode => !IsDefaultMode;

        [DataSourceProperty]
        public bool IsEquipmentMode
        {
            get => _editorMode == EditorMode.Equipment;
            private set
            {
                if (value && _editorMode != EditorMode.Equipment)
                    SwitchMode(EditorMode.Equipment);
                OnPropertyChanged(nameof(IsEquipmentMode));
                OnPropertyChanged(nameof(IsDefaultMode));
                OnPropertyChanged(nameof(IsDoctrinesMode));
            }
        }

        [DataSourceProperty]
        public bool IsNotEquipmentMode => !IsEquipmentMode;

        [DataSourceProperty]
        public bool IsDoctrinesMode
        {
            get => _editorMode == EditorMode.Doctrines;
            private set
            {
                if (value && _editorMode != EditorMode.Doctrines)
                    SwitchMode(EditorMode.Doctrines);
                OnPropertyChanged(nameof(IsEquipmentMode));
                OnPropertyChanged(nameof(IsDefaultMode));
                OnPropertyChanged(nameof(IsDoctrinesMode));
                OnPropertyChanged(nameof(IsNotDoctrinesMode));
            }
        }

        [DataSourceProperty]
        public bool IsNotDoctrinesMode => !IsDoctrinesMode;

        [DataSourceProperty]
        public bool CanSwitchToDoctrines =>
            !IsDoctrinesMode && Config.GetOption<bool>("EnableDoctrines");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        private bool _isTroopsSelected;

        [DataSourceProperty]
        public bool IsTroopsSelected
        {
            get => _isTroopsSelected;
            set
            {
                if (value == _isTroopsSelected)
                    return;
                _isTroopsSelected = value;
                OnPropertyChanged(nameof(IsTroopsSelected));
            }
        }

        [DataSourceProperty]
        public bool CanSwitchFaction => Player.Kingdom != null && !IsDoctrinesMode;

        [DataSourceProperty]
        public bool ShowRemoveButton =>
            IsDefaultMode && SelectedTroop?.IsRetinue == false && SelectedTroop?.IsMilitia == false;

        /* ━━━━━━━ Doctrines ━━━━━━ */

        private MBBindingList<DoctrineColumnVM> _doctrineColumns;

        [DataSourceProperty]
        public MBBindingList<DoctrineColumnVM> DoctrineColumns
        {
            get
            {
                _doctrineColumns ??= DoctrineColumnVM.CreateColumns();
                return _doctrineColumns;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Mode Switch ━━━━━ */

        [DataSourceMethod]
        public void ExecuteSwitchToDefault() => SwitchMode(EditorMode.Default);

        [DataSourceMethod]
        public void ExecuteSwitchToEquipment() => SwitchMode(EditorMode.Equipment);

        [DataSourceMethod]
        public void ExecuteSwitchToDoctrines() => SwitchMode(EditorMode.Doctrines);

        /* ━━━━ Faction Switch ━━━━ */

        [DataSourceMethod]
        public void ExecuteSwitchFaction()
        {
            if (Faction == Player.Clan)
                SwitchFaction(Player.Kingdom);
            else
                SwitchFaction(Player.Clan);
        }

        /* ━━━━━━━ Selection ━━━━━━ */

        [DataSourceMethod]
        [SafeMethod]
        public void ExecuteSelectTroops()
        {
            Log.Debug("Selecting Troops tab.");

            if (IsTroopsSelected == true)
                return;

            _screen.UnselectVanillaTabs();

            IsTroopsSelected = true;

            SwitchMode(EditorMode.Default);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WFaction Faction => _faction;

        public bool EditingIsAllowed
        {
            get
            {
                if (Config.GetOption<bool>("RestrictEditingToFiefs") == false)
                    return true;

                return TroopRules.IsAllowedInContextWithPopup(
                    SelectedTroop,
                    Faction,
                    L.S("action_modify", "modify")
                );
            }
        }
        public bool ConversionIsAllowed
        {
            get
            {
                var restrict = Config.GetOption<bool>("RestrictConversionToFiefs");
                Log.Debug($"ConversionIsAllowed: RestrictConversionToFiefs={restrict}");
                return restrict == false
                    || TroopRules.IsAllowedInContextWithPopup(
                        SelectedTroop,
                        Faction,
                        L.S("action_convert", "convert")
                    );
            }
        }

        public WCharacter SelectedTroop => TroopList?.SelectedRow?.Troop;

        /// <summary>
        /// Switches the editor mode (Default, Equipment, Doctrines) and refreshes relevant VMs.
        /// </summary>
        public void SwitchMode(EditorMode mode)
        {
            if (_editorMode == mode)
                return;

            if (mode == EditorMode.Doctrines && !Config.GetOption<bool>("EnableDoctrines"))
                return;

            _editorMode = mode;

            if (mode == EditorMode.Equipment)
            {
                EquipmentEditor.Refresh();
                EquipmentList.Refresh();
            }
            if (mode == EditorMode.Default)
            {
                TroopEditor.Refresh();
            }

            TroopEditor.OnPropertyChanged(nameof(TroopEditor.CanRemove));
            EquipmentEditor.OnPropertyChanged(nameof(EquipmentEditor.CanUnequip));

            OnPropertyChanged(nameof(IsDefaultMode));
            OnPropertyChanged(nameof(IsEquipmentMode));
            OnPropertyChanged(nameof(IsDoctrinesMode));

            OnPropertyChanged(nameof(IsNotDefaultMode));
            OnPropertyChanged(nameof(IsNotEquipmentMode));
            OnPropertyChanged(nameof(IsNotDoctrinesMode));

            OnPropertyChanged(nameof(CanSwitchToDoctrines));
            OnPropertyChanged(nameof(CanSwitchFaction));
        }

        /// <summary>
        /// Switches the editor to a new faction and rebuilds all editor VMs.
        /// </summary>
        [SafeMethod]
        public void SwitchFaction(WFaction faction)
        {
            _faction = faction;

            // Build troops if missing
            TroopBuilder.EnsureTroopsExist(faction);

            TroopEditor = new TroopEditorVM(this);
            TroopEditor.Refresh();

            TroopList = new TroopListVM(this);
            TroopList.Refresh();

            EquipmentEditor = new EquipmentEditorVM(this);
            EquipmentEditor.Refresh();

            EquipmentList = new EquipmentListVM(this);
            EquipmentList.Refresh();

            Refresh();
        }

        /// <summary>
        /// Refreshes all bindings and VMs for the editor screen.
        /// </summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(DoctrineColumns));

            OnPropertyChanged(nameof(TroopEditor));
            OnPropertyChanged(nameof(TroopList));
            OnPropertyChanged(nameof(EquipmentEditor));
            OnPropertyChanged(nameof(EquipmentList));

            OnPropertyChanged(nameof(ShowRemoveButton));

            OnPropertyChanged(nameof(IsDefaultMode));
            OnPropertyChanged(nameof(IsEquipmentMode));
            OnPropertyChanged(nameof(IsDoctrinesMode));
            OnPropertyChanged(nameof(IsNotDoctrinesMode));

            OnPropertyChanged(nameof(Model));

            OnPropertyChanged(nameof(Faction));
            OnPropertyChanged(nameof(FactionSwitchText));
            OnPropertyChanged(nameof(CanSwitchFaction));
        }
    }
}
