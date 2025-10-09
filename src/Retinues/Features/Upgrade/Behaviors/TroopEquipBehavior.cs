using System;
using System.Collections.Generic;
using Retinues.Troops.Edition;
using Retinues.GUI.Helpers;
using Retinues.Game.Menu;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace Retinues.Features.Upgrade.Behaviors
{
    [Serializable]
    public class PendingEquipData : IPendingData
    {
        [SaveableField(1)]
        public string TroopId;

        [SaveableField(2)]
        public int Remaining;

        [SaveableField(3)]
        public float Carry;

        [SaveableField(4)]
        public string ItemId;

        [SaveableField(5)]
        public EquipmentIndex Slot;

        string IPendingData.TroopId { get => TroopId; set => TroopId = value; }
        int IPendingData.Remaining { get => Remaining; set => Remaining = value; }
        float IPendingData.Carry { get => Carry; set => Carry = value; }
    }

    [SafeClass]
    public sealed class TroopEquipBehavior : BaseUpgradeBehavior<PendingEquipData>
    {
        private const float BaseEquipmentChangeTime = 0.01f;
        private static bool EquipmentChangeTakesTime =>
            Config.GetOption<bool>("EquipmentChangeTakesTime");
        private static float EquipmentChangeTimeModifier =>
            Config.GetOption<float>("EquipmentChangeTimeModifier");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override string SaveFieldName { get; set; } = "Retinues_Equip_Pending";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WItem GetStaged(WCharacter troop, EquipmentIndex slot)
        {
            if (troop == null)
                return null;
            if (Instance.Pending.TryGetValue(troop.StringId, out var dict))
            {
                foreach (var kvp in dict)
                {
                    var (s, itemId) = ParseObjKey(kvp.Key);
                    if (s == slot)
                        return itemId != null ? new WItem(itemId) : null;
                }
            }
            return null;
        }

        public static void UnstageEquipmentChange(WCharacter troop, EquipmentIndex slot)
        {
            if (troop == null)
                return;
            var troopId = troop.StringId;
            if (string.IsNullOrEmpty(troopId))
                return;

            if (Instance.Pending.TryGetValue(troopId, out var dict))
            {
                var slotPrefix = ((int)slot).ToString() + ":";
                // collect first to avoid modifying while enumerating
                var toRemove = new List<string>();
                foreach (var k in dict.Keys)
                    if (k.StartsWith(slotPrefix, StringComparison.Ordinal))
                        toRemove.Add(k);
                foreach (var k in toRemove)
                {
                    var (_, oldItemId) = ParseObjKey(k);
                    new WItem(oldItemId).Stock(); // Restock old item that was not equipped
                    dict.Remove(k);
                }
                if (dict.Count == 0)
                    Instance.Pending.Remove(troopId);
            }

            // Refresh settlement so the button updates immediately
            if (IsInManagedMenu(out _))
                RefreshManagedMenuOrDefault();
        }

        /// <summary>
        /// Stage an equipment change. If time is disabled, it equips immediately.
        /// </summary>
        public static void StageEquipmentChange(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Log.Info($"Staging equipment change: {troop?.Name} - {slot} => {item?.Name ?? "NONE"}");

            var troopId = troop?.StringId;
            var itemId = item?.StringId; // null => unequip

            if (string.IsNullOrEmpty(troopId))
                return;

            // If timing is off: clear any staged job for this slot, apply instantly, bail
            if (!EquipmentChangeTakesTime)
            {
                // remove any older staged change for this slot
                if (Instance.Pending.TryGetValue(troopId, out var map))
                {
                    var slotPrefix = ((int)slot).ToString() + ":";
                    // collect first to avoid modifying while enumerating
                    var toRemove = new List<string>();
                    foreach (var k in map.Keys)
                        if (k.StartsWith(slotPrefix, StringComparison.Ordinal))
                            toRemove.Add(k);
                    foreach (var k in toRemove)
                        map.Remove(k);
                    if (map.Count == 0)
                        Instance.Pending.Remove(troopId);
                }

                ApplyChange(troopId, slot, item);
                return;
            }

            // compute hours
            int hours;
            try
            {
                if (item != null)
                {
                    var costForTroop = EquipmentManager.GetItemValue(
                        item,
                        new WCharacter(troopId)
                    );
                    var rawHours = costForTroop / 100f * EquipmentChangeTimeModifier;
                    hours = Math.Max(1, (int)Math.Ceiling(rawHours));
                }
                else
                {
                    hours = Math.Max(
                        1,
                        (int)Math.Ceiling(BaseEquipmentChangeTime * EquipmentChangeTimeModifier)
                    );
                }
            }
            catch
            {
                hours = 1;
            }

            var objKey = ComposeObjKey(slot, itemId);
            var slotPrefixKey = ((int)slot).ToString() + ":";

            // Ensure only ONE staged job exists for this slot:
            if (Instance.Pending.TryGetValue(troopId, out var dict))
            {
                // 1) purge any previous job for the same slot (different item or old leftovers)
                var toRemove = new List<string>();
                foreach (var k in dict.Keys)
                {
                    if (k.StartsWith(slotPrefixKey, StringComparison.Ordinal) && k != objKey)
                    {
                        var (_, oldItemId) = ParseObjKey(k);
                        new WItem(oldItemId).Stock(); // Restock old item that was not equipped
                        // itemId is available here for further use if needed
                        toRemove.Add(k);
                    }
                }
                foreach (var k in toRemove)
                    dict.Remove(k);

                // if that was the last one, remove troop entry too
                if (dict.Count == 0)
                    Instance.Pending.Remove(troopId);

                // 2) same job restaged, just add time; otherwise insert fresh
                if (dict.TryGetValue(objKey, out var existing))
                {
                    existing.Remaining += hours;
                    Instance.SetPending(troopId, objKey, existing);
                }
                else
                {
                    Instance.SetPending(
                        troopId,
                        objKey,
                        new PendingEquipData
                        {
                            TroopId = troopId,
                            Remaining = hours,
                            ItemId = itemId,
                            Slot = slot,
                            Carry = 0f,
                        }
                    );
                }
            }
            else
            {
                // first entry for this troop
                Instance.SetPending(
                    troopId,
                    objKey,
                    new PendingEquipData
                    {
                        TroopId = troopId,
                        Remaining = hours,
                        ItemId = itemId,
                        Slot = slot,
                        Carry = 0f,
                    }
                );
            }

            // Refresh settlement so the button updates immediately
            if (IsInManagedMenu(out _))
                RefreshManagedMenuOrDefault();
        }

        /// <summary>Apply equipment change.</summary>
        public static void ApplyChange(string troopId, EquipmentIndex slot, WItem item)
        {
            if (string.IsNullOrEmpty(troopId))
                return;

            var troop = new WCharacter(troopId);
            EquipmentManager.Equip(troop, slot, item);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override string OptionId => "ret_equip_pending";
        protected override string OptionText => L.S("upgrade_equip_pending_btn", "Equip troops");
        protected override string InquiryTitle =>
            L.S("upgrade_equip_select_troop", "Select a troop to equip");
        protected override string InquiryDescription =>
            L.S("upgrade_equip_choose_troop", "Choose one pending troop to start equipping now.");
        protected override string InquiryAffirmative =>
            L.S("upgrade_equip_begin", "Begin upgrading equipment");
        protected override string InquiryNegative => L.S("cancel", "Cancel");
        protected override string ActionString => L.S("action_modify", "equip");
        protected override GameMenuOption.LeaveType LeaveType => GameMenuOption.LeaveType.Craft;

        protected override string BuildElementTitle(WCharacter troop, PendingEquipData data)
        {
            var item = data.ItemId != null ? new WItem(data.ItemId) : null;
            var itemName = item != null ? item.Name : L.S("upgrade_equip_unequip", "Unequip");
            return $"{troop.Name}\n{itemName} ({data.Remaining}h)";
        }

        // Start the timed wait for a single (troop, slot:item) entry
        protected override void StartWait(
            CampaignGameStarter starter,
            string troopId,
            string objId,
            PendingEquipData data
        )
        {
            var (slot, itemId) = ParseObjKey(objId);
            var troop = new WCharacter(troopId);
            var item = itemId != null ? new WItem(itemId) : null;

            TimedWaitMenu.Start(
                starter,
                idSuffix: $"equip_{troopId}_{(int)slot}_{itemId ?? "NONE"}",
                title: L.T("upgrade_equip_progress", "Equipping {NAME}...")
                    .SetTextVariable("NAME", troop.Name)
                    .ToString(),
                durationHours: data.Remaining,
                onCompleted: () =>
                {
                    // Atomic: apply once at the end
                    ApplyChange(troopId, slot, item);

                    // Remove this entry
                    RemovePending(troopId, objId);

                    if (Pending.Count == 0)
                        RefreshManagedMenuOrDefault();

                    Popup.Display(
                        L.T("equip_complete", "Equipment Updated"),
                        L.T("equip_complete_text", "{TROOP} has equipped ({ITEM}).")
                            .SetTextVariable("TROOP", new WCharacter(troopId).Name)
                            .SetTextVariable("ITEM", item?.Name)
                    );
                },
                onAborted: () => {
                    // Keep remaining time as-is; player can resume later
                },
                overlay: GameMenu.MenuOverlayType.SettlementWithBoth,
                onWholeHour: _ =>
                {
                    if (data.Remaining > 0)
                    {
                        data.Remaining -= 1;
                        // (No partial application for equipment; it’s atomic.)
                        Log.Debug(
                            $"Equip progress: {troop.Name} – 1h done, {data.Remaining}h left."
                        );
                    }
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string ComposeObjKey(EquipmentIndex slot, string itemIdOrNull) =>
            $"{(int)slot}:{itemIdOrNull ?? "NONE"}";

        private static (EquipmentIndex slot, string itemIdOrNull) ParseObjKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return (default, null);
            var idx = key.IndexOf(':');
            if (idx < 0)
                return (default, null);
            var slotStr = key.Substring(0, idx);
            var itemId = key.Substring(idx + 1);
            int slotInt = 0;
            int.TryParse(slotStr, out slotInt);
            return ((EquipmentIndex)slotInt, itemId == "NONE" ? null : itemId);
        }
    }
}
