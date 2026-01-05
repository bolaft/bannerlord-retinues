using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Character
{
    /// <summary>
    /// Lets the player force a troop formation class, or revert back to auto.
    /// </summary>
    public class FormationClassController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Action                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WCharacter> ChangeFormationClass { get; } =
            Action<WCharacter>("ChangeFormationClass")
                .DefaultTooltip(
                    L.T("button_change_formation_class_tooltip", "Select a formation class.")
                )
                .AddCondition(
                    _ => State.Character != null,
                    L.T("formation_no_character_reason", "No character selected.")
                )
                .ExecuteWith(_ => ShowPicker(State.Character));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Popup                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        private static List<InquiryElement> BuildOptions(WCharacter character)
        {
            var cur = character.FormationClassOverride;

            // Using FormationClass.Unset as the "Auto" identifier.
            // WCharacter.FormationClassOverride already treats Unset as "no override".
            var list = new List<InquiryElement>
            {
                Make(FormationClass.Unset, title: L.S("formation_option_auto", "Auto")),
                Make(FormationClass.Infantry),
                Make(FormationClass.Cavalry),
                Make(FormationClass.Ranged),
                Make(FormationClass.HorseArcher),
            };

            return list;
        }

        private static InquiryElement Make(FormationClass id, string title = null)
        {
            // Fallback to localized name if no custom title provided.
            title ??= id.GetLocalizedName().ToString();

            // Pass null imageIdentifier to avoid BL12/BL13 ImageIdentifier namespace differences.
            return new InquiryElement(
                identifier: id,
                title: title,
                imageIdentifier: null,
                isEnabled: true,
                hint: null
            );
        }

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
