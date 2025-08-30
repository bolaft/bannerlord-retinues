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

namespace CustomClanTroops.UI
{
    [ViewModelMixin("TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanManagementVM")]
    public sealed class ClanManagementMixinVM : BaseViewModelMixin<ClanManagementVM>
    {
        private bool _isTroopsSelected;

        private TroopRowVM _selected;

        private readonly MBBindingList<TroopRowVM> _basic = new();

        private readonly MBBindingList<TroopRowVM> _elite = new();

        [DataSourceProperty] public bool EditorDefaultMode => TroopEditor?.EditorDefaultMode ?? false;

        [DataSourceProperty] public bool EditorEquipmentMode => TroopEditor?.EditorEquipmentMode ?? false;

        [DataSourceProperty] public MBBindingList<TroopRowVM> CustomBasic => _basic;

        [DataSourceProperty] public MBBindingList<TroopRowVM> CustomElite => _elite;

        [DataSourceProperty] public TroopRowVM TroopRow
        {
            get => _selected;
            private set
            {
                if (_selected == value) return;
                _selected = value;
                // Only instantiate TroopEditor when a troop is selected
                TroopEditor = _selected != null ? new TroopEditorVM(this) : null;

                OnPropertyChanged(nameof(TroopRow));
                OnPropertyChanged(nameof(TroopEditor));
                OnPropertyChanged(nameof(IsAnyTroopSelected));
                OnPropertyChanged(nameof(EditorDefaultMode));
                OnPropertyChanged(nameof(EditorEquipmentMode));
            }
        }

        [DataSourceProperty] public TroopEditorVM TroopEditor { get; private set; }

        [DataSourceProperty] public bool IsAnyTroopSelected => _selected != null;

        [DataSourceProperty] public bool IsTroopsSelected
        {
            get => _isTroopsSelected;
            set
            {
                if (value == _isTroopsSelected) return;
                _isTroopsSelected = value;
                OnPropertyChanged(nameof(IsTroopsSelected));
                if (_isTroopsSelected) EnsureTroopsReady();
            }
        }

        [DataSourceMethod] public void ExecuteSelectTroops()
        {
            Log.Debug("ClanManagementMixinVM: Troops tab clicked");
            EnsureTroopsReady();

            IsTroopsSelected = true;

            // Deselect vanilla tabs + their panels
            ViewModel.IsMembersSelected = false;
            ViewModel.IsFiefsSelected = false;
            ViewModel.IsPartiesSelected = false;
            ViewModel.IsIncomeSelected = false;

            ViewModel.ClanMembers.IsSelected = false;
            ViewModel.ClanParties.IsSelected = false;
            ViewModel.ClanFiefs.IsSelected = false;
            ViewModel.ClanIncome.IsSelected = false;
        }

        public ClanManagementMixinVM(ClanManagementVM vm) : base(vm)
        {
            Log.Debug("ClanManagementMixinVM: created");
            ViewModel.PropertyChangedWithBoolValue += OnVanillaTabChanged;

            OnPropertyChanged(nameof(TroopEditor));
        }

        public override void OnFinalize()
        {
            ViewModel.PropertyChangedWithBoolValue -= OnVanillaTabChanged;
            Log.Debug("ClanManagementMixinVM: finalized");
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

        private void EnsureTroopsReady()
        {
            if (!TroopManager.CustomTroopsExist())
            {
                // Create them by cloning the player culture's troops
                TroopSetup.ClonePlayerCultureTroops();
            }

            try
            {
                UpdateLists();
                SelectFirstTroop();
            }
            catch (Exception ex)
            {
                Log.Error($"ClanManagementMixinVM.EnsureTroopsReady: {ex}");
            }
        }

        private void OnRowSelected(TroopRowVM vm) => SelectExclusive(vm);

        private void SelectExclusive(TroopRowVM vm)
        {
            if (vm == null) return;

            foreach (var r in _basic) if (!ReferenceEquals(r, vm) && r.IsSelected) r.IsSelected = false;
            foreach (var e in _elite) if (!ReferenceEquals(e, vm) && e.IsSelected) e.IsSelected = false;

            vm.IsSelected = true;
            TroopRow = vm;
            TroopEditor.Refresh();

            Log.Debug($"ClanManagementMixinVM: selected '{vm.Name}'");
        }

        public void SelectFirstTroop()
        {
            if (_basic.Count > 0)
            {
                SelectExclusive(_basic[0]);
                Log.Debug($"ClanManagementMixinVM: auto-selected '{_basic[0].Name}'");
            }
        }

        public void UpdateTroops()
        {
            TroopRow?.Refresh();
            TroopEditor?.Refresh();
            OnPropertyChanged(nameof(CustomBasic));
            OnPropertyChanged(nameof(CustomElite));
            OnPropertyChanged(nameof(TroopRow));
            OnPropertyChanged(nameof(TroopEditor));
            OnPropertyChanged(nameof(TroopRow));
            OnPropertyChanged(nameof(TroopEditor));
            OnPropertyChanged(nameof(EditorDefaultMode));
            OnPropertyChanged(nameof(EditorEquipmentMode));
        }

        public void UpdateLists()
        {
            var prevSelectedId = TroopRow?.Troop.StringId;

            _basic.Clear();
            foreach (var troop in TroopManager.BasicCustomTroops)
            {
                _basic.Add(new TroopRowVM(troop, OnRowSelected));
            }

            _elite.Clear();
            foreach (var troop in TroopManager.EliteCustomTroops)
            {
                _elite.Add(new TroopRowVM(troop, OnRowSelected));
            }

            Log.Debug($"ClanManagementMixinVM: lists built → Basic={_basic.Count}, Elite={_elite.Count}");
            OnPropertyChanged(nameof(CustomBasic));
            OnPropertyChanged(nameof(CustomElite));

            // Restore selection by StringId
            if (!string.IsNullOrEmpty(prevSelectedId))
            {
                var newSelected = _basic.Concat(_elite).FirstOrDefault(r => r.Troop.StringId == prevSelectedId);
                if (newSelected != null)
                    SelectExclusive(newSelected);
            }
            else
            {
                SelectFirstTroop();
            }
        }
    }
}
