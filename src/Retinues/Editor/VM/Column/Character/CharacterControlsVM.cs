using Retinues.Domain.Characters.Wrappers;
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
        //                         Export                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<WCharacter> ExportButton { get; } =
            new(
                action: CharacterController.ExportCharacter,
                arg: () => State.Character,
                refresh: [UIEvent.Character],
                sprite: "SPGeneral\\Skills\\gui_skills_icon_steward_tiny",
                color: "f8eed1ff"
            );

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
            set => CharacterController.SetMariner.Execute(value);
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
        //                        Remove                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<WCharacter> RemoveCharacterButton { get; } =
            new(
                action: CharacterTreeController.RemoveCharacter,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Tree],
                label: L.S("button_remove_character", "Delete"),
                visibilityGate: () => State.Character.IsHero == false
            );
    }
}
