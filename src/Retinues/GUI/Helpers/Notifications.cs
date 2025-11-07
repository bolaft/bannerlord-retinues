using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.GUI.Helpers
{
    /// <summary>
    /// Static helpers for showing popup inquiries.
    /// </summary>
    [SafeClass]
    public static class Notifications
    {
        /// <summary>
        /// Shows a popup inquiry with a title, description, and an OK button.
        /// </summary>
        public static void Popup(
            TextObject title,
            TextObject description,
            TextObject buttonText = null,
            bool pauseGame = true
        )
        {
            buttonText ??= GameTexts.FindText("str_ok");

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    description.ToString(),
                    false,
                    true,
                    null,
                    buttonText.ToString(),
                    null,
                    null
                ),
                pauseGame
            );
        }

        public static void Information(
            TextObject message,
            WCharacter announcer
        )
        {
            MBInformationManager.AddQuickInformation(message, extraTimeInMs: 5000, announcerCharacter: announcer.Base);
        }
    }
}
