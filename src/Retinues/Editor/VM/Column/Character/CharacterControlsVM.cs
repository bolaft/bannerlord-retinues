using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers.Character;
using Retinues.Editor.Events;
using Retinues.Modules;
using Retinues.UI.Services;
using Retinues.UI.VM;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Column.Character
{
    public class CharacterControlsVM : EventListenerVM
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

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowMarinerToggle => Mods.NavalDLC.IsLoaded && !State.Character.IsHero;

        [EventListener(UIEvent.Formation)]
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

        [EventListener(UIEvent.Tree, UIEvent.Character)]
        [DataSourceProperty]
        public bool CanRemoveCharacter =>
            CharacterTreeController.RemoveCharacter.Allow(State.Character);

        [EventListener(UIEvent.Tree, UIEvent.Character)]
        [DataSourceProperty]
        public Tooltip CanRemoveCharacterTooltip =>
            CharacterTreeController.RemoveCharacter.Tooltip(State.Character);

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
