using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Game.Menu;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
# if BL12
using TaleWorlds.CampaignSystem.Overlay;
# endif

namespace Retinues.Features.Staging
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

        [SaveableField(6)]
        public int CategoryValue; // Legacy, unused

        [SaveableField(7)]
        public int EquipmentIndex;

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

    /// <summary>
    /// Staged "equip" jobs with a unified public API (Stage/Unstage/Get/Clear).
    /// </summary>
    [SafeClass]
    public class EquipStagingBehavior : BaseStagingBehavior<PendingEquipData>
    {
        // Small typed payload for StageChange.
        public readonly struct EquipChange(EquipmentIndex slot, int equipmentIndex, WItem item)
        {
            public readonly EquipmentIndex Slot = slot;
            public readonly int EquipmentIndex = equipmentIndex;
            public readonly WItem Item = item; // null means "unequip"
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override string SaveFieldName { get; set; } = "Retinues_Equip_Pending";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Instance-level API (enforced by base)

        protected override PendingEquipData GetStagedChange(WCharacter troop, string objectKey)
        {
            if (troop == null || string.IsNullOrEmpty(objectKey))
                return null;
            return GetPending(troop.StringId, objectKey);
        }

        protected override List<PendingEquipData> GetStagedChanges(WCharacter troop)
        {
            if (troop == null)
                return [];
            return GetPending(troop.StringId);
        }

        protected override void StageChange(WCharacter troop, object payload)
        {
            if (troop == null)
                return;
            if (payload is not EquipChange p)
            {
                Log.Warn("TroopEquipBehavior.StageChange called with invalid payload.");
                return;
            }

            var item = p.Item;
            // compute time
            var cost = item == null ? 100 : EquipmentManager.GetItemCost(item, troop);
            var time = HoursFromGold(cost);

            SetPending(
                troop.StringId,
                ComposeKey((int)p.Slot, p.EquipmentIndex),
                new PendingEquipData
                {
                    TroopId = troop.StringId,
                    Remaining = time,
                    ItemId = item?.StringId,
                    Slot = p.Slot,
                    Carry = 0f,
                    EquipmentIndex = p.EquipmentIndex,
                }
            );

            if (IsInManagedMenu(out _))
                RefreshManagedMenuOrDefault();
        }

        protected override void UnstageChange(WCharacter troop, string objectKey)
        {
            if (troop == null || string.IsNullOrEmpty(objectKey))
                return;
            RemovePending(troop.StringId, objectKey);
        }

        protected override void UnstageChanges(WCharacter troop)
        {
            if (troop == null)
                return;

            foreach (var equipment in troop.Loadout.Equipments)
            foreach (var slot in WEquipment.Slots)
                RemovePending(troop.StringId, ComposeKey((int)slot, equipment.Index));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Static Accessors                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static PendingEquipData Get(
            WCharacter troop,
            EquipmentIndex slot,
            int equipmentIndex = 0
        ) =>
            ((EquipStagingBehavior)Instance).GetStagedChange(
                troop,
                ComposeKey((int)slot, equipmentIndex)
            );

        public static List<PendingEquipData> Get(WCharacter troop) =>
            ((EquipStagingBehavior)Instance).GetStagedChanges(troop);

        public static void Stage(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            int equipmentIndex = 0
        ) =>
            ((EquipStagingBehavior)Instance).StageChange(
                troop,
                new EquipChange(slot, equipmentIndex, item)
            );

        public static void Unstage(WCharacter troop, EquipmentIndex slot, int equipmentIndex = 0) =>
            ((EquipStagingBehavior)Instance).UnstageChange(
                troop,
                ComposeKey((int)slot, equipmentIndex)
            );

        public static void Unstage(WCharacter troop, int equipmentIndex = 0)
        {
            var behavior = (EquipStagingBehavior)Instance;
            foreach (var slot in WEquipment.Slots)
            {
                behavior.UnstageChange(troop, ComposeKey((int)slot, equipmentIndex));
            }
        }

        public static void Unstage(WCharacter troop)
        {
            var behavior = (EquipStagingBehavior)Instance;
            behavior.UnstageChanges(troop);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override string OptionId => "ret_equip_pending";
        protected override string OptionText => L.S("upgrade_equip_pending_btn", "Equip troops");
        protected override string InquiryTitle =>
            L.S("upgrade_equip_select_troop", "Select troops to equip");
        protected override string InquiryDescription =>
            L.S("upgrade_equip_choose_troop", "Choose one or more troops to start equipping now.");
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

        private readonly Dictionary<WCharacter, List<WItem>> _batchedActions = [];

        protected override void FinalModalSummary()
        {
            if (_batchedActions.Count == 0)
                return;

            var summaryLines = new List<string>();
            foreach (var entry in _batchedActions)
            {
                var troop = entry.Key;
                var items = entry.Value;

                var itemCounts = new Dictionary<string, int>();
                foreach (var item in items)
                {
                    var name =
                        item != null
                            ? item.Name.ToString()
                            : L.S("upgrade_equip_unequip", "Unequip").ToString();
                    if (!itemCounts.ContainsKey(name))
                        itemCounts[name] = 0;
                    itemCounts[name] += 1;
                }

                var itemSummaries = new List<string>();
                foreach (var ic in itemCounts)
                {
                    if (ic.Value > 1)
                        itemSummaries.Add($"{ic.Value} x {ic.Key}");
                    else
                        itemSummaries.Add(ic.Key);
                }

                var line = $"{troop.Name}: {string.Join(", ", itemSummaries)}";
                summaryLines.Add(line);
            }

            var summary = L.T(
                    "equip_complete_summary",
                    "The following troops have equipped new items:\n\n{SUMMARY}"
                )
                .SetTextVariable("SUMMARY", string.Join("\n", summaryLines));

            Notifications.Popup(L.T("equip_complete", "Equipment Updated"), summary);

            _batchedActions.Clear();
        }

        // Start the timed wait for a single (troop, slot:item) entry
        protected override void StartWait(
            CampaignGameStarter starter,
            string troopId,
            string objId,
            PendingEquipData data,
            Action onAfterCompleted = null
        )
        {
            var troop = new WCharacter(troopId);
            var item = data.ItemId != null ? new WItem(data.ItemId) : null;

            TimedWaitMenu.Start(
                starter,
                idSuffix: $"equip_{objId}",
                title: L.T("upgrade_equip_progress", "Equipping {NAME}...")
                    .SetTextVariable("NAME", troop.Name)
                    .ToString(),
                durationHours: data.Remaining,
                onCompleted: () =>
                {
                    var finalTroop = new WCharacter(troopId);
                    var finalItem = data.ItemId != null ? new WItem(data.ItemId) : null;

                    EquipmentManager.ApplyImmediate(
                        finalTroop,
                        data.EquipmentIndex,
                        data.Slot,
                        finalItem
                    );

                    RemovePending(troopId, objId);

                    if (Pending.Count == 0)
                        RefreshManagedMenuOrDefault();

                    var message = L.T("equip_complete_text", "{TROOP} has equipped {ITEM}.")
                        .SetTextVariable("TROOP", finalTroop.Name)
                        .SetTextVariable(
                            "ITEM",
                            finalItem != null
                                ? finalItem.Name
                                : L.S("upgrade_equip_unequip", "Unequip")
                        );

                    if (!_batchActive)
                    {
                        Notifications.Popup(L.T("equip_complete", "Equipment Updated"), message);
                    }
                    else
                    {
                        if (!_batchedActions.ContainsKey(finalTroop))
                            _batchedActions[finalTroop] = new List<WItem>();
                        _batchedActions[finalTroop].Add(finalItem);

                        Log.Message(message.ToString());
                    }

                    // Continue the batch
                    onAfterCompleted?.Invoke();
                },
                onAborted: () =>
                {
                    // Keep staged; just continue the batch so others can run
                    onAfterCompleted?.Invoke();
                },
# if BL13
                overlay: GameMenu.MenuOverlayType.SettlementWithBoth,
# else
                overlay: GameOverlays.MenuOverlayType.SettlementWithBoth,
# endif
                onWholeHour: _ =>
                {
                    if (data.Remaining > 0)
                        data.Remaining -= 1;
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Compose a unique key for (slot, equipmentIndex).
        /// </summary>
        private static string ComposeKey(int slot, int equipmentIndex) =>
            $"{slot}:{equipmentIndex}";

        /// <summary>
        /// Estimate hours required to equip an item based on its value in gold.
        /// </summary>
        private static int HoursFromGold(int gold)
        {
            double g = Math.Max(1, gold);
            double x = Math.Log10(g);

            const double m1 = 10.0;
            const double b1 = -18.0;

            const double m2 = 17.16811869688072;
            const double b2 = -39.504356090642155;

            const double m3 = 48.0;
            const double b3 = -153.5505602081289;

            double raw =
                g <= 1000.0 ? (m1 * x + b1)
                : g <= 5000.0 ? (m2 * x + b2)
                : (m3 * x + b3);

            raw *= Config.EquipmentChangeTimeModifier;
            raw /= 5;

            return Math.Max(1, (int)Math.Ceiling(raw));
        }
    }
}
