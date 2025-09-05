using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using CustomClanTroops.UI.VM;
using CustomClanTroops.UI.VM.Equipment;
using CustomClanTroops.UI.VM.Troop;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI
{
    [ViewModelMixin("TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanManagementVM")]
    public sealed class ClanScreen : BaseViewModelMixin<ClanManagementVM>, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private bool _isTroopsSelected;

        private EditorMode _editorMode = EditorMode.Default;

        // =========================================================================
        // Constructor
        // =========================================================================

        public ClanScreen(ClanManagementVM vm) : base(vm)
        {
            try
            {
                Log.Debug("Initializing Clan Screen.");
    
                if (Player.Clan.BasicTroops.IsEmpty() && Player.Clan.EliteTroops.IsEmpty())
                {
                    Log.Debug("No custom troops found, initializing default troops.");
                    Setup.Initialize();
                }

                TroopEditor = new TroopEditorVM(this);
                TroopEditor.Refresh();

                TroopList = new TroopListVM(this);
                TroopList.Refresh();

                EquipmentEditor = new EquipmentEditorVM(this);
                EquipmentEditor.Refresh();

                EquipmentList = new EquipmentListVM(this);
                EquipmentList.Refresh();

                Refresh();

                ViewModel.PropertyChangedWithBoolValue += OnVanillaTabChanged;
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
        public TroopEditorVM TroopEditor { get; private set; }

        [DataSourceProperty]
        public TroopListVM TroopList { get; private set; }

        [DataSourceProperty]
        public EquipmentEditorVM EquipmentEditor { get; private set; }

        [DataSourceProperty]
        public EquipmentListVM EquipmentList { get; private set; }

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
        public void ExecuteSelectTroops()
        {
            if (IsTroopsSelected == true) return;

            ViewModel.IsMembersSelected = false;
            ViewModel.IsFiefsSelected = false;
            ViewModel.IsPartiesSelected = false;
            ViewModel.IsIncomeSelected = false;

            ViewModel.ClanMembers.IsSelected = false;
            ViewModel.ClanParties.IsSelected = false;
            ViewModel.ClanFiefs.IsSelected = false;
            ViewModel.ClanIncome.IsSelected = false;

            IsTroopsSelected = true;

            SwitchMode(EditorMode.Default);
        }

        [DataSourceMethod]
        public void ExecuteSwitchToDefault() => SwitchMode(EditorMode.Default);

        [DataSourceMethod]
        public void ExecuteSwitchToEquipment() => SwitchMode(EditorMode.Equipment);

        // =========================================================================
        // Public API
        // =========================================================================

        public WCharacter SelectedTroop => TroopList?.SelectedRow?.Troop;

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

        // =========================================================================
        // Internals
        // =========================================================================

        private void OnVanillaTabChanged(object sender, TaleWorlds.Library.PropertyChangedWithBoolValueEventArgs e)
        {
            if (!e.Value) return;

            if (e.PropertyName == "IsMembersSelected" ||
                e.PropertyName == "IsFiefsSelected" ||
                e.PropertyName == "IsPartiesSelected" ||
                e.PropertyName == "IsIncomeSelected")
            {
                IsTroopsSelected = false;
            }
        }

        private void SwitchMode(EditorMode mode)
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
    }
}
