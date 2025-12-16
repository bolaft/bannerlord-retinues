using System.Linq;
using Retinues.Helpers;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers
{
    public class CharacterController : BaseController
    {
        /// <summary>
        /// Change the name of the selected character.
        /// </summary>
        public static void ChangeName()
        {
            static void Apply(string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    Inquiries.Popup(
                        L.T("invalid_name_title", "Invalid Name"),
                        L.T("invalid_name_body", "The name cannot be empty.")
                    );
                    return;
                }

                var character = State.Character.Editable;

                if (newName == character.Name)
                    return; // No change.

                character.Name = newName;
                EventManager.Fire(UIEvent.Name, EventScope.Local);
            }

            Inquiries.TextInputPopup(
                title: L.T("rename_unit", "New Name"),
                defaultInput: State.Character.Editable.Name,
                onConfirm: input => Apply(input.Trim()),
                description: L.T("enter_new_name", "Enter a new name:")
            );
        }

        /// <summary>
        /// Change the culture of the selected character.
        /// </summary>
        public static void ChangeCulture(WCulture newCulture)
        {
            var character = State.Character.Editable;

            if (newCulture == character.Culture)
                return;

            // 1) Update culture.
            character.Culture = newCulture;

            // 2) Apply appearance from that culture.
            if (character is WCharacter wc)
                wc.ApplyCultureBodyProperties();

            // 3) Notify the UI.
            EventManager.Fire(UIEvent.Culture, EventScope.Local);
        }

        /// <summary>
        /// Toggle the gender of the selected character.
        /// </summary>
        public static void ChangeGender()
        {
            var character = State.Character.Editable;

            // 1) Toggle gender
            character.IsFemale = !character.IsFemale;

            // 2) Apply appearance from that culture.
            if (character is WCharacter wc)
                wc.ApplyCultureBodyProperties();

            // 3) Notify the UI.
            EventManager.Fire(UIEvent.Gender, EventScope.Local);
        }

        /// <summary>
        /// Determines whether the current character can be removed.
        /// </summary>
        public static bool CanRemoveCharacter(out TextObject reason) =>
            Check(
                [
                    (
                        () => State.Character.IsHero == false,
                        L.T("character_cannot_remove_hero", "Heroes cannot be removed.")
                    ),
                    (
                        () => State.Character.UpgradeTargets.Count == 0,
                        L.T(
                            "character_cannot_remove_with_upgrades",
                            "Can't remove a unit that still has upgrades."
                        )
                    ),
                    (
                        () => !State.Character.IsRoot,
                        L.T("character_cannot_remove_root", "Root units cannot be removed.")
                    ),
                ],
                out reason
            );

        public static void RemoveCharacter()
        {
            if (!CanRemoveCharacter(out var reason))
            {
                Inquiries.Popup(L.T("cannot_remove_character_title", "Cannot Remove Unit"), reason);
                return;
            }

            Inquiries.Popup(
                title: L.T("inquiry_confirm_remove_character_title", "Delete Unit"),
                description: L.T(
                        "inquiry_confirm_remove_character_text",
                        "Are you sure you want to delete {UNIT_NAME}? This action cannot be undone."
                    )
                    .SetTextVariable("UNIT_NAME", State.Character.Name.ToString()),
                onConfirm: () =>
                {
                    var character = State.Character;

                    // 1) Select another character first.
                    State.Character = State.Faction.Troops.FirstOrDefault(c => c != character);

                    // 2) Remove from faction.
                    character.Remove();

                    // 3) Notify the UI.
                    EventManager.Fire(UIEvent.Tree, EventScope.Global);
                }
            );
        }
    }
}
