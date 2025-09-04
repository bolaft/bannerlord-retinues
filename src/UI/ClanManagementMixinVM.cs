using TaleWorlds.Core;
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
    public sealed class ClanManagementMixinVM : BaseViewModelMixin<ClanManagementVM>, IView
    {
        // =========================================================================
        // Constructor
        // =========================================================================

        public ClanManagementMixinVM(ClanManagementVM vm) : base(vm)
        {
            TroopEditor = new TroopEditorVM(this);
            EquipmentEditor = new EquipmentEditorVM(this);

            // Listen to vanilla tab changes to hide our panel
            ViewModel.PropertyChangedWithBoolValue += OnVanillaTabChanged;
        }

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public WCharacter SelectedTroop => TroopEditor.TroopList.SelectedRow?.Troop;

        // =========================================================================
        // Enums
        // =========================================================================

        public enum EditorMode
        {
            Default = 0,
            Equipment = 1
        }

        // =========================================================================
        // VMs
        // =========================================================================

        [DataSourceProperty] public TroopEditorVM TroopEditor { get; private set; }

        [DataSourceProperty] public EquipmentEditorVM EquipmentEditor { get; private set; }

        // =========================================================================
        // Tab Selection
        // =========================================================================

        private bool _isTroopsSelected;

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
            // Deselect vanilla tabs
            ViewModel.IsMembersSelected = false;
            ViewModel.IsFiefsSelected = false;
            ViewModel.IsPartiesSelected = false;
            ViewModel.IsIncomeSelected = false;

            // Deselect vanilla sub-tabs
            ViewModel.ClanMembers.IsSelected = false;
            ViewModel.ClanParties.IsSelected = false;
            ViewModel.ClanFiefs.IsSelected = false;
            ViewModel.ClanIncome.IsSelected = false;

            // Select troops tab
            IsTroopsSelected = true;

            // Check if a setup is needed
            if (Player.Clan.BasicTroops.IsEmpty() && Player.Clan.EliteTroops.IsEmpty())
                Log.Debug("No custom troops found, initializing default troops.");
                Setup.Initialize();
            
            Refresh();
        }

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            OnPropertyChanged(nameof(TroopEditor));
            OnPropertyChanged(nameof(EquipmentEditor));
            OnPropertyChanged(nameof(IsDefaultMode));
            OnPropertyChanged(nameof(IsEquipmentMode));

            TroopEditor.Refresh();
            EquipmentEditor.Refresh();
        }

        // =========================================================================
        // Tab Selection
        // =========================================================================

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

        [DataSourceMethod]
        public void ExecuteSwitchToDefault() => SwitchMode(EditorMode.Default);

        [DataSourceMethod]
        public void ExecuteSwitchToEquipment() => SwitchMode(EditorMode.Equipment);

        private void SwitchMode(EditorMode mode)
        {
            if (_editorMode == mode) return;

            _editorMode = mode;

            OnPropertyChanged(nameof(IsDefaultMode));
            OnPropertyChanged(nameof(IsEquipmentMode));
        }
    }
}