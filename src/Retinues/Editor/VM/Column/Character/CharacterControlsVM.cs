using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Controllers.Character;
using Retinues.Editor.Events;
using Retinues.Modules;
using Retinues.UI.Services;
using Retinues.UI.VM;
using TaleWorlds.Core;
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
        public bool ShowMarinerToggle =>
            Mods.NavalDLC.IsLoaded && State.Character != null && !State.Character.IsHero;

        [DataSourceProperty]
        public Checkbox MarinerToggle { get; } =
            new(
                action: CharacterController.SetMariner,
                getSelected: () => State.Character?.IsMariner ?? false,
                refresh: [UIEvent.Character, UIEvent.Formation],
                visibilityGate: () =>
                    Mods.NavalDLC.IsLoaded && State.Character != null && !State.Character.IsHero
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Formation)]
        [DataSourceProperty]
        public string FormationClassIcon => Icons.GetFormationClassIcon(State.Character);

        [DataSourceProperty]
        public Button<WCharacter> ChangeFormationClassButton { get; } =
            new(
                action: FormationClassController.ChangeFormationClass,
                label: State.Character.FormationClass.GetLocalizedName().ToString(),
                arg: () => State.Character,
                refresh: [UIEvent.Formation]
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
