using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Helpers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.GUI.Components;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Common.Column.Views;
using Retinues.GUI.Editor.Modules.Pages.Doctrines.Views.Panel;
using Retinues.GUI.Editor.Modules.Pages.Equipment.Views.Panel;
using Retinues.GUI.Editor.Shared.Views;
using Retinues.GUI.Editor.VM.Panel.Character;
using Retinues.GUI.Editor.VM.Panel.Library;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
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

        private BaseListVM _list;

        [DataSourceProperty]
        public BaseListVM List
        {
            get => _list;
            private set
            {
                if (value == _list)
                    return;

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
                    return;

                _column = value;
                OnPropertyChanged(nameof(Column));
            }
        }

        private BasePanelVM _panel;

        [DataSourceProperty]
        public BasePanelVM Panel
        {
            get => _panel;
            private set
            {
                if (value == _panel)
                    return;

                _panel = value;
                OnPropertyChanged(nameof(Panel));
            }
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
