using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Editor.MVC.Shared.Services.Appearance;
using Retinues.Interface.Services;

namespace Retinues.Editor.MVC.Common.Column.Controllers
{
    /// <summary>
    /// Controller for gender/species toggling and related validation.
    /// </summary>
    public class GenderController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Gender                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Cycles the selected character's gender through:
        /// male → female → male+mixed → female+mixed → male
        /// </summary>
        public static ControllerAction<WCharacter> CycleGender { get; } =
            Action<WCharacter>("CycleGender")
                .AddCondition(
                    c => c?.IsHero != true,
                    L.T("mixed_gender_hero_reason", "Not applicable to heroes")
                )
                .AddCondition(
                    applies: _ =>
                        RaceHelper.HasAlternateSpecies()
                        && State.Character != null
                        && !State.Character.IsHero,
                    test: c =>
                    {
                        if (c == null)
                            return true;

                        // Every transition toggles IsFemale; target is always !IsFemale.
                        return RaceHelper.FindTemplate(c.Culture, !c.IsFemale, c.Race) != null;
                    },
                    reason: L.T("gender_no_template", "Invalid gender/species/culture combination")
                )
                .AddCondition(
                    applies: _ =>
                        RaceHelper.HasAlternateSpecies()
                        && State.Character != null
                        && !State.Character.IsHero,
                    test: c =>
                    {
                        if (c == null)
                            return true;

                        return AppearanceGuard.CanRender(c.Culture, !c.IsFemale, c.Race);
                    },
                    reason: L.T("gender_no_template", "Invalid gender/species/culture combination")
                )
                .DefaultTooltip(L.T("gender_cycle_hint", "Cycle gender: male, female, mixed"))
                .ExecuteWith(CycleGenderImpl)
                .Fire(UIEvent.Gender);

        private static void CycleGenderImpl(WCharacter c)
        {
            if (c == null)
                return;

            if (!c.IsFemale && !c.IsMixedGender)
            {
                // male → female
                ToggleGenderImpl(c);
            }
            else if (c.IsFemale && !c.IsMixedGender)
            {
                // female → male+mixed
                c.IsMixedGender = true;
                ToggleGenderImpl(c);
            }
            else if (!c.IsFemale && c.IsMixedGender)
            {
                // male+mixed → female+mixed
                ToggleGenderImpl(c);
            }
            else
            {
                // female+mixed → male: clear mixed, toggle to male
                c.IsMixedGender = false;
                ToggleGenderImpl(c);
            }
        }

        /// <summary>
        /// Toggles the selected character's gender/species when valid and fires a gender update event.
        /// </summary>
        public static ControllerAction<WCharacter> ToggleGender { get; } =
            Action<WCharacter>("ToggleGender")
                .AddCondition(
                    applies: _ =>
                        RaceHelper.HasAlternateSpecies()
                        && State.Character != null
                        && !State.Character.IsHero,
                    test: c =>
                    {
                        if (c == null)
                            return true;

                        var targetFemale = !c.IsFemale;
                        return RaceHelper.FindTemplate(c.Culture, targetFemale, c.Race) != null;
                    },
                    reason: L.T("gender_no_template", "Invalid gender/species/culture combination")
                )
                .AddCondition(
                    applies: _ =>
                        RaceHelper.HasAlternateSpecies()
                        && State.Character != null
                        && !State.Character.IsHero,
                    test: c =>
                    {
                        if (c == null)
                            return true;

                        var targetFemale = !c.IsFemale;
                        return AppearanceGuard.CanRender(c.Culture, targetFemale, c.Race);
                    },
                    reason: L.T("gender_no_template", "Invalid gender/species/culture combination")
                )
                .DefaultTooltip(L.T("gender_toggle_hint", "Change gender"))
                .ExecuteWith(c => ToggleGenderImpl(c ?? State.Character))
                .Fire(UIEvent.Gender);

        /// <summary>
        /// Toggle the gender of the given character.
        /// </summary>
        private static void ToggleGenderImpl(WCharacter character)
        {
            if (character == null)
                return;

            AppearanceGuard.TryApply(
                () =>
                {
                    character.IsFemale = !character.IsFemale;

                    character.ApplyCultureBodyProperties();

                    return true;
                },
                character
            );
        }
    }
}
