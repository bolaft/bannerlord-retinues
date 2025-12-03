using System;
using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.GUI.Helpers
{
    /// <summary>
    /// Represents a single choice in a wizard popup (label, subtitle, callback, optional image).
    /// </summary>
    [SafeClass]
    public sealed class WizardPopupOption
    {
        /// <summary>
        /// Optional id for the option (handy for logging / debugging).
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Main label shown in the popup.
        /// </summary>
        public TextObject Title { get; }

        /// <summary>
        /// Subtitle / description shown under the main label.
        /// </summary>
        public TextObject Subtitle { get; }

        /// <summary>
        /// Optional image for this option (portrait, icon, etc.).
        /// If null, the popup's defaultImage (if any) will be used.
        /// </summary>
        public ImageIdentifier Image { get; }

        /// <summary>
        /// Whether the option can be selected.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Optional hint text explaining why the option is disabled.
        /// </summary>
        public TextObject DisabledHint { get; }

        /// <summary>
        /// Callback invoked when this option is chosen.
        /// </summary>
        public Action OnSelected { get; }

        public WizardPopupOption(
            string id,
            TextObject title,
            TextObject subtitle,
            Action onSelected,
            ImageIdentifier image = null,
            bool isEnabled = true,
            TextObject disabledHint = null)
        {
            Id = id;
            Title = title ?? TextObject.Empty;
            Subtitle = subtitle ?? TextObject.Empty;
            Image = image;
            IsEnabled = isEnabled;
            DisabledHint = disabledHint ?? TextObject.Empty;
            OnSelected = onSelected;
        }
    }

    /// <summary>
    /// Static helper to show a multi-choice popup using MultiSelectionInquiryData.
    /// Designed as the UI building block for the Retinues setup wizard.
    /// </summary>
    [SafeClass]
    public static class WizardPopup
    {
        /// <summary>
        /// Show a popup with multiple choices (2–N), subtitles and optional images.
        /// </summary>
        /// <param name="title">Popup title text.</param>
        /// <param name="description">Body text under the title.</param>
        /// <param name="options">List of choices; must contain at least one enabled option.</param>
        /// <param name="confirmText">Label for the validate button (defaults to "Done").</param>
        /// <param name="cancelText">Label for the cancel button (defaults to "Cancel").</param>
        /// <param name="onCancel">Callback when the popup is cancelled.</param>
        /// <param name="showCancel">Whether to show the cancel button.</param>
        /// <param name="pauseGame">Whether to pause the game while the popup is open.</param>
        /// <param name="defaultImage">
        /// Optional image used for options whose Image is null. If null, options
        /// without an image will simply have no icon.
        /// </param>
        public static void Show(
            TextObject title,
            TextObject description,
            IReadOnlyList<WizardPopupOption> options,
            TextObject confirmText = null,
            TextObject cancelText = null,
            Action onCancel = null,
            bool showCancel = true,
            bool pauseGame = true,
            ImageIdentifier defaultImage = null)
        {
            try
            {
                if (options == null || options.Count == 0)
                {
                    Log.Warn("WizardPopup.Show called with no options.");
                    return;
                }

                // Build InquiryElements from our options
                var elements = new List<InquiryElement>(options.Count);
                foreach (var opt in options)
                {
                    if (opt == null)
                        continue;

                    var titleStr = opt.Title?.ToString() ?? string.Empty;

                    // Subtitle is the main "hint" line under the title.
                    // If the option is disabled and has a DisabledHint, append that.
                    string hintStr;
                    if (!opt.IsEnabled && opt.DisabledHint != null)
                    {
                        var subtitleStr = opt.Subtitle?.ToString() ?? string.Empty;
                        var disabledStr = opt.DisabledHint.ToString();
                        hintStr = string.IsNullOrEmpty(subtitleStr)
                            ? disabledStr
                            : subtitleStr + "\n\n" + disabledStr;
                    }
                    else
                    {
                        hintStr = opt.Subtitle?.ToString() ?? string.Empty;
                    }

                    var image = opt.Image ?? defaultImage;

                    elements.Add(
                        new InquiryElement(
                            identifier: opt,
                            title: titleStr,
                            imageIdentifier: image,
                            isEnabled: opt.IsEnabled,
                            hint: hintStr
                        )
                    );
                }

                if (elements.Count == 0)
                {
                    Log.Warn("WizardPopup.Show built no InquiryElements (all options were null?).");
                    return;
                }

                confirmText ??= GameTexts.FindText("str_done");
                cancelText ??= GameTexts.FindText("str_cancel");

                void OnAffirmative(List<InquiryElement> selected)
                {
                    try
                    {
                        if (selected == null || selected.Count == 0)
                            return;

                        if (selected[0].Identifier is not WizardPopupOption chosen)
                            return;

                        chosen.OnSelected?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }
                }

                void OnNegative(List<InquiryElement> _)
                {
                    try
                    {
                        onCancel?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }
                }

                MBInformationManager.ShowMultiSelectionInquiry(
                    new MultiSelectionInquiryData(
                        titleText: title?.ToString() ?? string.Empty,
                        descriptionText: description?.ToString(),
                        inquiryElements: elements,
                        isExitShown: showCancel,
                        minSelectableOptionCount: 1,
                        maxSelectableOptionCount: 1,
                        affirmativeText: confirmText.ToString(),
                        negativeText: cancelText.ToString(),
                        affirmativeAction: OnAffirmative,
                        negativeAction: OnNegative
                    ),
                    pauseGame
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
