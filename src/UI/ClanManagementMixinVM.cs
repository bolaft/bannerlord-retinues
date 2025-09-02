using System;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;
using CustomClanTroops.UI.VM;
using CustomClanTroops.UI.Enums;

namespace CustomClanTroops.UI
{
    [ViewModelMixin("TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanManagementVM")]
    public sealed class ClanManagementMixinVM : BaseViewModelMixin<ClanManagementVM>
    {
        // ---- Constructor ----

        public ClanManagementMixinVM(ClanManagementVM vm) : base(vm)
        {
            Log.Debug("ClanManagementMixinVM: created (refactored)");

            // Child regions
            TroopEditor = new TroopEditorVM(this);
            TroopList = new TroopListVM(this);
            EquipmentEditor = new EquipmentEditorVM(this);
            EquipmentList = new EquipmentListVM(this);

            // Listen to vanilla tab changes to hide our panel
            ViewModel.PropertyChangedWithBoolValue += OnVanillaTabChanged;

            // Make sure initial state is consistent
            OnPropertyChanged(nameof(TroopEditor));
            OnPropertyChanged(nameof(TroopList));
            OnPropertyChanged(nameof(EquipmentEditor));
            OnPropertyChanged(nameof(EquipmentList));
            OnPropertyChanged(nameof(IsDefaultMode));
            OnPropertyChanged(nameof(IsEquipmentMode));
        }

        // ---- VMs ----
    
        [DataSourceProperty] public TroopEditorVM TroopEditor { get; private set; }

        [DataSourceProperty] public TroopListVM TroopList { get; private set; }

        [DataSourceProperty] public EquipmentEditorVM EquipmentEditor { get; private set; }

        [DataSourceProperty] public EquipmentListVM EquipmentList { get; private set; }

        // ---- Selection (row) ----

        [DataSourceProperty] public TroopRowVM SelectedRow { get; private set; }

        [DataSourceProperty] public CharacterViewModel TroopViewModel => SelectedRow?.Troop.ViewModel;

        // ---- Tab Initialization ----

        private bool _isTroopsSelected;

        public CharacterWrapper SelectedTroop => SelectedRow?.Troop;

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

        private void OnVanillaTabChanged(object sender, PropertyChangedWithBoolValueEventArgs e)
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

        [DataSourceMethod]
        public void ExecuteSelectTroops()
        {
            Log.Debug($"{nameof(ExecuteSelectTroops)} called");

            // deselect vanilla tabs
            ViewModel.IsMembersSelected = false;
            ViewModel.IsFiefsSelected = false;
            ViewModel.IsPartiesSelected = false;
            ViewModel.IsIncomeSelected = false;

            // Deselect vanilla sub-tabs
            ViewModel.ClanMembers.IsSelected = false;
            ViewModel.ClanParties.IsSelected = false;
            ViewModel.ClanFiefs.IsSelected = false;
            ViewModel.ClanIncome.IsSelected = false;

            // select ours
            IsTroopsSelected = true;

            // refresh troop lists if needed
            EnsureTroopsReady();
        }

        private void EnsureTroopsReady()
        {
            try
            {
                // If manager says nothing exists, seed from player culture.
                if (!TroopManager.CustomTroopsExist())
                {
                    Log.Debug("EnsureTroopsReady: no custom troops -> seeding from player culture.");
                    SetupManager.Setup();
                }

                // Update left pane
                TroopList.Refresh();
                OnPropertyChanged(nameof(TroopList));
            }
            catch (Exception ex)
            {
                Log.Error($"EnsureTroopsReady failed: {ex}");
            }
        }

        // ---- Mode switching ----

        private EditorMode _editorMode = EditorMode.Default;

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

        [DataSourceMethod] public void ExecuteSwitchToDefault() => SwitchMode(EditorMode.Default);

        [DataSourceMethod] public void ExecuteSwitchToEquipment()
        {
            // Refresh equipment list because skill changes may affect available items
            EquipmentList.Refresh();

            // Go to equipment mode
            SwitchMode(EditorMode.Equipment);
        }

        private void SwitchMode(EditorMode mode)
        {
            if (_editorMode == mode) return;
            _editorMode = mode;

            OnPropertyChanged(nameof(IsDefaultMode));
            OnPropertyChanged(nameof(IsEquipmentMode));
        }

        // ---- Lists & selection ----

        public void SelectFirstTroop()
        {
            var first = TroopList.EliteTroops.FirstOrDefault() ?? TroopList.BasicTroops.FirstOrDefault();
            if (first != null) HandleRowSelected(first);
        }

        public void SelectById(string stringId)
        {
            var row = TroopList.BasicTroops.Concat(TroopList.EliteTroops).FirstOrDefault(r => r.Troop.StringId == stringId);
            if (row != null) HandleRowSelected(row);
        }

        internal void HandleRowSelected(TroopRowVM row)
        {
            if (row == null) return;

            foreach (var r in TroopList.BasicTroops) r.IsSelected = ReferenceEquals(r, row);
            foreach (var r in TroopList.EliteTroops) r.IsSelected = ReferenceEquals(r, row);

            SelectedRow = row;
            OnPropertyChanged(nameof(SelectedRow));
            OnPropertyChanged(nameof(TroopViewModel));
            OnPropertyChanged(nameof(CanRemoveTroop));

            TroopEditor.Refresh();
            EquipmentEditor.Refresh();

            SwitchMode(EditorMode.Default);
        }

        // ---- Troop actions ----

        [DataSourceProperty]
        public bool CanRemoveTroop
        {
            get
            {
                if (SelectedTroop == null) return false;
                return SelectedTroop.Parent != null && SelectedTroop.UpgradeTargets.Count() == 0;
            }
        }

        [DataSourceMethod]
        public void ExecuteRemoveTroop()
        {
            Log.Debug($"{nameof(ExecuteRemoveTroop)} called.");

            if (SelectedTroop == null) return;

            ShowRemoveTroopConfirmation(() =>
            {
                TroopManager.RemoveTroop(SelectedTroop);
                TroopList.Refresh();
                OnPropertyChanged(nameof(TroopList));
                SelectFirstTroop();
            });
        }

        private void ShowRemoveTroopConfirmation(Action onConfirm)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Remove Troop",
                "Are you sure you want to permanently remove this troop?",
                true, true,
                "Yes", "No",
                () => onConfirm?.Invoke(),
                null
            ));
        }

        // ---- Refresh ----

        public void RefreshTroopViewModel()
        {
            OnPropertyChanged(nameof(TroopViewModel));
        }
    }
}
