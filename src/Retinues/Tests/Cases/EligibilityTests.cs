using System;
using System.Linq;
using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for EquipmentManager.CollectAvailableItems / BuildEligibilityList: the editor's
    /// item-list source. Guards the slot-fit-first filtering so non-fitting items never leak into
    /// a slot's list, and unlocked items surface only for the slots they fit.
    /// </summary>
    public static class EligibilityTests
    {
        /// <summary>
        /// Every item collected for a slot actually fits that slot (with everything unlocked).
        /// </summary>
        [GameTest(
            "CollectReturnsOnlySlotFittingItems",
            "eligibility",
            "Collected items all fit the requested slot"
        )]
        public static void CollectReturnsOnlySlotFittingItems(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            using var _all = TestConfig.Set(Config.AllEquipmentUnlocked, true);
            using var _town = TestConfig.Set(Config.RestrictItemsToTownInventory, false);

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A faction is available.");

            var collected = EquipmentManager.CollectAvailableItems(faction, EquipmentIndex.Body);
            Tests.AssertNotNull(collected, "CollectAvailableItems returned a list.");
            Tests.AssertTrue(collected.Count > 0, "Body slot has at least one eligible item.");

            foreach (var (item, _, _, _) in collected)
                Tests.AssertTrue(
                    item != null && item.Slots.Contains(EquipmentIndex.Body),
                    $"Collected item fits the Body slot: {item?.StringId}"
                );
        }

        /// <summary>
        /// An unlocked body item is collected for the Body slot but never for a weapon slot it
        /// cannot occupy (slot fit is enforced before any unlock rule).
        /// </summary>
        [GameTest(
            "UnlockedItemIsSlotScoped",
            "eligibility",
            "An unlocked item appears only for the slot it fits"
        )]
        public static void UnlockedItemIsSlotScoped(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            using var _all = TestConfig.Set(Config.AllEquipmentUnlocked, false);
            using var _culture = TestConfig.Set(Config.AllCultureEquipmentUnlocked, false);
            using var _kills = TestConfig.Set(Config.UnlockItemsFromKills, false);
            using var _town = TestConfig.Set(Config.RestrictItemsToTownInventory, false);

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A faction is available.");

            var bodyItem = FirstItem(i =>
                i.Slots.Contains(EquipmentIndex.Body)
                && !i.Slots.Contains(EquipmentIndex.Weapon0)
                && !i.IsCrafted
            );
            Tests.AssertNotNull(bodyItem, "A body-only item is available.");
            var id = bodyItem.StringId;

            bodyItem.Unlock();

            var bodyList = EquipmentManager.CollectAvailableItems(faction, EquipmentIndex.Body);
            Tests.AssertTrue(
                bodyList.Any(r => r.item?.StringId == id),
                "The unlocked body item is collected for the Body slot."
            );

            var weaponList = EquipmentManager.CollectAvailableItems(faction, EquipmentIndex.Weapon0);
            Tests.AssertFalse(
                weaponList.Any(r => r.item?.StringId == id),
                "The body item is not collected for a weapon slot it cannot occupy."
            );

            bodyItem.Lock();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static WItem FirstItem(Func<WItem, bool> predicate)
        {
            foreach (var io in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                try
                {
                    var item = new WItem(io);
                    if (predicate(item))
                        return item;
                }
                catch
                {
                    // Skip items that throw on property access.
                }
            }
            return null;
        }
    }
}
