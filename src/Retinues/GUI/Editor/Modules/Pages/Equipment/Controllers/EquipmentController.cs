using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.Services.Context;
using Retinues.Editor.Services.Equipments;
using Retinues.Modules;
using Retinues.UI.Services;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers.Equipment
{
    /// <summary>
    /// Non-view logic for equipment set navigation and mutation.
    /// </summary>
    public class EquipmentController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Select Prev Set                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SelectPrevSet { get; } =
            Action<bool>("SelectPrevSet")
                .AddCondition(
                    civilian =>
                    {
                        var list = GetEquipments(civilian);
                        int i = IndexOfByBase(list, State.Equipment);
                        return i > 0;
                    },
                    L.T("equipment_no_more_sets", "No more equipment sets.")
                )
                .ExecuteWith(civilian =>
                {
                    var list = GetEquipments(civilian);
                    int i = IndexOfByBase(list, State.Equipment);
                    if (i <= 0)
                        return;

                    State.Equipment = list[i - 1];
                });

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Select Next Set                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SelectNextSet { get; } =
            Action<bool>("SelectNextSet")
                .AddCondition(
                    civilian =>
                    {
                        var list = GetEquipments(civilian);
                        int i = IndexOfByBase(list, State.Equipment);
                        return i >= 0 && i < list.Count - 1;
                    },
                    L.T("equipment_no_more_sets", "No more equipment sets.")
                )
                .ExecuteWith(civilian =>
                {
                    var list = GetEquipments(civilian);
                    int i = IndexOfByBase(list, State.Equipment);
                    if (i < 0 || i >= list.Count - 1)
                        return;

                    State.Equipment = list[i + 1];
                });

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Create Set                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> CreateSet { get; } =
            Action<bool>("CreateSet")
                .RequireValidEditingContext()
                .AddCondition(
                    _ => State.Character.IsHero == false,
                    L.T("equipment_hero_sets_reason", "Heroes cannot have multiple equipment sets.")
                )
                .DefaultTooltip(L.T("equipments_create_set", "Create a new equipment set."))
                .ExecuteWith(CreateSetImpl);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Delete Set                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> DeleteSet { get; } =
            Action<bool>("DeleteSet")
                .RequireValidEditingContext()
                .AddCondition(
                    _ => State.Character.IsHero == false,
                    L.T("equipment_hero_sets_reason", "Heroes cannot have multiple equipment sets.")
                )
                .AddCondition(
                    civilian => GetEquipments(civilian).Count > 1,
                    L.T(
                        "equipment_cannot_delete_last_set",
                        "At least one equipment set must remain."
                    )
                )
                .AddCondition(
                    civilian =>
                    {
                        if (civilian)
                            return true;

                        var target = State.Equipment;
                        return CanDeleteBattleEquipment(target);
                    },
                    L.T(
                        "equipment_delete_breaks_battle_types_reason",
                        "Cannot delete this set because it is the last one enabled for at least one battle type."
                    )
                )
                .DefaultTooltip(L.T("equipments_delete_set", "Delete the selected equipment set."))
                .ExecuteWith(DeleteSetImpl);

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

        public static EditorAction<bool> CopyEquipment { get; } =
            Action<bool>("CopyEquipment")
                .RequireValidEditingContext()
                .AddCondition(
                    _ => State.Equipment != null,
                    L.T("equipment_copy_no_set_reason", "No equipment set selected.")
                )
                .DefaultTooltip(L.T("equipment_copy_tooltip", "Copy equipment to clipboard."))
                .ExecuteWith(_ => CopyEquipmentImpl());

        public static EditorAction<bool> PasteEquipment { get; } =
            Action<bool>("PasteEquipment")
                .RequireValidEditingContext()
                .AddCondition(
                    _ => State.Equipment != null,
                    L.T("equipment_paste_no_set_reason", "No equipment set selected.")
                )
                .AddCondition(
                    _ => HasClipboard,
                    L.T("equipment_paste_empty_clipboard_reason", "Clipboard is empty.")
                )
                .DefaultTooltip(L.T("equipment_paste_tooltip", "Paste equipment from clipboard."))
                .ExecuteWith(_ => PasteEquipmentImpl());

        private static EquipContext Ctx() =>
            new(State.Mode, PreviewController.Enabled, State.Character, State.Equipment);

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

            var lines = new List<string>();

            // First line: the question (always first).
            lines.Add(
                L.T("equipment_paste_confirm_question", "Copy equipment from {SRC} to {DST}?")
                    .SetTextVariable("SRC", sourceName)
                    .SetTextVariable("DST", targetName)
                    .ToString()
            );

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

        private static void NothingToApplyPopup(string sourceName, string targetName)
        {
            Inquiries.Popup(
                title: L.T("equipment_paste_nothing_title", "Copy Equipment"),
                description: L.T(
                        "equipment_paste_nothing_desc2",
                        "Nothing from {SRC} can be equipped on {DST}."
                    )
                    .SetTextVariable("SRC", sourceName)
                    .SetTextVariable("DST", targetName)
            );
        }

        private static void PasteEquipmentImpl()
        {
            if (State.Equipment == null)
                return;

            if (!HasClipboard)
                return;

            var equipmentRef = State.Equipment.Base;

            var ctx = Ctx();

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

                    var liveCtx = Ctx();

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Crafted Items                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SetShowCrafted { get; } =
            Action<bool>("SetShowCrafted")
                .AddCondition(
                    _ => State.Mode == EditorMode.Player,
                    L.T("crafted_player_only_reason", "Only available in player mode.")
                )
                .DefaultTooltip(value =>
                    value
                        ? L.T("crafted_items_only_tooltip", "Show crafted weapons.")
                        : L.T("crafted_items_hide_tooltip", "Hide crafted weapons.")
                )
                .ExecuteWith(value => State.ShowCrafted = value)
                .Fire(UIEvent.Crafted);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Battle Types                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private enum BattleType
        {
            Field,
            Siege,
            Naval,
        }

        private static IEnumerable<BattleType> RequiredBattleTypes()
        {
            yield return BattleType.Field;
            yield return BattleType.Siege;

            if (Mods.NavalDLC.IsLoaded)
                yield return BattleType.Naval;
        }

        private static bool GetBattleTypeValue(MEquipment e, BattleType type)
        {
            if (e == null)
                return false;

            return type switch
            {
                BattleType.Field => e.FieldBattleSet,
                BattleType.Siege => e.SiegeBattleSet,
                BattleType.Naval => e.NavalBattleSet,
                _ => false,
            };
        }

        private static void SetBattleTypeValue(MEquipment e, BattleType type, bool value)
        {
            if (e == null)
                return;

            switch (type)
            {
                case BattleType.Field:
                    e.FieldBattleSet = value;
                    break;
                case BattleType.Siege:
                    e.SiegeBattleSet = value;
                    break;
                case BattleType.Naval:
                    e.NavalBattleSet = value;
                    break;
            }
        }

        private static TextObject GetDisableReason(BattleType type)
        {
            return type switch
            {
                BattleType.Field => L.T(
                    "battle_type_field_required_reason",
                    "At least one battle equipment set must remain enabled for field battles."
                ),
                BattleType.Siege => L.T(
                    "battle_type_siege_required_reason",
                    "At least one battle equipment set must remain enabled for siege battles."
                ),
                BattleType.Naval => L.T(
                    "battle_type_naval_required_reason",
                    "At least one battle equipment set must remain enabled for naval battles."
                ),
                _ => L.T(
                    "battle_type_required_reason",
                    "At least one battle equipment set must remain enabled for this battle type."
                ),
            };
        }

        private static bool CoverageSatisfiedAfterChange(
            List<MEquipment> battleEquipments,
            MEquipment changing,
            BattleType type,
            bool newValue
        )
        {
            if (type == BattleType.Naval && !Mods.NavalDLC.IsLoaded)
                return true;

            if (battleEquipments == null || battleEquipments.Count == 0)
                return false;

            var changingBase = changing?.Base;

            for (int i = 0; i < battleEquipments.Count; i++)
            {
                var e = battleEquipments[i];
                if (e == null || e.IsCivilian)
                    continue;

                bool value = GetBattleTypeValue(e, type);

                if (changingBase != null && e.Base == changingBase)
                    value = newValue;

                if (value)
                    return true;
            }

            return false;
        }

        private static bool CanDisableBattleType(MEquipment equipment, BattleType type)
        {
            if (equipment == null || equipment.IsCivilian)
                return false;

            if (!GetBattleTypeValue(equipment, type))
                return true;

            var battle = GetEquipments(civilian: false);
            return CoverageSatisfiedAfterChange(battle, equipment, type, newValue: false);
        }

        public static TextObject GetFieldBattleDisableReason()
        {
            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return null;

            if (!e.FieldBattleSet)
                return null;

            return CanDisableBattleType(e, BattleType.Field)
                ? null
                : GetDisableReason(BattleType.Field);
        }

        public static TextObject GetSiegeBattleDisableReason()
        {
            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return null;

            if (!e.SiegeBattleSet)
                return null;

            return CanDisableBattleType(e, BattleType.Siege)
                ? null
                : GetDisableReason(BattleType.Siege);
        }

        public static TextObject GetNavalBattleDisableReason()
        {
            if (!Mods.NavalDLC.IsLoaded)
                return null;

            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return null;

            if (!e.NavalBattleSet)
                return null;

            return CanDisableBattleType(e, BattleType.Naval)
                ? null
                : GetDisableReason(BattleType.Naval);
        }

        public static bool CanDeleteBattleEquipment(MEquipment equipment)
        {
            if (equipment == null || equipment.IsCivilian)
                return true;

            var battle = GetEquipments(civilian: false);
            if (battle == null || battle.Count <= 1)
                return false;

            var targetBase = equipment.Base;
            var remaining = battle.Where(e => e != null && e.Base != targetBase).ToList();

            foreach (var type in RequiredBattleTypes())
            {
                bool any = false;

                for (int i = 0; i < remaining.Count; i++)
                {
                    var e = remaining[i];
                    if (e == null || e.IsCivilian)
                        continue;

                    if (GetBattleTypeValue(e, type))
                    {
                        any = true;
                        break;
                    }
                }

                if (!any)
                    return false;
            }

            return true;
        }

        public static EditorAction<bool> SetFieldBattleSet { get; } =
            Action<bool>("SetFieldBattleSet")
                .AddCondition(
                    _ => State.Equipment != null && State.Equipment.IsCivilian == false,
                    L.T(
                        "battle_types_civilian_reason",
                        "Civilian equipment sets do not have battle type restrictions."
                    )
                )
                .AddCondition(
                    value => value || CanDisableBattleType(State.Equipment, BattleType.Field),
                    GetDisableReason(BattleType.Field)
                )
                .DefaultTooltip(value =>
                    value
                        ? L.T(
                            "battle_type_field_checkbox_tooltip_enable",
                            "Enable for field battles."
                        )
                        : L.T(
                            "battle_type_field_checkbox_tooltip_disable",
                            "Disable for field battles."
                        )
                )
                .ExecuteWith(SetFieldBattleSetImpl)
                .Fire(UIEvent.BattleToggle);

        private static void SetFieldBattleSetImpl(bool value)
        {
            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return;

            if (!value && !CanDisableBattleType(e, BattleType.Field))
                return;

            SetBattleTypeValue(e, BattleType.Field, value);
        }

        public static EditorAction<bool> SetSiegeBattleSet { get; } =
            Action<bool>("SetSiegeBattleSet")
                .AddCondition(
                    _ => State.Equipment != null && State.Equipment.IsCivilian == false,
                    L.T(
                        "battle_types_civilian_reason",
                        "Civilian equipment sets do not have battle type restrictions."
                    )
                )
                .AddCondition(
                    value => value || CanDisableBattleType(State.Equipment, BattleType.Siege),
                    GetDisableReason(BattleType.Siege)
                )
                .DefaultTooltip(value =>
                    value
                        ? L.T(
                            "battle_type_siege_checkbox_tooltip_enable",
                            "Enable for siege battles."
                        )
                        : L.T(
                            "battle_type_siege_checkbox_tooltip_disable",
                            "Disable for siege battles."
                        )
                )
                .ExecuteWith(SetSiegeBattleSetImpl)
                .Fire(UIEvent.BattleToggle);

        private static void SetSiegeBattleSetImpl(bool value)
        {
            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return;

            if (!value && !CanDisableBattleType(e, BattleType.Siege))
                return;

            SetBattleTypeValue(e, BattleType.Siege, value);
        }

        public static EditorAction<bool> SetNavalBattleSet { get; } =
            Action<bool>("SetNavalBattleSet")
                .AddCondition(
                    _ => Mods.NavalDLC.IsLoaded,
                    L.T("naval_dlc_not_loaded", "War Sails is not installed.")
                )
                .AddCondition(
                    _ => State.Equipment != null && State.Equipment.IsCivilian == false,
                    L.T(
                        "battle_types_civilian_reason",
                        "Civilian equipment sets do not have battle type restrictions."
                    )
                )
                .AddCondition(
                    value => value || CanDisableBattleType(State.Equipment, BattleType.Naval),
                    GetDisableReason(BattleType.Naval)
                )
                .DefaultTooltip(value =>
                    value
                        ? L.T(
                            "battle_type_naval_checkbox_tooltip_enable",
                            "Enable for naval battles."
                        )
                        : L.T(
                            "battle_type_naval_checkbox_tooltip_disable",
                            "Disable for naval battles."
                        )
                )
                .ExecuteWith(SetNavalBattleSetImpl)
                .Fire(UIEvent.BattleToggle);

        private static void SetNavalBattleSetImpl(bool value)
        {
            if (!Mods.NavalDLC.IsLoaded)
                return;

            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return;

            if (!value && !CanDisableBattleType(e, BattleType.Naval))
                return;

            SetBattleTypeValue(e, BattleType.Naval, value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Queries                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static List<MEquipment> GetEquipments(bool civilian)
        {
            var all = State.Character?.Editable?.Equipments;
            if (all == null || all.Count == 0)
                return [];

            return civilian
                ? all.FindAll(e => e != null && e.IsCivilian)
                : all.FindAll(e => e != null && !e.IsCivilian);
        }

        public static int IndexOfByBase(List<MEquipment> list, MEquipment equipment)
        {
            if (list == null || list.Count == 0)
                return -1;

            var target = equipment?.Base;
            if (target == null)
                return -1;

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e?.Base == target)
                    return i;
            }

            return -1;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Mutations                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void SelectFirstOrPromptCreate(
            bool civilian,
            Action<MEquipment> applySelection,
            bool allowCreate
        )
        {
            if (applySelection == null)
                return;

            var character = State.Character;

            var list = GetEquipments(civilian);
            var first = list.FirstOrDefault();
            if (first != null)
            {
                applySelection(first);
                return;
            }

            if (!allowCreate || character == null || character.IsHero)
            {
                applySelection(null);
                return;
            }

            Inquiries.Popup(
                title: civilian
                    ? L.T("inquiry_no_civilian_sets", "No Civilian Equipments")
                    : L.T("inquiry_no_battle_sets", "No Battle Equipments"),
                description: L.T(
                        "inquiry_no_equipment_sets_text",
                        "{UNIT_NAME} has no {EQUIPMENT_TYPE}.\n\nCreate an empty one?"
                    )
                    .SetTextVariable(
                        "EQUIPMENT_TYPE",
                        civilian
                            ? L.T("inquiry_no_equipment_sets_civilian", "civilian equipments")
                            : L.T("inquiry_no_equipment_sets_battle", "battle equipments")
                    )
                    .SetTextVariable("UNIT_NAME", character.Name.ToString()),
                onConfirm: () =>
                {
                    var c = State.Character;
                    var created = MEquipment.Create(c, civilian: civilian);
                    c.EquipmentRoster.Add(created);
                    var refreshed = GetEquipments(civilian).FirstOrDefault();
                    applySelection(refreshed ?? created);
                }
            );
        }

        private static void CreateSetImpl(bool civilian)
        {
            void Apply(MEquipment source = null)
            {
                var character = State.Character;
                var created = MEquipment.Create(character, civilian: civilian, source: source);
                character.EquipmentRoster.Add(created);
                State.Equipment = created;
            }

            Inquiries.Popup(
                title: L.T("inquiry_confirm_create_equipment_set_title", "Create Equipment"),
                description: L.T(
                    "inquiry_confirm_create_equipment_set_text",
                    "Do you want to create a new equipment set by copying the current set or create an empty one?"
                ),
                choice1Text: L.T("inquiry_create_equipment_set_choice_copy", "Copy"),
                choice2Text: L.T("inquiry_create_equipment_set_choice_empty", "Empty"),
                onChoice1: () => Apply(source: State.Equipment),
                onChoice2: () => Apply()
            );
        }

        private static void DeleteSetImpl(bool civilian)
        {
            Inquiries.Popup(
                title: L.T("inquiry_confirm_delete_equipment_set_title", "Delete Equipment"),
                description: L.T(
                    "inquiry_confirm_delete_equipment_set_text",
                    "Are you sure you want to delete the current equipment set? This action cannot be undone."
                ),
                onConfirm: () =>
                {
                    var ctx = Ctx();

                    if (!ctx.EconomyEnabled)
                    {
                        State.Character.EquipmentRoster.Remove(State.Equipment);
                        State.Equipment = GetEquipments(civilian).FirstOrDefault();
                        return;
                    }

                    StocksHelper.TrackRosterStock(
                        State.Character?.EquipmentRoster,
                        () =>
                        {
                            State.Character.EquipmentRoster.Remove(State.Equipment);
                            State.Equipment = GetEquipments(civilian).FirstOrDefault();
                        }
                    );
                }
            );
        }
    }
}
