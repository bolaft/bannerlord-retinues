using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Game;
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

            if (ClanScreen.IsStudioMode)
            {
                RefreshCultureBanner();
                RefreshClanBanner();
            }

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

            if (ClanScreen.IsStudioMode && value == Screen.Doctrine)
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

# if BL13
        private BannerImageIdentifierVM _clanBanner;

        [DataSourceProperty]
        public BannerImageIdentifierVM ClanBanner
# else
        private ImageIdentifierVM _clanBanner;

        [DataSourceProperty]
        public ImageIdentifierVM ClanBanner
# endif
        {
            get => _clanBanner;
            private set
            {
                if (_clanBanner == value)
                    return;
                _clanBanner = value;
                OnPropertyChanged(nameof(ClanBanner));
            }
        }

        [DataSourceProperty]
        public string CultureName => State.Faction?.Culture?.Name;

        [DataSourceProperty]
        public string ClanName
        {
            get
            {
                if (State.Faction is WClan clan)
                    return clan.Name;
                if (State.Faction is WFaction faction)
                    return faction.Name;
                if (State.Faction is WCulture culture)
                    return WClan
                        .All.FirstOrDefault(c => c?.Culture?.StringId == culture.StringId)
                        ?.Name;
                return null;
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel CultureBannerHint =>
            Tooltip.MakeTooltip(null, L.S("select_culture_hint", "Select a culture."));

        [DataSourceProperty]
        public BasicTooltipViewModel ClanBannerHint =>
            Tooltip.MakeTooltip(null, L.S("select_clan_hint", "Select a clan."));

        /* ━━━━━━━ 3D Model ━━━━━━━ */

        [DataSourceProperty]
        public CharacterViewModel Model => State.Troop?.GetModel(State.Equipment.Index);

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
            State.Faction == Player.Clan
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
            && !ClanScreen.IsStudioMode;

        [DataSourceProperty]
        public bool ShowDoctrinesButton =>
            Screen != Screen.Equipment
            && (Config.EnableDoctrines ?? false)
            && !ClanScreen.IsStudioMode;

        [DataSourceProperty]
        public bool ShowEquipmentButton => Screen != Screen.Doctrine;

        [DataSourceProperty]
        public bool ShowGlobalEditorLink => ClanScreen.IsStudioMode == false;

        [DataSourceProperty]
        public bool ShowPersonalEditorLink => ClanScreen.IsStudioMode == true;

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
            State.UpdateFaction(State.Faction == Player.Clan ? Player.Kingdom : Player.Clan);

        /* ━━━━━━ Studio Mode ━━━━━ */

        /// <summary>
        /// Change the selected culture.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteSelectCulture()
        {
            try
            {
                List<InquiryElement> elements = [];

                // Build selection elements (single-select).
                foreach (var wc in WCulture.All)
                {
                    if (wc?.Name == null)
                        continue;

                    // Mods (shokuho) can have weird behaviors, skip them.
                    if (wc.RootBasic == null && wc.RootElite == null)
                        continue;

                    elements.Add(
                        new InquiryElement(wc.Base, wc.Name, wc.ImageIdentifier, true, null)
                    );
                }

                if (elements.Count == 0)
                {
                    Notifications.Popup(
                        L.T("no_cultures_title", "No Cultures Found"),
                        L.T("no_cultures_text", "No cultures are loaded in the current game.")
                    );
                    return;
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

                            // Update editor mode
                            ClanScreen.EditorMode = EditorMode.Culture;

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

        /// <summary>
        /// Change the selected clan.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteSelectClan()
        {
            try
            {
                // Build selection elements (single-select).
                List<InquiryElement> elements = [];

                foreach (var wc in WClan.All)
                {
                    if (wc?.Name == null)
                        continue;

                    elements.Add(
                        new InquiryElement(wc.Base, wc.Name, wc.ImageIdentifier, true, null)
                    );
                }

                if (elements.Count == 0)
                {
                    Notifications.Popup(
                        L.T("no_clans_title", "No Clans Found"),
                        L.T("no_clans_text", "No clans are loaded in the current game.")
                    );
                    return;
                }

                MBInformationManager.ShowMultiSelectionInquiry(
                    new MultiSelectionInquiryData(
                        titleText: L.S("select_clan_title", "Select Clan"),
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
                            if (selected[0]?.Identifier is not Clan clan)
                                return;
                            if (clan.StringId == State.Faction.StringId)
                                return; // No change

                            // Update editor mode
                            ClanScreen.EditorMode = EditorMode.Heroes;

                            // Refresh VM bindings & visuals.
                            State.UpdateFaction(new WClan(clan));
                            // Notify UI
                            OnPropertyChanged(nameof(ClanBanner));
                            RefreshClanBanner();
                            OnPropertyChanged(nameof(ClanName));
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
                OnPropertyChanged(nameof(ClanBanner));
                OnPropertyChanged(nameof(CultureName));
                OnPropertyChanged(nameof(ClanName));
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

        private void RefreshClanBanner()
        {
            WClan clan;

            if (ClanScreen.EditorMode == EditorMode.Culture)
            {
                var culture = State.Faction.Culture;
                if (culture == null)
                    return;

                // Find first clan with this culture
                clan = WClan.All.FirstOrDefault(c => c?.Culture?.StringId == culture.StringId);
            }
            else if (ClanScreen.EditorMode == EditorMode.Heroes)
            {
                // The faction is a clan
                clan = State.Faction as WClan;
            }
            else
            {
                // Use main hero's clan
                clan = new WClan(Hero.MainHero.Clan);
            }

            ClanBanner = clan.GetBannerImage(scale: 0.85f);
        }

        private void RefreshCultureBanner()
        {
            var culture = State.Faction.Culture;
            CultureBanner = culture?.GetBannerImage(scale: 0.85f);
        }
    }
}
