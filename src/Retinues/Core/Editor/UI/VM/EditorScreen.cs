using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Editor.UI.Mixins;
using Retinues.Core.Editor.UI.VM.Doctrines;
using Retinues.Core.Editor.UI.VM.Equipment;
using Retinues.Core.Editor.UI.VM.Troop;
using Retinues.Core.Features;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM
{
    public class EditorScreenVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ITroopScreen _screen;

        private EditorMode _editorMode = EditorMode.Default;

        private WFaction _faction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorScreenVM(WFaction faction, ITroopScreen screen)
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
        public CharacterViewModel Model => SelectedTroop?.Model;

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
        public bool ShowRemoveButton => IsDefaultMode && SelectedTroop?.IsRetinue == false;

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
        public void ExecuteSelectTroops()
        {
            Log.Debug("Selecting Troops tab.");

            try
            {
                if (IsTroopsSelected == true)
                    return;

                _screen.UnselectVanillaTabs();

                IsTroopsSelected = true;

                SwitchMode(EditorMode.Default);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WFaction Faction => _faction;

        public WCharacter SelectedTroop => TroopList?.SelectedRow?.Troop;

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

        public void SwitchFaction(WFaction faction)
        {
            Log.Debug($"Switching to faction: {faction?.Name ?? "null"}");

            if (!faction.RetinueElite.IsActive || !faction.RetinueBasic.IsActive)
            {
                Log.Info("No retinue troops found, initializing default retinue troops.");
                Setup.SetupFactionRetinue(faction);
            }

            if (faction.BasicTroops.IsEmpty() && faction.EliteTroops.IsEmpty())
            {
                Log.Debug("No custom troops found for faction.");

                // Always have clan troops if clan has fiefs, if player leads a kingdom or if can recruit anywhere is enabled
                if (
                    faction.HasFiefs
                    || Player.Kingdom != null
                    || Config.GetOption<bool>("RecruitAnywhere")
                )
                {
                    Log.Info("Initializing default troops.");

                    Setup.SetupFactionTroops(faction);
                }
            }

            _faction = faction;

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
