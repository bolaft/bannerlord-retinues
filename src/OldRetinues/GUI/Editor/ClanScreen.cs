using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.GUI.Editor.VM;
using Retinues.Mods;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor
{
    public enum EditorMode
    {
        Culture,
        Personal,
        Heroes,
    }

    [ViewModelMixin("RefreshValues", true)]
    public sealed class ClanScreen : BaseViewModelMixin<ClanManagementVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Editor Mode                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Current global editor mode
        public static EditorMode EditorMode { get; set; } = EditorMode.Personal;

        // Studio Mode means editing non-player troops
        public static bool IsStudioMode => EditorMode != EditorMode.Personal;

        /// <summary>
        /// Open the Clan screen with the editor in Studio Mode.
        /// </summary>
        public static void LaunchEditor(EditorMode mode = EditorMode.Culture)
        {
            try
            {
                EditorMode = mode;

                var gsm = TaleWorlds.Core.Game.Current?.GameStateManager;
                if (gsm == null)
                    return;

                var clanState = gsm.CreateState<ClanState>();
                gsm.PushState(clanState);

                if (mode == EditorMode.Personal)
                    Instance?.SelectEditorTab();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static ClanScreen Instance { get; private set; }

        public ClanScreen(ClanManagementVM vm)
            : base(vm)
        {
            try
            {
                Log.Info("Initializing ClanTroopScreen...");

                try
                {
                    SpriteLoader.LoadCategories(
                        "ui_charactercreation",
                        "ui_characterdeveloper",
                        "ui_inventory"
                    );
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
                if (IsStudioMode)
                    SelectEditorTab();

                Instance = this;

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
                // Leaving the clan screen in any way should exit Studio Mode.
                EditorMode = EditorMode.Personal;
                // Disable hotkey gate
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
        public bool IsTroopsSelected => Editor?.IsVisible == true;

        [DataSourceProperty]
        public bool IsTopPanelVisible => IsStudioMode == false;

        [DataSourceProperty]
        public bool IsFinancePanelVisible => !IsTroopsSelected && !IsStudioMode;

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
        public void ExecuteOpenPlayerMode() => OpenEditor(EditorMode.Personal);

        [DataSourceMethod]
        public void ExecuteOpenStudioMode() => OpenEditor(EditorMode.Culture);

        /* ━━━━━━━━ Helpers ━━━━━━━ */

        private void OpenEditor(EditorMode mode = EditorMode.Personal)
        {
            Log.Info($"Opening Editor in mode: {mode}");

            // flip global mode
            EditorMode = mode;

            // rebuild state & VM
            State.ResetAll();
            Editor = new EditorVM();
            OnPropertyChanged(nameof(Editor));

            // select our tab & refresh bindings
            SelectEditorTab();
        }

        private void SelectEditorTab()
        {
            // Give external mixins (like Banner Kings) a chance to reset
            // their custom tabs based on a vanilla selection.
            if (ModCompatibility.ForceClanTabsReset)
                ForceResetExternalTabs();

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
            OnPropertyChanged(nameof(IsFinancePanelVisible));
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
                    case "CourtSelected": // Bannerking
                    case "DemesneSelected": // Bannerking
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Mod Compatibility                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void ForceResetExternalTabs()
        {
            try
            {
                // Select a vanilla tab via the real API so other mixins bound to
                // SetSelectedCategory (like Banner Kings) can react and clear their flags.
                ViewModel.SetSelectedCategory(0); // 0 = Members
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
