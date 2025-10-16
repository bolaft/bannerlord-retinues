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
                Log.Info("Initializing ClanTroopScreen...");

                // Build editor VM with empty components
                _editor = new EditorVM();
                // Fill components
                _editor.Initialize();

                // Listen to vanilla tab changes to toggle editor visibility
                ViewModel.PropertyChangedWithBoolValue += OnVanillaTabChanged;

                // Block tab hotkeys when the editor is open
                ClanHotkeyGate.Active = true;
                ClanHotkeyGate.RequireShift = false;

                Log.Info("ClanTroopScreen initialized.");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public override void OnFinalize()
        {
            try
            {
                ClanHotkeyGate.Active = false; // restore default behavior
                base.OnFinalize();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly EditorVM _editor;

        [DataSourceProperty]
        public EditorVM Editor => _editor;

        [DataSourceProperty]
        public string TroopsTabText => L.S("troops_tab_text", "Troops");

        [DataSourceProperty]
        public bool IsTroopsSelected => Editor?.IsVisible == true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        [SafeMethod]
        public void ExecuteSelectTroops()
        {
            try
            {
                if (Editor?.IsVisible == true)
                    return;

                Log.Debug("Selecting Troops tab.");

                UnselectVanillaTabs();

                Editor.IsVisible = true;
                OnPropertyChanged(nameof(IsTroopsSelected));
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unselects all vanilla clan management tabs when switching to troop editor.
        /// </summary>
        public void UnselectVanillaTabs()
        {
            try
            {
                Log.Debug("Unselecting vanilla clan management tabs.");

                ViewModel.IsMembersSelected = false;
                ViewModel.IsFiefsSelected = false;
                ViewModel.IsPartiesSelected = false;
                ViewModel.IsIncomeSelected = false;

                ViewModel.ClanMembers.IsSelected = false;
                ViewModel.ClanParties.IsSelected = false;
                ViewModel.ClanFiefs.IsSelected = false;
                ViewModel.ClanIncome.IsSelected = false;
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Handles vanilla tab selection changes to keep troop editor tab state in sync.
        /// </summary>
        private void OnVanillaTabChanged(object sender, PropertyChangedWithBoolValueEventArgs e)
        {
            try
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
                    // A vanilla tab was selected, hide the troop editor
                    Log.Debug($"Vanilla tab selected ({e.PropertyName}), hiding troop editor.");

                    Editor.IsVisible = false;
                    OnPropertyChanged(nameof(IsTroopsSelected));
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
