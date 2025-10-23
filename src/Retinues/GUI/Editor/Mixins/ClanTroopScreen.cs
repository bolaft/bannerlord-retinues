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

        /// <summary>
        /// Initialize the mixin, create editor VM and hook tab events.
        /// </summary>
        public ClanTroopScreen(ClanManagementVM vm)
            : base(vm)
        {
            try
            {
                Log.Info("Initializing ClanTroopScreen...");

                // Load sprite categories early
                try
                {
                    Log.Debug("SpriteLoader: loading all categories on initial screen set...");
                    SpriteLoader.LoadAllCategories();
                    Log.Debug("SpriteLoader: loaded all categories on initial screen set.");
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                // Reset state
                State.ResetAll();

                // Build editor VM
                _editor = new EditorVM();

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

        /// <summary>
        /// Finalize mixin and restore original hotkey behavior.
        /// </summary>
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

        /// <summary>
        /// Editor view-model instance exposed to the view.
        /// </summary>
        [DataSourceProperty]
        public EditorVM Editor => _editor;

        /// <summary>
        /// Label for the troops tab.
        /// </summary>
        [DataSourceProperty]
        public string TroopsTabText => L.S("troops_tab_text", "Troops");

        /// <summary>
        /// Whether the troop editor tab is currently selected.
        /// </summary>
        [DataSourceProperty]
        public bool IsTroopsSelected => Editor?.IsVisible == true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        [SafeMethod]
        /// <summary>
        /// Select and show the troop editor tab.
        /// </summary>
        public void ExecuteSelectTroops()
        {
            try
            {
                if (Editor?.IsVisible == true)
                    return;

                Log.Debug("Selecting Troops tab.");

                UnselectVanillaTabs();

                // Show editor
                Editor.Show();

                // Notify UI
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
        /// Handle vanilla tab selection changes and hide the troop editor when a vanilla tab is chosen.
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

                    // Hide editor
                    Editor.Hide();

                    // Notify UI
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
