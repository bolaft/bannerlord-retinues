using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Common.Column.Views;
using Retinues.GUI.Editor.Modules.Common.TopPanel.View;
using Retinues.GUI.Editor.Modules.Pages.Character.Views.List;
using Retinues.GUI.Editor.Modules.Pages.Character.Views.Panel;
using Retinues.GUI.Editor.Modules.Pages.Doctrines.Views.List;
using Retinues.GUI.Editor.Modules.Pages.Doctrines.Views.Panel;
using Retinues.GUI.Editor.Modules.Pages.Equipment.Views.List;
using Retinues.GUI.Editor.Modules.Pages.Equipment.Views.Panel;
using Retinues.GUI.Editor.Modules.Pages.Library.Views.List;
using Retinues.GUI.Editor.Shared.Views;
using Retinues.GUI.Editor.VM.Panel.Library;
using Retinues.GUI.Services;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor
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

        private readonly ColumnVM _column = new();

        [DataSourceProperty]
        public ColumnVM Column => _column;

        /* ━━━━━━━━━ List ━━━━━━━━━ */

        private readonly CharacterListVM _characterList = new();
        private readonly EquipmentListVM _equipmentList = new();
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

        /* ━━━━━━━━━ Panel ━━━━━━━━ */

        private readonly CharacterPanelVM _characterPanel = new();
        private readonly EquipmentPanelVM _equipmentPanel = new();
        private readonly DoctrinesPanelVM _doctrinesPanel = new();
        private readonly LibraryPanelVM _libraryPanel = new();

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
