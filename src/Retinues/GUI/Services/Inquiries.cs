using System;
using System.Collections.Generic;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.GUI.Services
{
    /// <summary>
    /// Helpers for popups and selection inquiries.
    /// All player-facing texts are TextObjects.
    /// </summary>
    [SafeClass]
    public static class Inquiries
    {
        private sealed class ListenerOwner { }

        private static readonly ListenerOwner Owner = new();

        private static bool _hooked;
        private static readonly List<Action> Pending = new(16);

        [StaticClearAction]
        public static void ClearPending() => Pending.Clear();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Popup                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Popup with title, description and an OK-style button.
        /// </summary>
        public static void Popup(
            TextObject title,
            TextObject description,
            TextObject buttonText = null,
            bool pauseGame = true,
            bool delayUntilOnWorldMap = false
        )
        {
            ShowOrDelay(
                delayUntilOnWorldMap,
                () =>
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
            );
        }

        /// <summary>
        /// Confirmation popup with default Confirm / Cancel.
        /// </summary>
        public static void Popup(
            TextObject title,
            Action onConfirm,
            TextObject description = null,
            TextObject confirmText = null,
            TextObject cancelText = null,
            bool pauseGame = true,
            bool delayUntilOnWorldMap = false
        )
        {
            ShowOrDelay(
                delayUntilOnWorldMap,
                () =>
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
                                    Log.Exception(
                                        e,
                                        "Notifications.Popup confirm callback failed."
                                    );
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
            );
        }

        /// <summary>
        /// Confirmation popup with two choices.
        /// </summary>
        public static void Popup(
            TextObject title,
            Action onChoice1,
            Action onChoice2,
            TextObject choice1Text,
            TextObject choice2Text,
            TextObject description = null,
            bool pauseGame = true,
            bool delayUntilOnWorldMap = false
        )
        {
            ShowOrDelay(
                delayUntilOnWorldMap,
                () =>
                {
                    try
                    {
                        var inquiry = new InquiryData(
                            title?.ToString() ?? string.Empty,
                            description?.ToString() ?? string.Empty,
                            isAffirmativeOptionShown: true,
                            isNegativeOptionShown: true,
                            affirmativeText: choice1Text.ToString(),
                            negativeText: choice2Text.ToString(),
                            affirmativeAction: () =>
                            {
                                try
                                {
                                    onChoice1?.Invoke();
                                }
                                catch (Exception e)
                                {
                                    Log.Exception(
                                        e,
                                        "Notifications.Popup choice1 callback failed."
                                    );
                                }
                            },
                            negativeAction: () =>
                            {
                                try
                                {
                                    onChoice2?.Invoke();
                                }
                                catch (Exception e)
                                {
                                    Log.Exception(
                                        e,
                                        "Notifications.Popup choice2 callback failed."
                                    );
                                }
                            }
                        );

                        InformationManager.ShowInquiry(inquiry, pauseGame);
                    }
                    catch (Exception e)
                    {
                        Log.Exception(e, "Notifications.Popup (choice) failed.");
                    }
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Single-Select Popup                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shows a single-select popup and invokes the selection callback.
        /// </summary>
        public static void SelectPopup(
            TextObject title,
            List<InquiryElement> elements,
            Action<InquiryElement> onSelect,
            TextObject description = null,
            TextObject confirmText = null,
            TextObject cancelText = null,
            bool pauseGame = true,
            bool delayUntilOnWorldMap = false
        )
        {
            if (elements == null || elements.Count == 0)
                return;

            confirmText ??= GameTexts.FindText("str_accept");
            cancelText ??= GameTexts.FindText("str_cancel");

            ShowOrDelay(
                delayUntilOnWorldMap,
                () =>
                {
                    try
                    {
                        MBInformationManager.ShowMultiSelectionInquiry(
                            new MultiSelectionInquiryData(
                                titleText: title?.ToString() ?? string.Empty,
                                descriptionText: description?.ToString() ?? string.Empty,
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
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Multi-Select Popup                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shows a multi-select popup and invokes the selection callback.
        /// </summary>
        public static void MultiSelectPopup(
            TextObject title,
            List<InquiryElement> elements,
            Action<List<InquiryElement>> onSelect,
            int minSelectable = 0,
            int maxSelectable = int.MaxValue,
            TextObject description = null,
            TextObject confirmText = null,
            TextObject cancelText = null,
            bool pauseGame = true,
            bool delayUntilOnWorldMap = false
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

            ShowOrDelay(
                delayUntilOnWorldMap,
                () =>
                {
                    try
                    {
                        MBInformationManager.ShowMultiSelectionInquiry(
                            new MultiSelectionInquiryData(
                                titleText: title?.ToString() ?? string.Empty,
                                descriptionText: description?.ToString() ?? string.Empty,
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
                                        onSelect?.Invoke(selected ?? []);
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
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Text Input Popup                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shows a text input popup and invokes the confirm callback with input.
        /// </summary>
        public static void TextInputPopup(
            TextObject title,
            string defaultInput,
            Action<string> onConfirm,
            TextObject description = null,
            TextObject confirmText = null,
            TextObject cancelText = null,
            bool pauseGame = true,
            bool delayUntilOnWorldMap = false
        )
        {
            ShowOrDelay(
                delayUntilOnWorldMap,
                () =>
                {
                    try
                    {
                        confirmText ??= GameTexts.FindText("str_accept");
                        cancelText ??= GameTexts.FindText("str_cancel");

                        var inquiry = new TextInquiryData(
                            title?.ToString() ?? string.Empty,
                            description?.ToString() ?? string.Empty,
                            isAffirmativeOptionShown: true,
                            isNegativeOptionShown: true,
                            affirmativeText: confirmText.ToString(),
                            negativeText: cancelText.ToString(),
                            affirmativeAction: input =>
                            {
                                try
                                {
                                    onConfirm?.Invoke(input);
                                }
                                catch (Exception e)
                                {
                                    Log.Exception(
                                        e,
                                        "Notifications.TextInputPopup confirm callback failed."
                                    );
                                }
                            },
                            negativeAction: () => { },
                            defaultInputText: defaultInput ?? string.Empty
                        );

                        InformationManager.ShowTextInquiry(inquiry, pauseGame);
                    }
                    catch (Exception e)
                    {
                        Log.Exception(e, "Notifications.TextInputPopup failed.");
                    }
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Delaying                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensure the tick listener is hooked to flush pending actions.
        /// </summary>
        private static void EnsureHooked()
        {
            if (_hooked)
                return;

            try
            {
                // Safe to call multiple times; we guard with _hooked.
                CampaignEvents.TickEvent.AddNonSerializedListener(Owner, OnTick);
                _hooked = true;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Inquiries.EnsureHooked failed.");
            }
        }

        /// <summary>
        /// Tick handler that processes one pending action when appropriate.
        /// </summary>
        private static void OnTick(float dt)
        {
            if (Pending.Count == 0)
                return;

            if (!IsOnWorldMap())
                return;

            // Flush one at a time to avoid inquiry stacking spam.
            var action = Pending[0];
            Pending.RemoveAt(0);

            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Log.Exception(e, "Inquiries delayed action failed.");
            }
        }

        /// <summary>
        /// Returns whether the current active game state is the world map.
        /// </summary>
        private static bool IsOnWorldMap()
        {
            var game = Game.Current;
            var gsm = game?.GameStateManager;
            if (gsm == null)
                return false;

            return gsm.ActiveState is MapState;
        }

        /// <summary>
        /// Shows the given action immediately or queues it for the world map.
        /// </summary>
        private static void ShowOrDelay(bool delayUntilOnWorldMap, Action show)
        {
            if (show == null)
                return;

            if (!delayUntilOnWorldMap)
            {
                show();
                return;
            }

            EnsureHooked();

            if (IsOnWorldMap())
            {
                show();
                return;
            }

            Pending.Add(show);

            if (Pending.Count > 64)
                Pending.RemoveAt(0);
        }
    }
}
