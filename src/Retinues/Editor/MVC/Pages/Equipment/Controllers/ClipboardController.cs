using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Equipment.Services;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Pages.Equipment.Controllers
{
    /// <summary>
    /// Non-view logic for equipment set navigation and mutation.
    /// </summary>
    public class ClipboardController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Equipment Clipboard                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class EquipmentClipboard
        {
            public string Code;
            public bool IsCivilian;
            public string SourceUnitName;
        }

        private static EquipmentClipboard _clipboard;

        public static bool HasClipboard =>
            _clipboard != null && !string.IsNullOrEmpty(_clipboard.Code);

        /// <summary>
        /// Copy the current equipment set to the clipboard.
        /// </summary>
        public static ControllerAction<bool> CopyEquipment { get; } =
            Action<bool>("CopyEquipment")
                .RequireValidEditingContext()
                .DefaultTooltip(L.T("equipment_copy_tooltip", "Copy to clipboard"))
                .ExecuteWith(_ => CopyEquipmentImpl());

        /// <summary>
        /// Paste equipment from the clipboard to the selected set.
        /// </summary>
        public static ControllerAction<bool> PasteEquipment { get; } =
            Action<bool>("PasteEquipment")
                .RequireValidEditingContext()
                .AddCondition(
                    _ => HasClipboard,
                    L.T("equipment_paste_empty_clipboard_reason", "Clipboard is empty")
                )
                .DefaultTooltip(L.T("equipment_paste_tooltip", "Paste from clipboard"))
                .ExecuteWith(_ => PasteEquipmentImpl());

        /// <summary>
        /// Build a serializable equipment code for the given MEquipment, respecting preview mode.
        /// </summary>
        private static string BuildEquipmentCode(MEquipment source)
        {
            if (source == null)
                return null;

            if (!PreviewController.Enabled)
                return source.Code;

#if BL13
            var e = new TaleWorlds.Core.Equipment(TaleWorlds.Core.Equipment.EquipmentType.Battle);
#else
            var e = new TaleWorlds.Core.Equipment(false);
#endif

            int slotCount = (int)EquipmentIndex.NumEquipmentSetSlots;
            for (int i = 0; i < slotCount; i++)
            {
                var idx = (EquipmentIndex)i;
                var item = PreviewController.GetItem(idx);
                e[idx] = item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);
            }

            return e.CalculateEquipmentCode();
        }

        /// <summary>
        /// Decode equipment changes from an equipment code string into slot/item pairs.
        /// </summary>
        private static IEnumerable<(EquipmentIndex Slot, WItem Item)> DecodeEquipmentChanges(
            string code
        )
        {
            if (string.IsNullOrEmpty(code))
                yield break;

            var src = TaleWorlds.Core.Equipment.CreateFromEquipmentCode(code);
            if (src == null)
                yield break;

            int slotCount = (int)EquipmentIndex.NumEquipmentSetSlots;

            for (int i = 0; i < slotCount; i++)
            {
                var idx = (EquipmentIndex)i;
                var baseItem = src[idx].Item;
                var it = baseItem == null ? null : WItem.Get(baseItem);

                yield return (idx, it);

                if (idx == EquipmentIndex.Horse && it == null)
                    yield return (EquipmentIndex.HorseHarness, null);
            }
        }

        /// <summary>
        /// Populate before/after item arrays from the given plan and current getter.
        /// </summary>
        private static void FillArraysFromPlan(
            Func<EquipmentIndex, WItem> getCurrent,
            EquipPlan plan,
            out WItem[] before,
            out WItem[] after
        )
        {
            int slotCount = (int)EquipmentIndex.NumEquipmentSetSlots;

            before = new WItem[slotCount];
            after = new WItem[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                var idx = (EquipmentIndex)i;
                var it = getCurrent(idx);
                before[i] = it;
                after[i] = it;
            }

            if (plan == null)
                return;

            foreach (var kv in plan.Changes)
            {
                int ii = (int)kv.Key;
                if (ii < 0 || ii >= slotCount)
                    continue;

                after[ii] = kv.Value;
            }
        }

        /// <summary>
        /// Copy implementation that stores the selected equipment set into the clipboard.
        /// </summary>
        private static void CopyEquipmentImpl()
        {
            var e = State.Equipment;
            if (e == null)
                return;

            _clipboard = new EquipmentClipboard
            {
                Code = BuildEquipmentCode(e),
                IsCivilian = e.IsCivilian,
                SourceUnitName = State.Character?.Name?.ToString(),
            };

            Notifications.Message(L.S("equipment_copied_toast", "Copied equipment to clipboard."));

            EventManager.Fire(UIEvent.Clipboard);
        }

        /// <summary>
        /// Apply an equipment plan to either preview or live equipment.
        /// </summary>
        private static void ApplyPlan(EquipPlan plan)
        {
            if (plan == null)
                return;

            if (PreviewController.Enabled)
            {
                foreach (var kv in plan.Changes)
                    PreviewController.SetItem(kv.Key, kv.Value);

                return;
            }

            foreach (var kv in plan.Changes)
                State.Equipment.Set(kv.Key, kv.Value);
        }

        /// <summary>
        /// Find an item wrapper by its string id.
        /// </summary>
        private static WItem FindItemById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (var it in WItem.All)
            {
                if (it != null && it.StringId == id)
                    return it;
            }

            return null;
        }

        /// <summary>
        /// Join a list of names into a natural English list (commas + 'and').
        /// </summary>
        private static string JoinNatural(IReadOnlyList<string> names)
        {
            if (names == null || names.Count == 0)
                return string.Empty;

            if (names.Count == 1)
                return names[0];

            if (names.Count == 2)
                return names[0] + " and " + names[1];

            return string.Join(", ", names.Take(names.Count - 1))
                + " and "
                + names[names.Count - 1];
        }

        /// <summary>
        /// Join a map of item-id -> count into a natural string with quantities.
        /// </summary>
        private static string JoinNaturalFromMap(Dictionary<string, int> map)
        {
            if (map == null || map.Count == 0)
                return string.Empty;

            var list = new List<string>();

            foreach (var kv in map)
            {
                var item = FindItemById(kv.Key);
                var name = item?.Name?.ToString();

                if (string.IsNullOrEmpty(name))
                    continue;

                if (kv.Value > 1)
                    list.Add($"{name} x{kv.Value}");
                else
                    list.Add(name);
            }

            return JoinNatural(list);
        }

        /// <summary>
        /// Build the confirmation message block describing what will/can't be copied and costs.
        /// </summary>
        private static string BuildSentenceBlock(
            EquipPlan plan,
            bool economyEnabled,
            string sourceName,
            string targetName
        )
        {
            if (plan == null)
            {
                return L.T(
                        "equipment_paste_confirm_question",
                        "Copy equipment from {SRC} to {DST}?"
                    )
                    .SetTextVariable("SRC", sourceName)
                    .SetTextVariable("DST", targetName)
                    .ToString();
            }

            static bool IsPlural(IReadOnlyList<string> names) => names != null && names.Count != 1;

            static TextObject TPlural(
                bool plural,
                string idSingular,
                string fallbackSingular,
                string idPlural,
                string fallbackPlural
            )
            {
                return plural ? L.T(idPlural, fallbackPlural) : L.T(idSingular, fallbackSingular);
            }

            var lines = new List<string>
            {
                // First line: the question (always first).
                L.T("equipment_paste_confirm_question", "Copy equipment from {SRC} to {DST}?")
                    .SetTextVariable("SRC", sourceName)
                    .SetTextVariable("DST", targetName)
                    .ToString(),
            };

            // Cost line (only if buying is required).
            if (economyEnabled && plan.TotalCost > 0 && plan.PurchaseById.Count > 0)
            {
                var buyList = JoinNaturalFromMap(plan.PurchaseById);

                if (!string.IsNullOrEmpty(buyList))
                {
                    // We decide singular/plural based on number of distinct purchased item names in the sentence.
                    // (Even if English is identical, we still use distinct ids.)
                    var buyNames = new List<string>();
                    foreach (var kv in plan.PurchaseById)
                    {
                        var item = FindItemById(kv.Key);
                        var name = item?.Name?.ToString();
                        if (!string.IsNullOrEmpty(name))
                            buyNames.Add(name);
                    }

                    bool plural = buyNames.Count != 1;

                    lines.Add(
                        TPlural(
                                plural,
                                idSingular: "equipment_paste_buy_singular",
                                fallbackSingular: "Copying this equipment will require buying {ITEMS} for {COST} denars.",
                                idPlural: "equipment_paste_buy_plural",
                                fallbackPlural: "Copying this equipment will require buying {ITEMS} for {COST} denars."
                            )
                            .SetTextVariable("ITEMS", buyList)
                            .SetTextVariable("COST", plan.TotalCost)
                            .ToString()
                    );
                }
            }

            // "Only note things that won't be copied" -> sentences only for skipped categories that have names.
            void AddSkipLine(
                EquipSkipReason reason,
                string idSingular,
                string fallbackSingular,
                string idPlural,
                string fallbackPlural
            )
            {
                var names = plan.SkippedNamesOf(reason);
                if (names == null || names.Count == 0)
                    return;

                var joined = JoinNatural(names);
                if (string.IsNullOrEmpty(joined))
                    return;

                bool plural = IsPlural(names);

                lines.Add(
                    TPlural(plural, idSingular, fallbackSingular, idPlural, fallbackPlural)
                        .SetTextVariable("ITEMS", joined)
                        .ToString()
                );
            }

            AddSkipLine(
                EquipSkipReason.Locked,
                idSingular: "equipment_paste_locked_singular",
                fallbackSingular: "{ITEMS} is not unlocked and will not be equipped.",
                idPlural: "equipment_paste_locked_plural",
                fallbackPlural: "{ITEMS} are not unlocked and will not be equipped."
            );

            AddSkipLine(
                EquipSkipReason.Skill,
                idSingular: "equipment_paste_skill_singular",
                fallbackSingular: "{ITEMS} does not meet skill requirements and will not be equipped.",
                idPlural: "equipment_paste_skill_plural",
                fallbackPlural: "{ITEMS} do not meet skill requirements and will not be equipped."
            );

            AddSkipLine(
                EquipSkipReason.Tier,
                idSingular: "equipment_paste_tier_singular",
                fallbackSingular: "{ITEMS} is above the unit tier and will not be equipped.",
                idPlural: "equipment_paste_tier_plural",
                fallbackPlural: "{ITEMS} are above the unit tier and will not be equipped."
            );

            AddSkipLine(
                EquipSkipReason.Limits,
                idSingular: "equipment_paste_limits_singular",
                fallbackSingular: "{ITEMS} exceeds equipment limits and will not be equipped.",
                idPlural: "equipment_paste_limits_plural",
                fallbackPlural: "{ITEMS} exceed equipment limits and will not be equipped."
            );

            AddSkipLine(
                EquipSkipReason.CivilianMismatch,
                idSingular: "equipment_paste_civilian_singular",
                fallbackSingular: "{ITEMS} is not a civilian item and will not be equipped.",
                idPlural: "equipment_paste_civilian_plural",
                fallbackPlural: "{ITEMS} are not civilian items and will not be equipped."
            );

            AddSkipLine(
                EquipSkipReason.Incompatible,
                idSingular: "equipment_paste_incompatible_singular",
                fallbackSingular: "{ITEMS} is incompatible and will not be equipped.",
                idPlural: "equipment_paste_incompatible_plural",
                fallbackPlural: "{ITEMS} are incompatible and will not be equipped."
            );

            AddSkipLine(
                EquipSkipReason.Other,
                idSingular: "equipment_paste_other_singular",
                fallbackSingular: "{ITEMS} cannot be equipped and will be skipped.",
                idPlural: "equipment_paste_other_plural",
                fallbackPlural: "{ITEMS} cannot be equipped and will be skipped."
            );

            // If we only have the question, return only it (no trailing blank lines).
            if (lines.Count == 1)
                return lines[0];

            return string.Join("\n\n", lines);
        }

        /// <summary>
        /// Show a popup indicating insufficient funds for the paste operation.
        /// </summary>
        private static void NotEnoughGoldPopup(string sourceName, string targetName, int required)
        {
            Inquiries.Popup(
                title: L.T("cant_afford_title", "Not Enough Money"),
                description: L.T(
                        "equipment_paste_cant_afford_desc",
                        "Copying equipment from {SRC} to {DST} requires {COST} denars, but you only have {GOLD}."
                    )
                    .SetTextVariable("SRC", sourceName)
                    .SetTextVariable("DST", targetName)
                    .SetTextVariable("COST", required)
                    .SetTextVariable("GOLD", Player.Gold)
            );
        }

        /// <summary>
        /// Show a popup indicating nothing can be applied from the clipboard.
        /// </summary>
        private static void NothingToApplyPopup(string sourceName, string targetName)
        {
            Inquiries.Popup(
                title: L.T("equipment_paste_nothing_title", "Copy Equipment"),
                description: L.T(
                        "equipment_paste_nothing_desc2",
                        "Nothing new from {SRC} can be equipped on {DST}."
                    )
                    .SetTextVariable("SRC", sourceName)
                    .SetTextVariable("DST", targetName)
            );
        }

        /// <summary>
        /// Execute the paste flow: compute plan, confirm costs, and apply equipment changes.
        /// </summary>
        private static void PasteEquipmentImpl()
        {
            if (State.Equipment == null)
                return;

            if (!HasClipboard)
                return;

            var equipmentRef = State.Equipment.Base;

            /// <summary>
            /// Build the current context for planning/applying equipment changes.
            /// </summary>
            static EquipContext GetContext() =>
                new(State.Mode, PreviewController.Enabled, State.Character, State.Equipment);

            var ctx = GetContext();

            Func<EquipmentIndex, WItem> getCurrent = PreviewController.Enabled
                ? PreviewController.GetItem
                : State.Equipment.Get;

            var changes = DecodeEquipmentChanges(_clipboard.Code);
            var plan = EquipPlanner.BuildPlan(ctx, getCurrent, changes);
            if (plan == null)
                return;

            FillArraysFromPlan(getCurrent, plan, out var before, out var after);
            EquipEconomy.ComputeBatchEconomy(ctx, State.Equipment, before, after, plan);

            string sourceName = _clipboard.SourceUnitName;
            if (string.IsNullOrEmpty(sourceName))
                sourceName = L.S("equipment_clipboard_source_fallback", "Clipboard");

            string targetName = State.Character?.Name?.ToString() ?? string.Empty;

            // 2) Show "nothing to apply" immediately.
            if (plan.EquipOps == 0 && plan.UnequipOps == 0)
            {
                NothingToApplyPopup(sourceName, targetName);
                return;
            }

            // 2) Show "not enough gold" immediately (no confirm first).
            if (ctx.EconomyEnabled && plan.TotalCost > 0 && Player.Gold < plan.TotalCost)
            {
                NotEnoughGoldPopup(sourceName, targetName, plan.TotalCost);
                return;
            }

            // 3) Sentence style body + source info, and only mention what won't be equipped.

            string body = BuildSentenceBlock(plan, ctx.EconomyEnabled, sourceName, targetName);

            Inquiries.Popup(
                title: L.T("equipment_paste_confirm_title", "Copy Equipment"),
                description: new TextObject(body),
                onConfirm: () =>
                {
                    if (
                        State.Equipment == null
                        || !ReferenceEquals(State.Equipment.Base, equipmentRef)
                    )
                        return;

                    var liveCtx = GetContext();

                    Func<EquipmentIndex, WItem> liveGetCurrent = PreviewController.Enabled
                        ? PreviewController.GetItem
                        : State.Equipment.Get;

                    var livePlan = EquipPlanner.BuildPlan(
                        liveCtx,
                        liveGetCurrent,
                        DecodeEquipmentChanges(_clipboard.Code)
                    );

                    if (livePlan == null)
                        return;

                    FillArraysFromPlan(
                        liveGetCurrent,
                        livePlan,
                        out var liveBefore,
                        out var liveAfter
                    );
                    EquipEconomy.ComputeBatchEconomy(
                        liveCtx,
                        State.Equipment,
                        liveBefore,
                        liveAfter,
                        livePlan
                    );

                    if (livePlan.EquipOps == 0 && livePlan.UnequipOps == 0)
                        return;

                    // Still re-check; if gold changed after the popup, we must block.
                    if (
                        liveCtx.EconomyEnabled
                        && livePlan.TotalCost > 0
                        && Player.Gold < livePlan.TotalCost
                    )
                    {
                        NotEnoughGoldPopup(sourceName, targetName, livePlan.TotalCost);
                        return;
                    }

                    bool ok = EquipApplier.Apply(liveCtx, livePlan, () => ApplyPlan(livePlan));
                    if (!ok)
                    {
                        if (liveCtx.EconomyEnabled && livePlan.TotalCost > 0)
                            NotEnoughGoldPopup(sourceName, targetName, livePlan.TotalCost);

                        return;
                    }

                    _clipboard = null;

                    EventManager.FireBatch(() =>
                    {
                        EventManager.Fire(UIEvent.Item);
                        EventManager.Fire(UIEvent.Equipment);
                    });
                }
            );
        }
    }
}
