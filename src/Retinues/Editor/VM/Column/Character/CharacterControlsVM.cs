using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers.Character;
using Retinues.Helpers;
using Retinues.Module;
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
        //                         Mariner                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool ShowMarinerToggle => Mods.NavalDLC.IsLoaded && !State.Character.IsHero;

        [EventListener(UIEvent.Character, UIEvent.Formation)]
        [DataSourceProperty]
        public bool IsMariner
        {
            get => State.Character.IsMariner;
            set => CharacterController.ChangeMariner(value);
        }

        [DataSourceProperty]
        public Tooltip MarinerTooltip =>
            State.Mode == EditorMode.Universal
                ? new Tooltip(
                    L.S(
                        "mariner_toggle_tooltip_universal",
                        "Set this unit's mariner ability.\nMariners are better suited for naval combat."
                    )
                )
                : new Tooltip(
                    L.S(
                        "mariner_toggle_tooltip",
                        "Set this unit's mariner ability.\nMariners are better suited for naval combat, but earn skill points at a slightly reduced rate."
                    )
                );

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
            _cantRemoveCharacterReason = CharacterTreeController.RemoveCharacter.Reason(
                State.Character
            );

            OnPropertyChanged(nameof(CanRemoveCharacter));
            OnPropertyChanged(nameof(CanRemoveCharacterTooltip));
        }

        [DataSourceMethod]
        public void ExecuteRemoveCharacter() =>
            CharacterTreeController.RemoveCharacter.Execute(State.Character);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Export Button                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Tooltip ExportTooltip =>
            new(
                L.S(
                    "button_export_character_tooltip",
                    "Save this character and add it to the library."
                )
            );

        [DataSourceMethod]
        public void ExecuteExport() => CharacterController.ExportSelectedCharacter();
    }
}
