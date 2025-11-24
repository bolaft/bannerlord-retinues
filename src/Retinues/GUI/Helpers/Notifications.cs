using System;
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

        /// <summary>
        /// Shows a popup inquiry with a title, description, and Confirm/Cancel buttons.
        /// </summary>
        public static void ConfirmationPopup(
            TextObject title,
            TextObject description,
            Action onConfirm,
            TextObject confirmText = null,
            TextObject cancelText = null,
            bool pauseGame = true
        )
        {
            confirmText ??= L.T("confirm", "Confirm");
            cancelText ??= L.T("cancel", "Cancel");

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    description.ToString(),
                    true,
                    true,
                    confirmText.ToString(),
                    cancelText.ToString(),
                    onConfirm,
                    null
                ),
                pauseGame
            );
        }

        /// <summary>
        /// Shows a quick information message at the top of the screen.
        /// </summary>
        public static void Information(TextObject message, WCharacter announcer)
        {
            MBInformationManager.AddQuickInformation(
                message,
                extraTimeInMs: 5000,
                announcerCharacter: announcer.Base
            );
        }

        /// <summary>
        /// Logs a message to the in-game message log with optional color.
        /// </summary>
        public static void Log(string message, string color = "#ffffffe0")
        {
            InformationManager.DisplayMessage(
                new InformationMessage(message, Color.ConvertStringToColor(color))
            );
        }

        /// <summary>
        /// Logs a message to the in-game message log with optional color.
        /// </summary>
        public static void Log(TextObject message, string color = "#ffffffe0")
        {
            Log(message.ToString(), color);
        }
    }
}
