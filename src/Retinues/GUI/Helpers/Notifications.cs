using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
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

        public static void Information(TextObject message, WCharacter announcer)
        {
            MBInformationManager.AddQuickInformation(
                message,
                extraTimeInMs: 5000,
                announcerCharacter: announcer.Base
            );
        }

        public static void Log(string message, string color = "#ffffffe0")
        {
            InformationManager.DisplayMessage(
                new InformationMessage(message, Color.ConvertStringToColor(color))
            );
        }

        public static void MapNotification(TextObject message)
        {
            var data = new MercenaryOfferMapNotification(null, message);
            MapNotification(data);
        }

        public static void MapNotification(InformationData data)
        {
            Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(data);
        }
    }
}
