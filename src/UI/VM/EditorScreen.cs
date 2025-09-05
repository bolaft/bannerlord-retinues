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

        private ITroopScreen _screen;

        private EditorMode _editorMode = EditorMode.Default;

        // =========================================================================
        // Constructor
        // =========================================================================

        public EditorScreenVM(WFaction faction, ITroopScreen screen)
        {
            try
            {
                Log.Debug("Initializing Troop Screen VM.");

                _screen = screen;

                if (faction.BasicTroops.IsEmpty() && faction.EliteTroops.IsEmpty())
                {
                    Log.Debug("No custom troops found, initializing default troops.");
                    Setup.Initialize(faction);
                }

                TroopEditor = new TroopEditorVM(this);
                TroopEditor.Refresh();

                TroopList = new TroopListVM(faction, this);
                TroopList.Refresh();

                EquipmentEditor = new EquipmentEditorVM(this);
                EquipmentEditor.Refresh();

                EquipmentList = new EquipmentListVM(faction, this);
                EquipmentList.Refresh();

                Refresh();
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
            Log.Info("Selecting Troops tab.");
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

        // =========================================================================
        // Public API
        // =========================================================================

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
        }
    }
}
