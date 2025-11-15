using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Doctrines;
using Retinues.GUI.Editor.VM.Equipment;
using Retinues.GUI.Editor.VM.Troop;
using Retinues.GUI.Helpers;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
# if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.GUI.Editor.VM
{
    /// <summary>
    /// Available editor screens.
    /// </summary>
    public enum Screen
    {
        Troop = 0,
        Equipment = 1,
        Doctrine = 2,
    }

    /// <summary>
    /// Root view-model coordinating editor screens.
    /// </summary>
    [SafeClass]
    public class EditorVM : BaseVM
    {
        /// <summary>
        /// Initialize the editor and its child screens.
        /// </summary>
        public EditorVM()
        {
            TroopScreen = new TroopScreenVM();
            EquipmentScreen = new EquipmentScreenVM();
            DoctrineScreen = new DoctrineScreenVM();

            if (ClanScreen.IsGlobalEditorMode)
                RefreshCultureBanner();

            SwitchScreen(Screen.Troop);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Screen                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Screen Screen { get; set; }

        /// <summary>
        /// Switch the active editor screen and update visibility.
        /// </summary>
        public void SwitchScreen(Screen value)
        {
            if (Screen == value && IsVisible)
                return;

            if (ClanScreen.IsGlobalEditorMode && value == Screen.Doctrine)
                return; // Doctrines not available in Studio Mode

            Log.Info($"Switching screen from {Screen} to {value}");

            Screen = value;

            // Toggle screen visibility via Show/Hide helpers
            if (Screen == Screen.Troop)
                TroopScreen.Show();
            else
                TroopScreen.Hide();
            if (Screen == Screen.Equipment)
                EquipmentScreen.Show();
            else
                EquipmentScreen.Hide();
            if (Screen == Screen.Doctrine)
                DoctrineScreen.Show();
            else
                DoctrineScreen.Hide();

            // Notify UI
            OnPropertyChanged(nameof(InTroopScreen));
            OnPropertyChanged(nameof(InEquipmentScreen));
            OnPropertyChanged(nameof(InDoctrineScreen));
            OnPropertyChanged(nameof(ShowFactionButton));
            OnPropertyChanged(nameof(ShowDoctrinesButton));
            OnPropertyChanged(nameof(ShowEquipmentButton));
            OnPropertyChanged(nameof(ShowGlobalEditorLink));
            OnPropertyChanged(nameof(EquipmentButtonText));
            OnPropertyChanged(nameof(DoctrinesButtonText));
            OnPropertyChanged(nameof(FactionButtonText));
            OnPropertyChanged(nameof(EquipmentButtonBrush));
            OnPropertyChanged(nameof(DoctrinesButtonBrush));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Faction] = [nameof(FactionButtonText), nameof(ShowFactionButton)],
                [UIEvent.Appearance] = [nameof(Model)],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public TroopScreenVM TroopScreen { get; set; }

        [DataSourceProperty]
        public EquipmentScreenVM EquipmentScreen { get; set; }

        [DataSourceProperty]
        public DoctrineScreenVM DoctrineScreen { get; set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Studio Mode ━━━━━ */

        [DataSourceProperty]
        public string TroopStudioTitle => L.S("troop_studio_title", "Troop Editor");

# if BL13
        private BannerImageIdentifierVM _cultureBanner;

        [DataSourceProperty]
        public BannerImageIdentifierVM CultureBanner
# else
        private ImageIdentifierVM _cultureBanner;

        [DataSourceProperty]
        public ImageIdentifierVM CultureBanner
# endif
        {
            get => _cultureBanner;
            private set
            {
                if (_cultureBanner == value)
                    return;
                _cultureBanner = value;
                OnPropertyChanged(nameof(CultureBanner));
            }
        }

        [DataSourceProperty]
        public string CultureName =>
            ClanScreen.IsGlobalEditorMode ? State.Faction?.Culture?.Name : string.Empty;

        [DataSourceProperty]
        public BasicTooltipViewModel CultureBannerHint =>
            Tooltip.MakeTooltip(null, L.S("select_culture_hint", "Select a culture."));

        /* ━━━━━━━ 3D Model ━━━━━━━ */

        [DataSourceProperty]
        public CharacterViewModel Model => State.Troop?.GetModel(State.Equipment.Index);

        /* ━━━━━━━ Gauntlet ━━━━━━━ */

        [DataSourceProperty]
        public int TableauMarginLeft => ClanScreen.IsGlobalEditorMode ? 60 : 0;

        [DataSourceProperty]
        public int PanelMarginLeft => ClanScreen.IsGlobalEditorMode ? 440 : 340;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string EquipmentButtonText =>
            Screen == Screen.Equipment
                ? L.S("close_equipment_button_text", "Back")
                : L.S("equipment_button_text", "Equipment");

        [DataSourceProperty]
        public string DoctrinesButtonText =>
            Screen == Screen.Doctrine
                ? L.S("close_doctrines_button_text", "Back")
                : L.S("doctrines_button_text", "Doctrines");

        [DataSourceProperty]
        public string FactionButtonText =>
            (StringIdentifier)State.Faction == Player.Clan
                ? L.S("switch_to_kingdom_troops", "Kingdom Troops")
                : L.S("switch_to_clan_troops", "Clan Troops");

        [DataSourceProperty]
        public string HelpText => L.S("editor_help_text", "Help");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowFactionButton =>
            Screen == Screen.Troop
            && Player.Kingdom != null
            && !Config.NoKingdomTroops
            && !ClanScreen.IsGlobalEditorMode;

        [DataSourceProperty]
        public bool ShowDoctrinesButton =>
            Screen != Screen.Equipment
            && (Config.EnableDoctrines ?? false)
            && !ClanScreen.IsGlobalEditorMode;

        [DataSourceProperty]
        public bool ShowEquipmentButton => Screen != Screen.Doctrine;

        [DataSourceProperty]
        public bool ShowGlobalEditorLink => ClanScreen.IsGlobalEditorMode == false;

        [DataSourceProperty]
        public bool ShowPersonalEditorLink => ClanScreen.IsGlobalEditorMode == true;

        /* ━━━━━━━━ Brushes ━━━━━━━ */

        [DataSourceProperty]
        public string EquipmentButtonBrush =>
            Screen == Screen.Equipment ? "ButtonBrush3" : "ButtonBrush1";

        [DataSourceProperty]
        public string DoctrinesButtonBrush =>
            Screen == Screen.Doctrine ? "ButtonBrush3" : "ButtonBrush1";

        /* ━━━━━━━━ Screens ━━━━━━━ */

        [DataSourceProperty]
        public bool InTroopScreen => Screen == Screen.Troop;

        [DataSourceProperty]
        public bool InEquipmentScreen => Screen == Screen.Equipment;

        [DataSourceProperty]
        public bool InDoctrineScreen => Screen == Screen.Doctrine;

        /* ━━━━━━━ Tooltips ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel HelpHint =>
            Tooltip.MakeTooltip(
                null,
                L.S("editor_help_tooltip_text", "Open the online Retinues documentation.")
            );

        [DataSourceProperty]
        public BasicTooltipViewModel GlobalEditorHint =>
            Tooltip.MakeTooltip(
                null,
                L.S("global_editor_tooltip_text", "Open the global editor for all factions.")
            );

        [DataSourceProperty]
        public BasicTooltipViewModel PersonalEditorHint =>
            Tooltip.MakeTooltip(
                null,
                L.S(
                    "personal_editor_tooltip_text",
                    "Open the editor for your clan and kingdom troops."
                )
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Help ━━━━━━━━━ */

        /// <summary>
        /// Open the global editor view.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteShowGlobalEditor()
        {
            ClanScreen.Instance?.ExecuteOpenStudioMode();
        }

        /// <summary>
        /// Open the global editor view.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteShowPersonalEditor()
        {
            ClanScreen.Instance?.ExecuteOpenPlayerMode();
        }

        /// <summary>
        /// Open the documentation page.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteShowHelp()
        {
            const string url = "https://bolaft.github.io/bannerlord-retinues/";

            // Confirmation dialog
            var inquiry = new InquiryData(
                titleText: L.S("docs_title", "Open Documentation"),
                text: L.S(
                    "docs_body",
                    "This will open the Retinues documentation in your default web browser.\n\nContinue?"
                ),
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: GameTexts.FindText("str_ok").ToString(),
                negativeText: GameTexts.FindText("str_cancel").ToString(),
                affirmativeAction: () => URL.OpenInBrowser(url),
                negativeAction: () => { }
            );

            InformationManager.ShowInquiry(inquiry);
        }

        /* ━━━━━━ Mode Switch ━━━━━ */

        /// <summary>
        /// Toggle between equipment and troop screens.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteToggleEquipment() =>
            SwitchScreen(Screen == Screen.Equipment ? Screen.Troop : Screen.Equipment);

        /// <summary>
        /// Toggle between doctrines and troop screens.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteToggleDoctrines() =>
            SwitchScreen(Screen == Screen.Doctrine ? Screen.Troop : Screen.Doctrine);

        /* ━━━━ Faction Switch ━━━━ */

        /// <summary>
        /// Switch displayed faction between clan and kingdom troops.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteSwitchFaction() =>
            State.UpdateFaction(
                (StringIdentifier)State.Faction == Player.Clan ? Player.Kingdom : Player.Clan
            );

        /* ━━━━━━ Studio Mode ━━━━━ */

        /// <summary>
        /// Change the selected troop's culture.
        /// </summary>[DataSourceMethod]
        [DataSourceMethod]
        public void ExecuteSelectCulture()
        {
            try
            {
                // Collect all cultures from the object database.
                var cultures =
                    MBObjectManager
                        .Instance.GetObjectTypeList<CultureObject>()
                        ?.OrderBy(c => c?.Name?.ToString())
                        .ToList()
                    ?? [];

                if (cultures.Count == 0)
                {
                    Notifications.Popup(
                        L.T("no_cultures_title", "No Cultures Found"),
                        L.T("no_cultures_text", "No cultures are loaded in the current game.")
                    );
                    return;
                }

                // Build selection elements (single-select).
                var elements = new List<InquiryElement>(cultures.Count);

                foreach (var c in cultures)
                {
                    if (c?.Name == null)
                        continue;

                    var wc = new WCulture(c);
                    var root = wc.RootBasic ?? wc.RootElite;
                    var imageIdentifier = root?.ImageIdentifier;

                    // Mods (shokuho) can have weird behaviors, skip them.
                    if (imageIdentifier == null)
                        continue;

                    elements.Add(
                        new InquiryElement(c, wc.Name.ToString(), root.ImageIdentifier, true, null)
                    );
                }

                MBInformationManager.ShowMultiSelectionInquiry(
                    new MultiSelectionInquiryData(
                        titleText: L.S("select_culture_title", "Select Culture"),
                        descriptionText: null,
                        inquiryElements: elements,
                        isExitShown: true,
                        minSelectableOptionCount: 1,
                        maxSelectableOptionCount: 1,
                        affirmativeText: L.S("confirm", "Confirm"),
                        negativeText: L.S("cancel", "Cancel"),
                        affirmativeAction: selected =>
                        {
                            if (selected == null || selected.Count == 0)
                                return;
                            if (selected[0]?.Identifier is not CultureObject culture)
                                return;
                            if (culture.StringId == State.Faction.Culture.StringId)
                                return; // No change

                            // Refresh VM bindings & visuals.
                            State.UpdateFaction(new WCulture(culture));

                            // Notify UI
                            OnPropertyChanged(nameof(CultureBanner));
                            RefreshCultureBanner();
                            OnPropertyChanged(nameof(CultureName));
                        },
                        negativeAction: new Action<List<InquiryElement>>(_ => { })
                    )
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        [DataSourceMethod]
        public void ExecuteExportAll()
        {
            TroopImportExport.PromptAndExport(
                suggestedName: TroopImportExport.SuggestTimestampName("troops")
            );
        }

        [DataSourceMethod]
        public void ExecuteImportAll()
        {
            TroopImportExport.PickAndImportUnified(afterImport: () =>
            {
                // Refresh VM bindings & visuals after possible culture import
                State.UpdateFaction(State.Faction);
                OnPropertyChanged(nameof(CultureBanner));
                OnPropertyChanged(nameof(CultureName));
            });
        }

        [DataSourceMethod]
        public void ExecuteResetAll()
        {
            // Confirmation dialog
            var inquiry = new InquiryData(
                titleText: L.S("reset_all", "Reset All Troop Data"),
                text: L.S(
                    "reset_all_body",
                    "This will reset all culture troop data to their default values. Player clan and kingdom troops will not be affected.\n\nContinue?"
                ),
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: GameTexts.FindText("str_ok").ToString(),
                negativeText: GameTexts.FindText("str_cancel").ToString(),
                affirmativeAction: () =>
                {
                    FactionBehavior.ResetCultureTroops = true;
                    Notifications.Popup(
                        L.T("reset_culture_troops_title", "Culture Troops Reset"),
                        L.T(
                            "reset_culture_troops_text",
                            "All culture troops will be reset upon saving and reloading the game."
                        )
                    );
                },
                negativeAction: () => { }
            );

            InformationManager.ShowInquiry(inquiry);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void RefreshCultureBanner()
        {
            CultureBanner = BannerHelper.GetBannerImageFromCulture(
                State.Faction as WCulture,
                scale: 0.85f
            );
        }
    }
}
