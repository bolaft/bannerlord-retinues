using Retinues.Engine;
using Retinues.Model.Factions;
using Retinues.Utilities;

namespace Retinues.Editor.Controllers
{
    public class CharacterController : BaseController
    {
        /// <summary>
        /// Change the name of the selected character.
        /// </summary>
        public static void ChangeName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                Inquiries.Popup(
                    L.T("invalid_name_title", "Invalid Name"),
                    L.T("invalid_name_body", "The name cannot be empty.")
                );
                return;
            }

            var character = State.Character;

            if (State.Character == null)
                return;

            if (newName == character.Name)
                return; // No change.

            character.Name = newName;
            EventManager.Fire(UIEvent.Name, EventScope.Local);
        }

        /// <summary>
        /// Change the culture of the selected character.
        /// </summary>
        public static void ChangeCulture(WCulture newCulture)
        {
            var character = State.Character;

            if (newCulture == character.Culture)
                return;

            // 1) Update culture.
            character.Culture = newCulture;

            // 2) Apply appearance from that culture.
            character.ApplyCultureBodyProperties();

            // 3) Notify the UI.
            EventManager.Fire(UIEvent.Culture, EventScope.Local);
        }

        /// <summary>
        /// Toggle the gender of the selected character.
        /// </summary>
        public static void ChangeGender()
        {
            var character = State.Character;

            // 1) Toggle gender
            character.IsFemale = !character.IsFemale;

            // 2) Apply appearance from that culture.
            character.ApplyCultureBodyProperties();

            // 3) Notify the UI.
            EventManager.Fire(UIEvent.Gender, EventScope.Local);
        }
    }
}
