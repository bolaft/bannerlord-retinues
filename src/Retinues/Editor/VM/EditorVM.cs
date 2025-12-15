using System;
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Editor.VM.Column;
using Retinues.Editor.VM.List;
using Retinues.Editor.VM.Panel.Character;
using Retinues.Editor.VM.Panel.Equipment;
using Retinues.Helpers;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    /// <summary>
    /// Editor pages available in the editor.
    /// </summary>
    public enum EditorPage
    {
        Character = 0,
        Equipment = 1,
        Doctrines = 2,
    }

    /// <summary>
    /// Root editor ViewModel; initializes shared state and child VMs.
    /// </summary>
    public class EditorVM : BaseVM
    {
        private readonly Action _close;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorVM(Action close, EditorLaunchArgs args = null)
        {
            _close = close;

            // Initialize the troop list VM.
            List = new ListVM();

            // Initialize the tableau VM.
            Column = new ColumnVM();

            // Initialize the character panel VM.
            CharacterPanel = new CharacterPanelVM();

            // Initialize the equipment panel VM.
            EquipmentPanel = new EquipmentPanelVM();

            // Page defaults to character editing.
            SetPage(EditorPage.Character);

            // Start each editor session from a clean shared state.
            State.Reset(args);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rooting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteClose()
        {
            _close?.Invoke();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Done Button                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string DoneButtonText => L.S("editor_done_button", "Done");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Top Panel                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string EditorTitle => L.S("editor_title", "Troop Editor");

        /* ━━━━━━━━ Culture ━━━━━━━ */

        [EventListener(UIEvent.CultureFaction)]
        [DataSourceProperty]
        public string CultureName =>
            State.Culture?.Name?.ToString() ?? L.S("editor_culture_select", "Select a Culture");

        [EventListener(UIEvent.CultureFaction)]
        [DataSourceProperty]
        public object CultureBanner => State.Culture?.Image ?? Banners.EmptyImage;

        [DataSourceProperty]
        public Tooltip CultureBannerHint => new(L.S("editor_culture_select", "Select a Culture"));

        [DataSourceMethod]
        public void ExecuteSelectCulture()
        {
            var elements = new List<InquiryElement>();

            foreach (var culture in WCulture.All)
            {
                var imageIdentifier = culture.ImageIdentifier;
                var name = culture.Name;

                if (imageIdentifier == null || name == null)
                    continue;

                elements.Add(
                    new InquiryElement(
                        identifier: culture,
                        title: name,
                        imageIdentifier: imageIdentifier
                    )
                );
            }

            if (elements.Count == 0)
            {
                Inquiries.Popup(
                    L.T("no_cultures_title", "No Cultures Found"),
                    L.T("no_cultures_text", "No cultures are loaded in the current game.")
                );
                return;
            }

            Inquiries.SelectPopup(
                title: L.T("select_culture_title", "Select Culture"),
                elements: elements,
                onSelect: element =>
                    FactionController.SelectCulture(element?.Identifier as WCulture)
            );
        }

        /* ━━━━━━━━━ Clan ━━━━━━━━━ */

        [EventListener(UIEvent.ClanFaction)]
        [DataSourceProperty]
        public string ClanName =>
            State.Clan?.Name?.ToString() ?? L.S("editor_clan_select", "Select a Clan");

        [EventListener(UIEvent.ClanFaction)]
        [DataSourceProperty]
        public object ClanBanner => State.Clan?.Image ?? Banners.EmptyImage;

        [DataSourceProperty]
        public Tooltip ClanBannerHint => new(L.S("editor_clan_select", "Select a Clan"));

        [DataSourceMethod]
        public void ExecuteSelectClan()
        {
            var elements = new List<InquiryElement>();

            foreach (var clan in WClan.All)
            {
                Log.Info($"Found clan: {clan.Name}");
                Log.Info($"Clan culture: {clan.Culture?.Name}");
                Log.Info($"Selected culture: {State.Culture?.Name}");
                if (State.Culture != null && clan.Culture != State.Culture)
                    continue;

                var imageIdentifier = clan.ImageIdentifier;
                var name = clan.Name;

                if (imageIdentifier == null || name == null)
                {
                    Log.Info($"Skipping clan {clan.Name} due to missing image or name.");
                    continue;
                }

                elements.Add(
                    new InquiryElement(
                        identifier: clan,
                        title: name,
                        imageIdentifier: imageIdentifier
                    )
                );
            }

            if (elements.Count == 0)
            {
                Inquiries.Popup(
                    L.T("no_clans_title", "No Clans Found"),
                    L.T("no_clans_text", "No clans are loaded in the current game.")
                );
                return;
            }

            Inquiries.SelectPopup(
                title: L.T("select_clan_title", "Select Clan"),
                elements: elements,
                onSelect: element => FactionController.SelectClan(element?.Identifier as WClan)
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private ListVM _list;

        [DataSourceProperty]
        public ListVM List
        {
            get => _list;
            private set
            {
                if (value == _list)
                {
                    return;
                }

                _list = value;
                OnPropertyChanged(nameof(List));
            }
        }

        private ColumnVM _column;

        [DataSourceProperty]
        public ColumnVM Column
        {
            get => _column;
            private set
            {
                if (value == _column)
                {
                    return;
                }

                _column = value;
                OnPropertyChanged(nameof(Column));
            }
        }

        private CharacterPanelVM _characterPanel;

        [DataSourceProperty]
        public CharacterPanelVM CharacterPanel
        {
            get => _characterPanel;
            private set
            {
                if (value == _characterPanel)
                {
                    return;
                }

                _characterPanel = value;
                OnPropertyChanged(nameof(CharacterPanel));
            }
        }

        private EquipmentPanelVM _equipmentPanel;

        [DataSourceProperty]
        public EquipmentPanelVM EquipmentPanel
        {
            get => _equipmentPanel;
            private set
            {
                if (value == _equipmentPanel)
                {
                    return;
                }

                _equipmentPanel = value;
                OnPropertyChanged(nameof(EquipmentPanel));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Page                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorPage Page = EditorPage.Character;

        public static void SetPage(EditorPage page)
        {
            if (Page == page)
                return;

            Page = page;

            // Notify any listeners that page changed (columns, buttons, etc.).
            EventManager.Fire(UIEvent.Page, EventScope.Global);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _isVisible;

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible)
                {
                    return;
                }

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
}
