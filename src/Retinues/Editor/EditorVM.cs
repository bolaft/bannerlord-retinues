using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Common.Column.Views;
using Retinues.Editor.MVC.Common.TopPanel.View;
using Retinues.Editor.MVC.Pages.Character.Views.List;
using Retinues.Editor.MVC.Pages.Character.Views.Panel;
using Retinues.Editor.MVC.Pages.Doctrines.Views.List;
using Retinues.Editor.MVC.Pages.Doctrines.Views.Panel;
using Retinues.Editor.MVC.Pages.Equipment.Views.List;
using Retinues.Editor.MVC.Pages.Equipment.Views.Panel;
using Retinues.Editor.MVC.Pages.Library.Views.List;
using Retinues.Editor.MVC.Pages.Library.Views.Panel;
using Retinues.Editor.MVC.Pages.Settings.Views.Panel;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Services;
using TaleWorlds.Library;

namespace Retinues.Editor
{
    /// <summary>
    /// Root editor ViewModel; initializes shared state and child VMs.
    /// </summary>
    public class EditorVM : EventListenerVM
    {
        private readonly Action _close;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorVM(Action close, EditorLaunchArgs args = null)
        {
            // Set close action.
            _close = close;

            // Equipment list owns view-only options (e.g., crafted filter).
            _equipmentList = new EquipmentListVM();
            _column = new ColumnVM(_equipmentList);

            // Start each editor session from a clean shared state.
            EditorState.Reset(args);

            // Initial refresh.
            OnPageChanged();
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
        //                       Background                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IndoorsBackground => State.Mode == EditorMode.Player;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ Top Panel ━━━━━━ */

        private readonly TopPanelVM _topPanel = new();

        [DataSourceProperty]
        public TopPanelVM TopPanel => _topPanel;

        /* ━━━━━━━━ Column ━━━━━━━━ */

        private readonly ColumnVM _column;

        [DataSourceProperty]
        public ColumnVM Column => _column;

        /* ━━━━━━━━━ List ━━━━━━━━━ */

        private readonly CharacterListVM _characterList = new();
        private readonly EquipmentListVM _equipmentList;
        private readonly DoctrinesListVM _doctrinesList = new();
        private readonly LibraryListVM _libraryList = new();

        private BaseListVM _list;

        [DataSourceProperty]
        public BaseListVM List
        {
            get => _list;
            private set
            {
                if (value != _list)
                {
                    _list = value;
                    OnPropertyChanged(nameof(List));
                }
            }
        }

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsListVisible => State.Page != EditorPage.Settings;

        /* ━━━━━━━━━ Panel ━━━━━━━━ */

        private readonly CharacterPanelVM _characterPanel = new();
        private readonly EquipmentPanelVM _equipmentPanel = new();
        private readonly DoctrinesPanelVM _doctrinesPanel = new();
        private readonly LibraryPanelVM _libraryPanel = new();
        private readonly SettingsPanelVM _settingsPanel = new();

        private BasePanelVM _panel;

        [DataSourceProperty]
        public BasePanelVM Panel
        {
            get => _panel;
            private set
            {
                if (value != _panel)
                {
                    _panel = value;
                    OnPropertyChanged(nameof(Panel));
                }
            }
        }

        /* ━━━━━━━━ Refresh ━━━━━━━ */

        [EventListener(UIEvent.Page)]
        protected void OnPageChanged()
        {
            List = State.Page switch
            {
                EditorPage.Character => _characterList,
                EditorPage.Equipment => _equipmentList,
                EditorPage.Doctrines => _doctrinesList,
                EditorPage.Library => _libraryList,
                _ => null,
            };

            Panel = State.Page switch
            {
                EditorPage.Character => _characterPanel,
                EditorPage.Equipment => _equipmentPanel,
                EditorPage.Doctrines => _doctrinesPanel,
                EditorPage.Library => _libraryPanel,
                EditorPage.Settings => _settingsPanel,
                _ => null,
            };
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
                    return;

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
}
