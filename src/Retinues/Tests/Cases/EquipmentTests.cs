using System;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Stocks;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor;
using Retinues.Managers;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the equipment economy: stocks, loadout set invariants, shared-copy accounting,
    /// equip gating, and cost. All troop mutation happens on throwaway sandbox stubs and config
    /// is driven deterministically via TestConfig.
    /// </summary>
    public static class EquipmentTests
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Stocks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// StocksBehavior add/get/has-stock tracks counts and floors at zero.
        /// </summary>
        [GameTest(
            "StockAddGetRemove",
            "equipment",
            "StocksBehavior add/get tracks item stock and floors at zero"
        )]
        public static void StockAddGetRemove(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            Tests.AssertNotNull(StocksBehavior.Instance, "Stocks behavior is registered.");
            using var sandbox = new TestSandbox();

            var item = FirstItem(i => i.Value > 0);
            Tests.AssertNotNull(item, "An item is available.");
            var id = item.StringId;

            StocksBehavior.Set(id, 0);
            Tests.AssertEqual(0, StocksBehavior.Get(id), "Stock starts at zero.");
            Tests.AssertFalse(StocksBehavior.HasStock(id), "No stock at zero.");

            StocksBehavior.Add(id, 3);
            Tests.AssertEqual(3, StocksBehavior.Get(id), "Add increases stock.");
            Tests.AssertTrue(StocksBehavior.HasStock(id), "Has stock after add.");

            StocksBehavior.Add(id, -2);
            Tests.AssertEqual(1, StocksBehavior.Get(id), "Negative add reduces stock.");

            StocksBehavior.Add(id, -5);
            Tests.AssertEqual(0, StocksBehavior.Get(id), "Stock floors at zero.");
            Tests.AssertFalse(StocksBehavior.HasStock(id), "No stock after draining.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Set rules                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// A custom loadout keeps at least one battle and one civilian set; the only battle set
        /// cannot be removed or flipped to civilian.
        /// </summary>
        [GameTest(
            "LoadoutSetInvariants",
            "equipment",
            "Loadout keeps >=1 battle and >=1 civilian set; the last battle set is protected"
        )]
        public static void LoadoutSetInvariants(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var vanilla = sandbox.NewFaction()?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A vanilla template troop is available.");

            var troop = sandbox.NewStub();
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: true, keepSkills: false);
            troop.Loadout.Clear(); // 1 empty battle + 1 empty civilian
            troop.Loadout.Normalize();

            Tests.AssertTrue(troop.Loadout.BattleSets.Count >= 1, "At least one battle set.");
            Tests.AssertTrue(troop.Loadout.CivilianSets.Count >= 1, "At least one civilian set.");
            Tests.AssertFalse(troop.Loadout.Get(0).IsCivilian, "First set is a battle set.");

            // Cannot remove the only battle set.
            if (troop.Loadout.BattleSets.Count == 1)
            {
                troop.Loadout.Remove(troop.Loadout.BattleSets[0]);
                Tests.AssertTrue(
                    troop.Loadout.BattleSets.Count >= 1,
                    "The only battle set cannot be removed."
                );
            }

            // Cannot flip the only battle set to civilian.
            if (troop.Loadout.BattleSets.Count == 1)
            {
                troop.Loadout.ToggleCivilian(troop.Loadout.BattleSets[0], makeCivilian: true);
                Tests.AssertTrue(
                    troop.Loadout.BattleSets.Count >= 1,
                    "The only battle set cannot be flipped to civilian."
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Shared copies                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Equipping the same item in a second set needs no extra copy (shared); a new item needs
        /// exactly one. Run in free mode so no gold/stock is touched.
        /// </summary>
        [GameTest(
            "SharedCopyQuoteDelta",
            "equipment",
            "Same item across sets needs no extra copy; a new item needs one"
        )]
        public static void SharedCopyQuoteDelta(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            using var _free = TestConfig.Set(Config.EquippingTroopsCostsGold, false);
            using var _tier = TestConfig.Set(Config.AllowedTierDifference, 99);

            var vanilla = sandbox.NewFaction()?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A vanilla template troop is available.");

            var troop = sandbox.NewStub();
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: false, keepSkills: false);
            troop.Loadout.Clear(); // 1 empty battle (0) + 1 empty civilian (1)

            var slot = EquipmentIndex.Body;
            bool Eligible(WItem i) =>
                i.Slots.Contains(slot)
                && i.RelevantSkill == null
                && !i.IsCrafted
                && !i.IsCivilian;

            var itemA = FirstItem(Eligible);
            Tests.AssertNotNull(itemA, "A body-armor item is available.");
            var aId = itemA.StringId;
            var itemB = FirstItem(i => Eligible(i) && i.StringId != aId);
            Tests.AssertNotNull(itemB, "A second distinct body-armor item is available.");

            // Equip item A into set 0.
            var r = EquipmentManager.TryEquip(troop, 0, slot, itemA);
            Tests.AssertTrue(r.Ok, "Equipped item A into set 0.");

            // The SAME item into another set needs no extra copy.
            var qSame = EquipmentManager.QuoteEquip(troop, 1, slot, itemA);
            Tests.AssertEqual(0, qSame.DeltaAdd, "Shared item needs no extra copy.");

            // A DIFFERENT item into another set needs exactly one copy.
            var qNew = EquipmentManager.QuoteEquip(troop, 1, slot, itemB);
            Tests.AssertEqual(1, qNew.DeltaAdd, "A new item needs one copy.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Equip gating                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// A too-high-tier item is blocked by the tier-difference rule unless Ironclad is unlocked.
        /// </summary>
        [GameTest(
            "CanEquipTierGating",
            "equipment",
            "A too-high-tier item is blocked by the tier rule unless Ironclad is unlocked"
        )]
        public static void CanEquipTierGating(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            using var _tier = TestConfig.Set(Config.AllowedTierDifference, 0);

            var vanilla = sandbox.NewFaction()?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A vanilla template troop is available.");

            var troop = sandbox.NewStub();
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: false, keepSkills: true);

            // Body armor (no skill requirement) of a tier above the troop's, so only the tier
            // rule can trip the gate.
            var highTier = FirstItem(i =>
                i.Slots.Contains(EquipmentIndex.Body)
                && i.RelevantSkill == null
                && !i.IsCrafted
                && i.Tier > troop.Tier
            );
            Tests.AssertNotNull(highTier, "A higher-tier body-armor item is available.");

            bool ironclad = DoctrineAPI.IsDoctrineUnlocked<Ironclad>();
            bool canEquip = EquipmentManager.CanEquip(troop, highTier, out var reasons);

            if (ironclad)
            {
                Tests.AssertTrue(canEquip, "With Ironclad, the tier rule does not block armor.");
            }
            else
            {
                Tests.AssertFalse(canEquip, "Without Ironclad, a too-high-tier item is blocked.");
                Tests.AssertTrue(
                    (reasons & EquipmentManager.EquipLimitReason.TierDifference) != 0,
                    "Block reason includes TierDifference."
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Cost                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Item cost is zero in free mode and scales with the cost multiplier when paid.
        /// </summary>
        [GameTest(
            "GetItemCostScalesWithConfig",
            "equipment",
            "Item cost is zero in free mode and scales with the cost multiplier when paid"
        )]
        public static void GetItemCostScalesWithConfig(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            if (ClanScreen.IsStudioMode)
                return; // cost is always zero in studio mode; nothing to assert

            using var sandbox = new TestSandbox();

            var item = FirstItem(i => i.Value > 100 && !i.IsCrafted);
            Tests.AssertNotNull(item, "A valuable item is available.");

            using (TestConfig.Set(Config.EquippingTroopsCostsGold, false))
            {
                Tests.AssertEqual(0, EquipmentManager.GetItemCost(item), "Free mode costs nothing.");
            }

            using (TestConfig.Set(Config.EquippingTroopsCostsGold, true))
            {
                int c1;
                using (TestConfig.Set(Config.EquipmentCostMultiplier, 1f))
                {
                    c1 = EquipmentManager.GetItemCost(item);
                    Tests.AssertTrue(c1 > 0, "Paid mode has a positive cost.");
                    Tests.AssertTrue(
                        c1 <= item.Value,
                        "Cost does not exceed item value at 1x multiplier."
                    );
                }

                using (TestConfig.Set(Config.EquipmentCostMultiplier, 3f))
                {
                    int c3 = EquipmentManager.GetItemCost(item);
                    Tests.AssertTrue(c3 >= c1, "Higher multiplier yields higher-or-equal cost.");
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the first item matching the predicate, or null.
        /// </summary>
        private static WItem FirstItem(Func<WItem, bool> predicate)
        {
            foreach (var io in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                WItem item;
                try
                {
                    item = new WItem(io);
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
