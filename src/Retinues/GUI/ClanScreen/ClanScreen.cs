using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.Editor.VM;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.ClanScreen
{
    [ViewModelMixin("RefreshValues", true)]
    public sealed class ClanScreen : BaseViewModelMixin<ClanManagementVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Constants                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // List of sprite sheets required by the troop editor
        static readonly string[] SpriteSheetsToLoad =
        [
            "ui_charactercreation",
            "ui_characterdeveloper",
            "ui_inventory",
        ];

        // Vanilla tab property names
        static readonly string[] VanillaTabs =
        [
            "IsMembersSelected",
            "IsFiefsSelected",
            "IsPartiesSelected",
            "IsIncomeSelected",
        ];

        // Third-party tab property names
        static readonly string[] ModdedTabs =
        [
            "CourtSelected", // Banner Kings
            "DemesneSelected", // Banner Kings
        ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Launcher                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Open the Clan screen editor.
        /// </summary>
        public static void Launch()
        {
            // Get the game state manager
            var gsm = Game.Current?.GameStateManager;
            if (gsm == null)
                return;

            // Push the clan screen state
            var state = gsm.CreateState<ClanState>();
            gsm.PushState(state);

            // Select the editor tab
            Instance?.OpenEditor();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static ClanScreen Instance { get; private set; }

        public ClanScreen(ClanManagementVM vm)
            : base(vm)
        {
            // Load sprites used by the editor
            SpriteManager.Load(SpriteSheetsToLoad);

            // Listen once for vanilla tab changes
            ViewModel.PropertyChangedWithBoolValue += OnTabChanged;

            // Singleton instance
            Instance = this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void OnFinalize()
        {
            base.OnFinalize();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Editor ━━━━━━━━ */

        [DataSourceProperty]
        public EditorVM Editor { get; set; }

        /* ━━━━━━━━ Strings ━━━━━━━ */

        [DataSourceProperty]
        public string EditorTabLabel => L.S("editor_tab_label", "Troops");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowEditorPanel => Editor != null;

        [DataSourceProperty]
        public bool ShowTopPanel => true;

        [DataSourceProperty]
        public bool ShowFinancePanel => ShowEditorPanel == false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteSelectEditorTab()
        {
            OpenEditor();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Open / Close                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OpenEditor()
        {
            // Give third-party mixins a chance to reset their custom tabs (e.g. Banner Kings).
            ViewModel.SetSelectedCategory(0); // 0 = Members

            // Unselect vanilla tabs
            UnselectVanillaTabs();

            // Create editor VM if needed
            Editor ??= new EditorVM();

            // Toggle panel visibility
            UpdateVisibility();
        }

        private void CloseEditor()
        {
            // Clear editor VM
            Editor = null;

            // Toggle panel visibility
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            OnPropertyChanged(nameof(ShowEditorPanel));
            OnPropertyChanged(nameof(ShowTopPanel));
            OnPropertyChanged(nameof(ShowFinancePanel));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Tab Management                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        private void OnTabChanged(object sender, PropertyChangedWithBoolValueEventArgs e)
        {
            if (!e.Value)
                return;

            if (Array.Exists(VanillaTabs, tab => tab == e.PropertyName))
            {
                // A vanilla tab was selected, hide the editor
                CloseEditor();
                return;
            }

            if (Array.Exists(ModdedTabs, tab => tab == e.PropertyName))
            {
                // A modded tab was selected, hide the editor
                CloseEditor();
                return;
            }
        }
    }
}
