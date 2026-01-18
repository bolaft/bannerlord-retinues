using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Character.Controllers
{
    /// <summary>
    /// Lets the player force a troop formation class, or revert back to auto.
    /// </summary>
    public class FormationClassController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Action                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Opens the formation class picker for the current character.
        /// </summary>
        public static ControllerAction<bool> ChangeFormationClass { get; } =
            Action<bool>("ChangeFormationClass")
                .DefaultTooltip(
                    L.T("button_change_formation_class_tooltip", "Select a formation class.")
                )
                .ExecuteWith(_ => ShowPicker(State.Character));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Popup                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Show the formation class picker for the given character.
        /// </summary>
        private static void ShowPicker(WCharacter character)
        {
            if (character == null)
                return;

            var options = BuildOptions(character);

            Inquiries.SelectPopup(
                L.T("formation_popup_title", "Formation Class"),
                options,
                selected => ApplySelection(character, [selected]),
                description: L.T(
                    "formation_popup_desc",
                    "Choose how this unit is categorized. Leave on 'Auto' to categorize it based on its equipment."
                ),
                pauseGame: true
            );
        }

        /// <summary>
        /// Build the list of formation class options for the picker.
        /// </summary>
        private static List<InquiryElement> BuildOptions(WCharacter character)
        {
            var cur = character.FormationClassOverride;

            // Using FormationClass.Unset as the "Auto" identifier.
            // WCharacter.FormationClassOverride already treats Unset as "no override".
            var list = new List<InquiryElement>
            {
                Make(
                    FormationClass.Unset,
                    title: L.S("formation_option_auto", "Auto"),
                    L.S(
                        "formation_option_auto_hint",
                        "Let the game automatically choose the formation class based on equipment."
                    )
                ),
                Make(FormationClass.Infantry),
                Make(FormationClass.Cavalry),
                Make(FormationClass.Ranged),
                Make(FormationClass.HorseArcher),
            };

            return list;
        }

        /// <summary>
        /// Create an InquiryElement entry for a formation class.
        /// </summary>
        private static InquiryElement Make(
            FormationClass id,
            string title = null,
            string hint = null
        )
        {
            // Fallback to localized name if no custom title provided.
            title ??= id.GetLocalizedName().ToString();

            // Fallback to default hint if no custom hint provided.
            hint ??= L.T("formation_option_hint", "Force the {CLASS} formation.")
                .SetTextVariable("CLASS", title.ToLower())
                .ToString();

            // Pass null imageIdentifier to avoid BL12/BL13 ImageIdentifier namespace differences.
            return new InquiryElement(
                identifier: id,
                title: title,
                imageIdentifier: null,
                isEnabled: true,
                hint: hint
            );
        }

        /// <summary>
        /// Apply the selected formation class to the character (Unset = Auto).
        /// </summary>
        private static void ApplySelection(WCharacter character, List<InquiryElement> selected)
        {
            if (character == null)
                return;

            var first = selected?.FirstOrDefault();

            if (first?.Identifier is FormationClass fc)
                character.FormationClassOverride = fc; // Unset means Auto
            else
                character.FormationClassOverride = FormationClass.Unset;

            EventManager.Fire(UIEvent.Formation);
        }
    }
}
