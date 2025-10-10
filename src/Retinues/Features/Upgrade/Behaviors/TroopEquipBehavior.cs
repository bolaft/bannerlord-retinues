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

        [SaveableField(6)]
        public int CategoryValue;

        [SaveableField(7)]
        public int EquipmentIndex;

        // convenience EquipmentIndex accessor
        public EquipmentIndex Slot
        {
            get => (EquipmentIndex)SlotValue;
            set => SlotValue = (int)value;
        }

        // convenience WLoadout.Category accessors
        public WLoadout.Category Category
        {
            get => (WLoadout.Category)CategoryValue;
            set => CategoryValue = (int)value;
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
        private static int EquipmentChangeTimeModifier =>
            Config.GetOption<int>("EquipmentChangeTimeModifier");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override string SaveFieldName { get; set; } = "Retinues_Equip_Pending";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Get a staged equipment change for a troop, if any.
        /// </summary>
        public static PendingEquipData GetStagedChange(
            WCharacter troop,
            EquipmentIndex slot,
            WLoadout.Category category,
            int index = 0
        )
        {
            if (troop == null || Instance == null)
                return null;
            return Instance.GetPending(troop.StringId, ComposeKey((int)slot, (int)category, index));
        }

        /// <summary>
        /// Unstage a previously staged equipment change.
        /// </summary>
        public static void UnstageChange(
            WCharacter troop,
            EquipmentIndex slot,
            WLoadout.Category category,
            int index = 0
        )
        {
            Instance.RemovePending(troop?.StringId, ComposeKey((int)slot, (int)category, index));
        }

        /// <summary>
        /// Stage an equipment change.
        /// </summary>
        public static void StageEquipmentChange(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            WLoadout.Category category,
            int index = 0
        )
        {
            if (troop == null || item == null || Instance == null)
            {
                Log.Debug("[StageEquipmentChange] Null check failed, aborting.");
                return;
            }

            // compute time
            var cost = item == null ? 100 : EquipmentManager.GetItemValue(item, troop);
            var time = HoursFromGold(cost);

            // set staged job with a non-null objId
            Instance.SetPending(
                troop.StringId,
                ComposeKey((int)slot, (int)category, index),
                new PendingEquipData
                {
                    TroopId = troop.StringId,
                    Remaining = time,
                    ItemId = item.StringId,
                    Slot = slot,
                    Carry = 0f,
                    Category = category,
                    EquipmentIndex = index,
                }
            );

            if (IsInManagedMenu(out _))
                RefreshManagedMenuOrDefault();
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
            var item = new WItem(data.ItemId);

            TimedWaitMenu.Start(
                starter,
                idSuffix: $"equip_{objId}",
                title: L.T("upgrade_equip_progress", "Equipping {NAME}...")
                    .SetTextVariable("NAME", troop.Name)
                    .ToString(),
                durationHours: data.Remaining,
                onCompleted: () =>
                {
                    EquipmentManager.ApplyEquip(
                        troop,
                        data.Slot,
                        item,
                        data.Category,
                        data.EquipmentIndex
                    );
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
                onAborted: () => { },
                overlay: GameMenu.MenuOverlayType.SettlementWithBoth,
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

        /// <summary>Compose a unique key for (slot, item, category, index) quadruplet.</summary>
        private static string ComposeKey(int slot, int category, int equipmentIndex) =>
            $"{slot}:{category}:{equipmentIndex}";

        /// <summary>
        /// Estimate hours required to equip an item based on its value in gold.
        /// </summary>
        private static int HoursFromGold(int gold)
        {
            double g = Math.Max(1, gold);
            double x = Math.Log10(g);

            // Precomputed slopes/intercepts
            const double m1 = 10.0;
            const double b1 = -18.0;

            const double m2 = 17.16811869688072; // (24-12) / (log10(5000) - 3)
            const double b2 = -39.504356090642155; // 12 - m2*3

            const double m3 = 48.0; // (72-24) / (log10(50000)-log10(5000)) == 48
            const double b3 = -153.5505602081289; // 24 - m3*log10(5000)

            double raw =
                g <= 1000.0 ? (m1 * x + b1)
                : g <= 5000.0 ? (m2 * x + b2)
                : (m3 * x + b3);

            raw *= EquipmentChangeTimeModifier;
            raw /= 3; // Hardcoded adjustment

            // Round up to whole hours, keep a minimum of 1h.
            return Math.Max(1, (int)Math.Ceiling(raw));
        }
    }
}
