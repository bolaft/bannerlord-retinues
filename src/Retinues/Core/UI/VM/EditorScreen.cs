using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.UI.VM;
using CustomClanTroops.UI.VM.Equipment;
using CustomClanTroops.UI.VM.Troop;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;
using CustomClanTroops.Wrappers.Campaign;

namespace CustomClanTroops.UI
{
    public class EditorScreenVM : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private bool _isTroopsSelected;

        private readonly ITroopScreen _screen;

        private EditorMode _editorMode = EditorMode.Default;

        private WFaction _faction;

        // =========================================================================
        // Constructor
        // =========================================================================

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

        // =========================================================================
        // Enums
        // =========================================================================

        public enum EditorMode
        {
            Default = 0,
            Equipment = 1
        }

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public bool IsTroopsSelected
        {
            get => _isTroopsSelected;
            set
            {
                if (value == _isTroopsSelected) return;
                _isTroopsSelected = value;
                OnPropertyChanged(nameof(IsTroopsSelected));
            }
        }

        [DataSourceProperty]
        public bool CanSwitchFaction => Player.Kingdom != null;

        [DataSourceProperty]
        public string FactionSwitchText => Faction == Player.Clan ? "Switch to\nKingdom Troops" : "Switch to\nClan Troops";

        [DataSourceProperty]
        public TroopEditorVM TroopEditor { get; private set; }

        [DataSourceProperty]
        public TroopListVM TroopList { get; private set; }

        [DataSourceProperty]
        public EquipmentEditorVM EquipmentEditor { get; private set; }

        [DataSourceProperty]
        public EquipmentListVM EquipmentList { get; private set; }

        [DataSourceProperty]
        public bool IsDefaultMode
        {
            get => _editorMode == EditorMode.Default;
            private set
            {
                if (value && _editorMode != EditorMode.Default) SwitchMode(EditorMode.Default);
                OnPropertyChanged(nameof(IsDefaultMode));
                OnPropertyChanged(nameof(IsEquipmentMode));
            }
        }

        [DataSourceProperty]
        public bool IsEquipmentMode
        {
            get => _editorMode == EditorMode.Equipment;
            private set
            {
                if (value && _editorMode != EditorMode.Equipment) SwitchMode(EditorMode.Equipment);
                OnPropertyChanged(nameof(IsEquipmentMode));
                OnPropertyChanged(nameof(IsDefaultMode));
            }
        }

        [DataSourceProperty]
        public CharacterViewModel Model => SelectedTroop?.Model;

        // =========================================================================
        // Action Bindings
        // =========================================================================

        [DataSourceMethod]
        public void ExecuteSwitchToDefault() => SwitchMode(EditorMode.Default);

        [DataSourceMethod]
        public void ExecuteSwitchToEquipment() => SwitchMode(EditorMode.Equipment);

        [DataSourceMethod]
        public void ExecuteSelectTroops()
        {
            Log.Debug("Selecting Troops tab.");

            try
            {
                if (IsTroopsSelected == true) return;

                _screen.UnselectVanillaTabs();

                IsTroopsSelected = true;

                SwitchMode(EditorMode.Default);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        [DataSourceMethod]
        public void ExecuteSwitchFaction()
        {
            Log.Debug("Switching faction.");

            if (Faction == Player.Clan)
                SwitchFaction(Player.Kingdom);
            else
                SwitchFaction(Player.Clan);
        }

        // =========================================================================
        // Public API
        // =========================================================================

        public WFaction Faction => _faction;

        public WCharacter SelectedTroop => TroopList?.SelectedRow?.Troop;

        public void SwitchMode(EditorMode mode)
        {
            if (_editorMode == mode) return;

            _editorMode = mode;

            // Refresh only when switching to Equipment mode
            if (mode == EditorMode.Equipment)
            {
                EquipmentEditor.Refresh();
                EquipmentList.Refresh();
            }

            // Button updates
            TroopEditor.OnPropertyChanged(nameof(TroopEditor.CanRemove));
            EquipmentEditor.OnPropertyChanged(nameof(EquipmentEditor.CanUnequip));

            // UI updates
            OnPropertyChanged(nameof(IsDefaultMode));
            OnPropertyChanged(nameof(IsEquipmentMode));
        }

        public void SwitchFaction(WFaction faction)
        {
            if (faction.BasicTroops.IsEmpty() && faction.EliteTroops.IsEmpty())
            {
                Log.Debug("No custom troops found, initializing default troops.");
                Setup.Initialize(faction);
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
            Log.Debug("Refreshing.");

            OnPropertyChanged(nameof(TroopEditor));
            OnPropertyChanged(nameof(TroopList));
            OnPropertyChanged(nameof(EquipmentEditor));
            OnPropertyChanged(nameof(EquipmentList));

            OnPropertyChanged(nameof(IsDefaultMode));
            OnPropertyChanged(nameof(IsEquipmentMode));

            OnPropertyChanged(nameof(Model));

            OnPropertyChanged(nameof(Faction));
            OnPropertyChanged(nameof(FactionSwitchText));
            OnPropertyChanged(nameof(CanSwitchFaction));
        }
    }
}
