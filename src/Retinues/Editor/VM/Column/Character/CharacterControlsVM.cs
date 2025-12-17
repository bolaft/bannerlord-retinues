using System.Diagnostics.Tracing;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Helpers;
using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Editor.VM.Column.Character
{
    public class CharacterControlsVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => EditorVM.Page == EditorPage.Character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Remove Button                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string RemoveCharacterButtonText => L.S("button_remove_character", "Delete");

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowRemoveCharacterButton => State.Character.IsHero == false;

        [DataSourceProperty]
        public bool CanRemoveCharacter => _cantRemoveCharacterReason == null;

        [DataSourceProperty]
        public Tooltip CanRemoveCharacterTooltip =>
            _cantRemoveCharacterReason == null ? null : new Tooltip(_cantRemoveCharacterReason);

        private TextObject _cantRemoveCharacterReason = null;

        [EventListener(UIEvent.Tree, UIEvent.Character)]
        private void UpdateRemoveButtonState()
        {
            CharacterController.CanRemoveCharacter(out _cantRemoveCharacterReason);

            OnPropertyChanged(nameof(CanRemoveCharacter));
            OnPropertyChanged(nameof(CanRemoveCharacterTooltip));
        }

        [DataSourceMethod]
        public void ExecuteRemoveCharacter() => CharacterController.RemoveCharacter();
    }
}
