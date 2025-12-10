using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Engine;
using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Editor.VM.Panel.Character
{
    /// <summary>
    /// Character details panel.
    /// </summary>
    public class CharacterPanel : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        IsVisible                       //
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

        [EventListener(UIEvent.Mode)]
        private void ToggleVisibility()
        {
            IsVisible = EditorVM.Mode == EditorMode.Character;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Name                           //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string NameHeaderText => L.S("name_header_text", "Name");

        [EventListener(UIEvent.Troop, UIEvent.Name)]
        [DataSourceProperty]
        public string Name => State.Character.Name;

        /// <summary>
        /// Prompt to rename the selected character.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteRename()
        {
            var character = State.Character;
            if (character == null)
                return;

            Notifications.TextInputPopup(
                title: L.T("rename_troop", "Rename Troop"),
                defaultInput: character.Name,
                onConfirm: input => CharacterController.ChangeName(input.Trim()),
                description: L.T("enter_new_name", "Enter a new name:")
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Culture                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string CultureHeaderText => L.S("culture_header_text", "Culture");

        [EventListener(UIEvent.Troop, UIEvent.Culture)]
        [DataSourceProperty]
        public string CultureText
        {
            get
            {
                var culture = State.Character?.Culture;
                var name = culture?.Name;

                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }

                return L.S("unknown", "Unknown");
            }
        }

        /// <summary>
        /// Change the selected character's culture.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteChangeCulture()
        {
            // TODO: implement culture selection logic later.
        }
    }
}
