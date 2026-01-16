using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Editor.Shared.Services.Appearance;
using Retinues.GUI.Services;

namespace Retinues.GUI.Editor.Modules.Common.Column.Controllers
{
    public class GenderController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Gender                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static ControllerAction<WCharacter> ToggleGender { get; } =
            Action<WCharacter>("ToggleGender")
                .AddCondition(
                    applies: _ =>
                        RaceHelper.HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: c => c?.Culture != null,
                    reason: L.T("gender_no_culture", "No culture is selected.")
                )
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
                    reason: L.T(
                        "gender_no_template",
                        "This culture has no valid body template for that gender/species."
                    )
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
                    reason: L.T(
                        "gender_not_renderable",
                        "That gender/species combination cannot be rendered."
                    )
                )
                .DefaultTooltip(L.T("gender_toggle_hint", "Change Gender"))
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
