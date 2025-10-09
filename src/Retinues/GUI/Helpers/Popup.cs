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
    public static class Popup
    {
        /// <summary>
        /// Shows a popup inquiry with a title, description, and an OK button.
        /// </summary>
        public static void Display(
            TextObject title,
            TextObject description,
            TextObject buttonText = null
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
                )
            );
        }
    }
}
