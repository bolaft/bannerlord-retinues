using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Compatibility;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Character.Controllers;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Character.Views.Column
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
        //                      Captain Mode                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Character, UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool ShowCaptainModeToggle =>
            !State.Character.IsHero && DoctrineCatalog.Captains.IsAcquired;

        [DataSourceProperty]
        public Icon CaptainModeIcon { get; } =
            new(
                tooltip: new Tooltip(L.T("captain_mode_toggle_tooltip", "Captain mode.")),
                refresh: [UIEvent.Character, UIEvent.Doctrine],
                spriteFactory: () =>
                    State.Character.IsCaptain
                        ? @"Encyclopedia\star_without_glow"
                        : @"Encyclopedia\star_outline",
                visibilityGate: () => !State.Character.IsHero && DoctrineCatalog.Captains.IsAcquired
            );

        [DataSourceProperty]
        public Checkbox CaptainModeToggle { get; } =
            new(
                action: CaptainsController.ToggleCaptainMode,
                getSelected: () => State.Character?.IsCaptain ?? false,
                refresh: [UIEvent.Character, UIEvent.Doctrine],
                visibilityGate: () => !State.Character.IsHero && DoctrineCatalog.Captains.IsAcquired
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Mariner                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowMarinerToggle => Mods.NavalDLC.IsLoaded && !State.Character.IsHero;

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
                visibilityGate: () => Mods.NavalDLC.IsLoaded && !State.Character.IsHero
            );

        [DataSourceProperty]
        public Checkbox MarinerToggle { get; } =
            new(
                action: CharacterController.SetMariner,
                getSelected: () => State.Character?.IsMariner ?? false,
                refresh: [UIEvent.Formation],
                visibilityGate: () => Mods.NavalDLC.IsLoaded && !State.Character.IsHero
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Mixed Gender                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowMixedGenderToggle => !State.Character.IsHero;

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
                visibilityGate: () => !State.Character.IsHero
            );

        [DataSourceProperty]
        public Checkbox MixedGenderToggle { get; } =
            new(
                action: CharacterController.SetMixedGender,
                getSelected: () => State.Character?.IsMixedGender ?? false,
                refresh: [UIEvent.Character],
                visibilityGate: () => !State.Character.IsHero
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
        //                     Captain Enabled                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<WCharacter> ToggleCaptainEnabledButton { get; } =
            new(
                action: CaptainsController.ToggleCaptainEnabled,
                arg: () => State.Character,
                refresh: [UIEvent.Character],
                spriteFactory: () =>
                    State.Character?.IsCaptainEnabled == true
                        ? "Popup.Delete.Button"
                        : "Popup.Done.Button",
                labelFactory: () =>
                    State.Character?.IsCaptainEnabled == true
                        ? L.S("button_disable_captain", "Disable")
                        : L.S("button_enable_captain", "Enable"),
                visibilityGate: () => State.Character?.IsCaptain == true
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rank Up                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<WCharacter> RankUpButton { get; } =
            new(
                action: RetinueController.RankUp,
                arg: () => State.Character,
                label: L.S("button_rank_up", "Rank Up"),
                refresh: [UIEvent.Skill, UIEvent.Doctrine],
                visibilityGate: () => State.Mode == EditorMode.Player && State.Character.IsRetinue
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Remove                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<WCharacter> RemoveCharacterButton { get; } =
            new(
                action: UpgradeController.RemoveCharacter,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Tree],
                label: L.S("button_remove_character", "Delete"),
                visibilityGate: () =>
                    State.Character.IsHero == false
                    && State.Character.IsCaptain == false
                    && State.Character.IsRetinue == false
            );
    }
}
