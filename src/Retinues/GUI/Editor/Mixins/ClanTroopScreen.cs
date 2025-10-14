using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.GUI.Editor.VM;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Mixins
{
    /// <summary>
    /// Mixin for ClanManagementVM to inject the custom troop editor screen and manage tab selection.
    /// </summary>
    [ViewModelMixin(
        "TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanManagementVM"
    )]
    public sealed class ClanTroopScreen : BaseViewModelMixin<ClanManagementVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ClanTroopScreen(ClanManagementVM vm)
            : base(vm)
        {
            try
            {
                // Initialize the editor screen ViewModel
                _editor = new EditorVM();

                ViewModel.PropertyChangedWithBoolValue += OnVanillaTabChanged;

                ClanHotkeyGate.Active = true;
                ClanHotkeyGate.RequireShift = false;
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public override void OnFinalize()
        {
            ClanHotkeyGate.Active = false; // restore default behavior
            base.OnFinalize();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly EditorVM _editor;

        [DataSourceProperty]
        public EditorVM Editor => _editor;

        [DataSourceProperty]
        public string TroopsTabText => L.S("troops_tab_text", "Troops");

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

                if (value == true)
                    Editor.Show();
                else
                    Editor.Hide();

                OnPropertyChanged(nameof(Editor));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        [SafeMethod]
        public void ExecuteSelectTroops()
        {
            Log.Debug("Selecting Troops tab.");

            if (IsTroopsSelected == true)
                return;

            UnselectVanillaTabs();

            IsTroopsSelected = true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unselects all vanilla clan management tabs when switching to troop editor.
        /// </summary>
        public void UnselectVanillaTabs()
        {
            ViewModel.IsMembersSelected = false;
            ViewModel.IsFiefsSelected = false;
            ViewModel.IsPartiesSelected = false;
            ViewModel.IsIncomeSelected = false;

            ViewModel.ClanMembers.IsSelected = false;
            ViewModel.ClanParties.IsSelected = false;
            ViewModel.ClanFiefs.IsSelected = false;
            ViewModel.ClanIncome.IsSelected = false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Handles vanilla tab selection changes to keep troop editor tab state in sync.
        /// </summary>
        private void OnVanillaTabChanged(object sender, PropertyChangedWithBoolValueEventArgs e)
        {
            if (!e.Value)
                return;

            if (
                e.PropertyName == "IsMembersSelected"
                || e.PropertyName == "IsFiefsSelected"
                || e.PropertyName == "IsPartiesSelected"
                || e.PropertyName == "IsIncomeSelected"
            )
            {
                if (Editor != null)
                    IsTroopsSelected = false;
            }
        }
    }
}
