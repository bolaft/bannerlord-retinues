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
        /// Toggles the selected character's gender/species when valid and fires a gender update event.
        /// </summary>
        public static ControllerAction<WCharacter> ToggleGender { get; } =
            Action<WCharacter>("ToggleGender")
                .AddCondition(
                    applies: _ =>
                        RaceHelper.HasAlternateSpecies() && State.Character?.Editable is WCharacter,
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
                        RaceHelper.HasAlternateSpecies() && State.Character?.Editable is WCharacter,
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
                .ExecuteWith(c => ToggleGenderImpl((c ?? State.Character)?.Editable))
                .Fire(UIEvent.Gender);

        /// <summary>
        /// Toggle the gender of the given character.
        /// </summary>
        private static void ToggleGenderImpl(Domain.Characters.ICharacterData character)
        {
            if (character == null)
                return;

            AppearanceGuard.TryApply(
                () =>
                {
                    character.IsFemale = !character.IsFemale;

                    if (character is WCharacter wc)
                        wc.ApplyCultureBodyProperties();

                    return true;
                },
                character as WCharacter
            );
        }
    }
}
