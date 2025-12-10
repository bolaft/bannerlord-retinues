using Retinues.Engine;
using Retinues.Utilities;

namespace Retinues.Editor.Controllers
{
    public abstract class CharacterController : BaseController
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

            // Update the character's name.
            State.Character.Name = newName;

            // Refresh this panel label immediately.
            EventManager.Fire(UIEvent.Name, EventScope.Local);
        }
    }
}
