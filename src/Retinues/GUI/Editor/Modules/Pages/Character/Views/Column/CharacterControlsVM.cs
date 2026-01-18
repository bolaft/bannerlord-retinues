using Retinues.Compatibility;
using Retinues.Domain.Characters.Wrappers;
using Retinues.GUI.Components;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Pages.Character.Controllers;
using Retinues.GUI.Services;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Modules.Pages.Character.Views.Column
{
    public class CharacterControlsVM : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => State.Page == EditorPage.Character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Mariner                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowMarinerToggle =>
            Mods.NavalDLC.IsLoaded && State.Character != null && !State.Character.IsHero;

        [DataSourceProperty]
        public Icon MarinerIcon { get; } =
            new(
                tooltipFactory: () =>
                    State.Mode == EditorMode.Universal
                        ? new Tooltip(
                            L.T(
                                "mariner_toggle_tooltip_universal",
                                "Mariners are better suited for naval combat."
                            )
                        )
                        : new Tooltip(
                            L.T(
                                "mariner_toggle_tooltip",
                                "Mariners are better suited for naval combat, but earn skill points at a slightly reduced rate."
                            )
                        ),
                refresh: [UIEvent.Formation],
                visibilityGate: () =>
                    Mods.NavalDLC.IsLoaded && State.Character != null && !State.Character.IsHero
            );

        [DataSourceProperty]
        public Checkbox MarinerToggle { get; } =
            new(
                action: CharacterController.SetMariner,
                getSelected: () => State.Character?.IsMariner ?? false,
                refresh: [UIEvent.Formation],
                visibilityGate: () =>
                    Mods.NavalDLC.IsLoaded && State.Character != null && !State.Character.IsHero
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Mixed Gender                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowMixedGenderToggle => State.Character != null && !State.Character.IsHero;

        [DataSourceProperty]
        public Icon MixedGenderIcon { get; } =
            new(
                tooltipFactory: () =>
                    new Tooltip(L.T("mixed_gender_toggle_tooltip", "Mixed gender unit.")),
                refresh: [UIEvent.Character, UIEvent.Gender],
                spriteFactory: () =>
                {
                    var c = State.Character;
                    if (c == null)
                        return null;

                    return c.IsFemale
                        ? "SPGeneral\\GeneralFlagIcons\\male_only"
                        : "SPGeneral\\GeneralFlagIcons\\female_only";
                },
                visibilityGate: () => State.Character != null && !State.Character.IsHero
            );

        [DataSourceProperty]
        public Checkbox MixedGenderToggle { get; } =
            new(
                action: CharacterController.SetMixedGender,
                getSelected: () => State.Character?.IsMixedGender ?? false,
                refresh: [UIEvent.Character],
                visibilityGate: () => State.Character != null && !State.Character.IsHero
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowFormationClass => State.Character?.IsHero == false;

        [DataSourceProperty]
        public Icon FormationClassIcon { get; } =
            new(
                tooltipFactory: () =>
                {
                    var c = State.Character;
                    if (c == null)
                        return null;

                    return new Tooltip(
                        L.T("formation_class_icon_tooltip", "Formation: {CLASS}.")
                            .SetTextVariable(
                                "CLASS",
                                c.FormationClass.GetLocalizedName().ToString().ToLower()
                            )
                    );
                },
                spriteFactory: () => Icons.GetFormationClassIcon(State.Character),
                refresh: [UIEvent.Formation],
                visibilityGate: () => State.Character != null
            );

        [DataSourceProperty]
        public Checkbox FormationClassButton { get; } =
            new(
                action: FormationClassController.ChangeFormationClass,
                getSelected: () =>
                    State.Character != null
                    && State.Character.FormationClassOverride != FormationClass.Unset,
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
