using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.GUI.Editor.VM;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Mixins
{
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

                try
                {
                    SpriteLoader.LoadCategories("ui_charactercreation", "ui_characterdeveloper");
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                // Build editor VM for current launch mode
                State.ResetAll();
                Editor = new EditorVM();

                // Listen once for vanilla tab changes
                ViewModel.PropertyChangedWithBoolValue += OnVanillaTabChanged;

                // Gate hotkeys while the editor is around
                ClanHotkeyGate.Active = true;
                ClanHotkeyGate.RequireShift = false;

                // Auto-select our editor tab if we launched in Studio Mode
                if (State.IsStudioMode)
                    SelectEditorTab();

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
                ClanHotkeyGate.Active = false;
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

        [DataSourceProperty]
        public EditorVM Editor { get; private set; }

        [DataSourceProperty]
        public string TroopsTabText => L.S("troops_tab_text", "Troops");

        [DataSourceProperty]
        public string ClanTroopsButtonText => L.S("clan_troops_button_text", "Clan Troops");

        [DataSourceProperty]
        public BasicTooltipViewModel ClanTroopsHint =>
            Tooltip.MakeTooltip(
                null,
                L.S("switch_player_mode_hint", "Switch to clan troops editor.")
            );

        [DataSourceProperty]
        public bool IsTroopsSelected => Editor?.IsVisible == true;

        [DataSourceProperty]
        public bool IsTopPanelVisible => State.IsStudioMode == false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        [SafeMethod]
        public void ExecuteSelectTroops()
        {
            try
            {
                if (Editor?.IsVisible != true)
                    SelectEditorTab();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        [DataSourceMethod]
        public void ExecuteOpenPlayerMode()
        {
            Log.Info("Switching to player troop editor mode...");

            // flip global mode
            State.IsStudioMode = false;

            // rebuild state & VM
            State.ResetAll();
            Editor = new EditorVM();
            OnPropertyChanged(nameof(Editor));

            // select our tab & refresh bindings
            SelectEditorTab();

            Log.Info("Player troop editor mode ready.");
        }

        // ── Helpers ───────────────────────────────────────────

        private void SelectEditorTab()
        {
            UnselectVanillaTabs();
            Editor.Show();
            UpdateVisibilityFlags();
        }

        private void HideEditor()
        {
            Editor.Hide();
            UpdateVisibilityFlags();
        }

        public void UnselectVanillaTabs()
        {
            try
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
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private void UpdateVisibilityFlags()
        {
            OnPropertyChanged(nameof(IsTroopsSelected));
            OnPropertyChanged(nameof(IsTopPanelVisible));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnVanillaTabChanged(object sender, PropertyChangedWithBoolValueEventArgs e)
        {
            try
            {
                if (!e.Value)
                    return;

                switch (e.PropertyName)
                {
                    case "IsMembersSelected":
                    case "IsFiefsSelected":
                    case "IsPartiesSelected":
                    case "IsIncomeSelected":
                        Log.Debug($"Vanilla tab selected ({e.PropertyName}), hiding troop editor.");
                        HideEditor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
