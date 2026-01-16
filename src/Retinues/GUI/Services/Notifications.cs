using Retinues.Framework.Runtime;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.GUI.Services
{
    /// <summary>
    /// Helpers for popups and selection inquiries.
    /// All player-facing texts are TextObjects.
    /// </summary>
    [SafeClass]
    public static class Notifications
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Log                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Displays a simple message with optional color.
        /// </summary>
        public static void Message(string message, string color = "#e7cb8dff")
        {
            InformationManager.DisplayMessage(new InformationMessage(message, color));
        }

        /// <summary>
        /// Displays a localized message with optional color.
        /// </summary>
        public static void Message(TextObject message, string color = "#e7cb8dff")
        {
            Message(message?.ToString() ?? string.Empty, color);
        }
    }
}
