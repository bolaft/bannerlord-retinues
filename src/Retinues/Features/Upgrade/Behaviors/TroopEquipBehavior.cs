using System;
using Retinues.Game.Menu;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
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
        public int SlotValue;

        // convenience accessors for code
        public EquipmentIndex Slot
        {
            get => (EquipmentIndex)SlotValue;
            set => SlotValue = (int)value;
        }

        string IPendingData.TroopId
        {
            get => TroopId;
            set => TroopId = value;
        }
        int IPendingData.Remaining
        {
            get => Remaining;
            set => Remaining = value;
        }
        float IPendingData.Carry
        {
            get => Carry;
            set => Carry = value;
        }
    }

    [SafeClass]
    public sealed class TroopEquipBehavior : BaseUpgradeBehavior<PendingEquipData>
    {
        private const float BaseEquipmentChangeTime = 0.005f;
        private static int EquipmentChangeTimeModifier =>
            Config.GetOption<int>("EquipmentChangeTimeModifier");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override string SaveFieldName { get; set; } = "Retinues_Equip_Pending";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static PendingEquipData GetStagedChange(WCharacter troop, EquipmentIndex slot)
        {
            if (troop == null || Instance == null || Instance.Pending == null)
                return null;

            var list = Instance.GetPending(troop.StringId);
            if (list == null)
                return null;

            foreach (var data in list)
                if (data?.Slot == slot)
                    return data;

            return null;
        }

        /// <summary>
        /// Stage an equipment change.
        /// </summary>
        public static void StageEquipmentChange(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Log.Debug(
                "[StageEquipmentChange] Called for troop "
                    + troop?.Name
                    + ", slot "
                    + slot
                    + ", item "
                    + (item?.Name ?? "NONE")
            );

            var troopId = troop?.StringId;
            var itemId = item?.StringId;
            if (string.IsNullOrEmpty(troopId))
            {
                Log.Debug("[StageEquipmentChange] troopId is null or empty, aborting.");
                return;
            }

            // compute time
            int hours;
            try
            {
                if (item != null)
                {
                    Log.Debug("[StageEquipmentChange] Calculating hours for item.");
                    var costForTroop = EquipmentManager.GetItemValue(item, new WCharacter(troopId));
                    var rawHours = costForTroop / 100f * EquipmentChangeTimeModifier;
                    hours = Math.Max(1, (int)Math.Ceiling(rawHours));
                }
                else
                {
                    Log.Debug("[StageEquipmentChange] Calculating hours for unequip.");
                    hours = Math.Max(
                        1,
                        (int)Math.Ceiling(BaseEquipmentChangeTime * EquipmentChangeTimeModifier)
                    );
                }
            }
            catch
            {
                Log.Debug(
                    "[StageEquipmentChange] Exception during hour calculation, defaulting to 1."
                );
                hours = 1;
            }

            // ensure only one staged job per (troop, slot)
            if (Instance.Pending.TryGetValue(troopId, out var dict) && dict != null)
            {
                Log.Debug("[StageEquipmentChange] Removing previous staged jobs for slot.");
                var slotPrefix = ((int)slot).ToString() + ":";
                var toRemove = new System.Collections.Generic.List<string>();
                foreach (var k in dict.Keys)
                    if (k.StartsWith(slotPrefix, StringComparison.Ordinal))
                        toRemove.Add(k);
                foreach (var k in toRemove)
                    dict.Remove(k);
                if (dict.Count == 0)
                    Instance.Pending.Remove(troopId);
            }

            // set staged job with a non-null objId
            var objId = ComposeObjKey(slot, itemId);
            Log.Debug("[StageEquipmentChange] Setting new staged job: " + objId);
            Instance.SetPending(
                troopId,
                objId,
                new PendingEquipData
                {
                    TroopId = troopId,
                    Remaining = hours,
                    ItemId = itemId, // null => unequip
                    Slot = slot,
                    Carry = 0f,
                }
            );

            if (IsInManagedMenu(out _))
            {
                Log.Debug("[StageEquipmentChange] In managed menu, refreshing.");
                RefreshManagedMenuOrDefault();
            }
        }

        public static void UnstageEquipmentChange(WCharacter troop, EquipmentIndex slot)
        {
            Log.Debug(
                "[UnstageEquipmentChange] Called for troop " + troop?.Name + ", slot " + slot
            );

            if (troop == null || Instance == null || Instance.Pending == null)
            {
                Log.Debug("[UnstageEquipmentChange] Null check failed, aborting.");
                return;
            }

            if (
                !Instance.Pending.TryGetValue(troop.StringId, out var dict)
                || dict == null
                || dict.Count == 0
            )
            {
                Log.Debug("[UnstageEquipmentChange] No pending data for troop, aborting.");
                return;
            }

            var slotPrefix = ((int)slot).ToString() + ":";
            var toRemove = new System.Collections.Generic.List<string>();
            foreach (var k in dict.Keys)
                if (k.StartsWith(slotPrefix, StringComparison.Ordinal))
                    toRemove.Add(k);

            Log.Debug("[UnstageEquipmentChange] Removing staged jobs for slot.");
            foreach (var k in toRemove)
                dict.Remove(k);
            if (dict.Count == 0)
            {
                Log.Debug(
                    "[UnstageEquipmentChange] No more pending jobs for troop, removing entry."
                );
                Instance.Pending.Remove(troop.StringId);
            }
        }

        /// <summary>Apply equipment change.</summary>
        public static void ApplyChange(string troopId, EquipmentIndex slot, WItem item)
        {
            Log.Debug(
                "[ApplyChange] Called for troopId "
                    + troopId
                    + ", slot "
                    + slot
                    + ", item "
                    + (item?.Name ?? "NONE")
            );

            if (string.IsNullOrEmpty(troopId))
            {
                Log.Debug("[ApplyChange] troopId is null or empty, aborting.");
                return;
            }

            var troop = new WCharacter(troopId);
            Log.Debug("[ApplyChange] Calling EquipmentManager.ApplyEquip.");
            EquipmentManager.ApplyEquip(troop, slot, item);
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
        protected override string ActionString => L.S("action_equip", "equip");
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
            var troop = new WCharacter(troopId);
            var item = data.ItemId != null ? new WItem(data.ItemId) : null; // ← guard

            TimedWaitMenu.Start(
                starter,
                idSuffix: $"equip_{troopId}_{(int)data.Slot}_{data.ItemId ?? "NONE"}",
                title: L.T("upgrade_equip_progress", "Equipping {NAME}...")
                    .SetTextVariable("NAME", troop.Name)
                    .ToString(),
                durationHours: data.Remaining,
                onCompleted: () =>
                {
                    ApplyChange(troopId, data.Slot, item);
                    RemovePending(troopId, objId); // ← remove by the same key we staged

                    if (Pending.Count == 0)
                        RefreshManagedMenuOrDefault();

                    Popup.Display(
                        L.T("equip_complete", "Equipment Updated"),
                        L.T("equip_complete_text", "{TROOP} has equipped {ITEM}.")
                            .SetTextVariable("TROOP", new WCharacter(troopId).Name)
                            .SetTextVariable(
                                "ITEM",
                                item?.Name ?? L.S("upgrade_equip_unequip", "Unequip")
                            )
                    );
                },
                onAborted: () => { /* keep pending */
                },
                overlay: GameMenu.MenuOverlayType.SettlementWithBoth,
                onWholeHour: _ =>
                {
                    if (data.Remaining > 0)
                    {
                        data.Remaining -= 1;
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
            $"{(int)slot}:{(itemIdOrNull ?? "NONE")}";

        private static (EquipmentIndex Slot, string ItemIdOrNull) ParseObjKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return (default, null);
            var i = key.IndexOf(':');
            if (i < 0)
                return (default, null);
            var slotStr = key.Substring(0, i);
            var itemId = key.Substring(i + 1);
            _ = int.TryParse(slotStr, out var slotInt);
            return ((EquipmentIndex)slotInt, itemId == "NONE" ? null : itemId);
        }
    }
}
