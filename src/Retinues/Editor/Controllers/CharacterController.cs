using Retinues.Engine;
using Retinues.Model.Factions;
using Retinues.Utilities;

namespace Retinues.Editor.Controllers
{
    public class CharacterController : BaseController
    {
        public static void ChangeName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                Notifications.Popup(
                    L.T("invalid_name_title", "Invalid Name"),
                    L.T("invalid_name_body", "The name cannot be empty.")
                );
                return;
            }

            if (newName == State.Character.Name)
                return; // No change.

            // Update the character's name.
            State.Character.Name = newName;

            // Refresh this panel label immediately.
            EventManager.Fire(UIEvent.Name, EventScope.Local);
        }

        public static void ChangeCulture(WCulture newCulture)
        {
            if (newCulture == null)
                return;

            if (newCulture == State.Character.Culture)
                return; // No change.

            // Update the character's culture.
            State.Character.Culture = newCulture;

            // Fire UI events so panels bound to culture refresh.
            EventManager.Fire(UIEvent.Culture, EventScope.Local);
        }
    }
}
