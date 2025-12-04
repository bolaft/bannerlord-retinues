using System;
using System.Collections.Generic;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Engine
{
    /// <summary>
    /// Helpers for popups and selection inquiries.
    /// All player-facing texts are TextObjects.
    /// </summary>
    public static class Notifications
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Popup                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Popup with title, description and an OK-style button.
        /// Example:
        ///   Notifications.Popup(
        ///       L.T("op_complete", "Operation completed successfully."),
        ///       L.T("op_details", "Your changes have been saved.")
        ///   );
        /// </summary>
        public static void Popup(
            TextObject title,
            TextObject description,
            TextObject buttonText = null,
            bool pauseGame = true
        )
        {
            try
            {
                buttonText ??= GameTexts.FindText("str_ok");

                var inquiry = new InquiryData(
                    title?.ToString() ?? string.Empty,
                    description?.ToString() ?? string.Empty,
                    isAffirmativeOptionShown: false,
                    isNegativeOptionShown: true, // "OK" uses negative slot in vanilla
                    affirmativeText: null,
                    negativeText: buttonText.ToString(),
                    affirmativeAction: null,
                    negativeAction: null
                );

                InformationManager.ShowInquiry(inquiry, pauseGame);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Notifications.Popup failed.");
            }
        }

        /// <summary>
        /// Confirmation popup with default Confirm / Cancel.
        /// Example:
        ///   Notifications.Popup(
        ///       L.T("confirm_title", "Are you sure?"),
        ///       onConfirm: DoTheThing
        ///   );
        /// </summary>
        public static void Popup(
            TextObject title,
            Action onConfirm,
            TextObject description = null,
            TextObject confirmText = null,
            TextObject cancelText = null,
            bool pauseGame = true
        )
        {
            try
            {
                confirmText ??= GameTexts.FindText("str_accept");
                cancelText ??= GameTexts.FindText("str_cancel");

                var inquiry = new InquiryData(
                    title?.ToString() ?? string.Empty,
                    description?.ToString() ?? string.Empty,
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: confirmText.ToString(),
                    negativeText: cancelText.ToString(),
                    affirmativeAction: () =>
                    {
                        try
                        {
                            onConfirm?.Invoke();
                        }
                        catch (Exception e)
                        {
                            Log.Exception(e, "Notifications.Popup confirm callback failed.");
                        }
                    },
                    negativeAction: null
                );

                InformationManager.ShowInquiry(inquiry, pauseGame);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Notifications.Popup (confirm) failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Single-select popup                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Single-selection popup (wraps MultiSelectionInquiryData with max=1).
        /// Example:
        ///   Notifications.SelectPopup(
        ///       L.T("choose_option", "Choose an option:"),
        ///       elements,
        ///       elem => { ... }
        ///   );
        /// </summary>
        public static void SelectPopup(
            TextObject title,
            List<InquiryElement> elements,
            Action<InquiryElement> onSelect,
            TextObject confirmText = null,
            TextObject cancelText = null,
            bool pauseGame = true
        )
        {
            if (elements == null || elements.Count == 0)
                return;

            confirmText ??= GameTexts.FindText("str_accept");
            cancelText ??= GameTexts.FindText("str_cancel");

            try
            {
                MBInformationManager.ShowMultiSelectionInquiry(
                    new MultiSelectionInquiryData(
                        titleText: title?.ToString() ?? string.Empty,
                        descriptionText: string.Empty,
                        inquiryElements: elements,
                        isExitShown: true,
                        minSelectableOptionCount: 1,
                        maxSelectableOptionCount: 1,
                        affirmativeText: confirmText.ToString(),
                        negativeText: cancelText.ToString(),
                        affirmativeAction: selected =>
                        {
                            try
                            {
                                if (selected == null || selected.Count == 0)
                                    return;

                                onSelect?.Invoke(selected[0]);
                            }
                            catch (Exception e)
                            {
                                Log.Exception(
                                    e,
                                    "Notifications.SelectPopup affirmative callback failed."
                                );
                            }
                        },
                        negativeAction: _ => { }
                    ),
                    pauseGame
                );
            }
            catch (Exception e)
            {
                Log.Exception(e, "Notifications.SelectPopup failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Multi-selection popup                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Multi-selection popup.
        /// Example:
        ///   Notifications.MultiSelectPopup(
        ///       L.T("choose_options", "Choose an option:"),
        ///       elements,
        ///       onSelect: selected => { ... },
        ///       minSelectable: 0,
        ///       maxSelectable: 3
        ///   );
        /// </summary>
        public static void MultiSelectPopup(
            TextObject title,
            List<InquiryElement> elements,
            Action<List<InquiryElement>> onSelect,
            int minSelectable = 0,
            int maxSelectable = int.MaxValue,
            TextObject confirmText = null,
            TextObject cancelText = null,
            bool pauseGame = true
        )
        {
            if (elements == null || elements.Count == 0)
                return;

            if (minSelectable < 0)
                minSelectable = 0;

            if (maxSelectable < 1)
                maxSelectable = 1;

            if (maxSelectable < minSelectable)
                maxSelectable = minSelectable;

            confirmText ??= GameTexts.FindText("str_done");
            cancelText ??= GameTexts.FindText("str_cancel");

            try
            {
                MBInformationManager.ShowMultiSelectionInquiry(
                    new MultiSelectionInquiryData(
                        titleText: title?.ToString() ?? string.Empty,
                        descriptionText: string.Empty,
                        inquiryElements: elements,
                        isExitShown: true,
                        minSelectableOptionCount: minSelectable,
                        maxSelectableOptionCount: maxSelectable,
                        affirmativeText: confirmText.ToString(),
                        negativeText: cancelText.ToString(),
                        affirmativeAction: selected =>
                        {
                            try
                            {
                                onSelect?.Invoke(selected ?? new List<InquiryElement>());
                            }
                            catch (Exception e)
                            {
                                Log.Exception(
                                    e,
                                    "Notifications.MultiSelectPopup affirmative callback failed."
                                );
                            }
                        },
                        negativeAction: _ => { }
                    ),
                    pauseGame
                );
            }
            catch (Exception e)
            {
                Log.Exception(e, "Notifications.MultiSelectPopup failed.");
            }
        }
    }
}
