using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Engine
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

        public static void Message(string message, string color = "#ffffffff")
        {
            InformationManager.DisplayMessage(new InformationMessage(message, color));
        }

        public static void Message(TextObject message, string color = "#ffffffff")
        {
            Message(message?.ToString() ?? string.Empty, color);
        }
    }
}
