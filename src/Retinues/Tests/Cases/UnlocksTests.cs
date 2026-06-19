using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Features.Unlocks;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for item unlocking: the unlock store, the kill-progress engine, and the paste
    /// availability gate. Config is driven deterministically and the unlock store is restored by
    /// the sandbox.
    /// </summary>
    public static class UnlocksTests
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Unlock store                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unlock/Lock toggles an item's unlocked state and Unlock is idempotent.
        /// </summary>
        [GameTest("UnlockLockToggle", "unlocks", "Unlock/Lock toggles an item's unlocked state")]
        public static void UnlockLockToggle(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            Tests.AssertNotNull(UnlocksBehavior.Instance, "Unlocks behavior is registered.");
            using var sandbox = new TestSandbox();

            var item = FirstItem(i => i.Slots.Count > 0 && !i.IsCrafted);
            Tests.AssertNotNull(item, "An equippable item is available.");

            item.Lock();
            Tests.AssertFalse(item.IsUnlocked, "Item starts locked.");

            item.Unlock();
            Tests.AssertTrue(item.IsUnlocked, "Item is unlocked after Unlock().");

            item.Unlock(); // idempotent
            Tests.AssertTrue(item.IsUnlocked, "Unlock is idempotent.");

            item.Lock();
            Tests.AssertFalse(item.IsUnlocked, "Item is locked after Lock().");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Kill progress                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Defeat progress accumulates below the threshold and unlocks the item once the kill
        /// threshold is reached.
        /// </summary>
        [GameTest(
            "KillProgressUnlocksAtThreshold",
            "unlocks",
            "Defeat progress accumulates and unlocks the item at the kill threshold"
        )]
        public static void KillProgressUnlocksAtThreshold(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            var unlocks = UnlocksBehavior.Instance;
            Tests.AssertNotNull(unlocks, "Unlocks behavior is registered.");

            using var sandbox = new TestSandbox();
            using var _kills = TestConfig.Set(Config.UnlockItemsFromKills, true);
            using var _all = TestConfig.Set(Config.AllEquipmentUnlocked, false);
            using var _bonus = TestConfig.Set(Config.PlayerCultureUnlockBonus, false);
            using var _threshold = TestConfig.Set(Config.RequiredKillsPerItem, 3);

            var item = FirstItem(i => i.Slots.Count > 0 && !i.IsCrafted);
            Tests.AssertNotNull(item, "An equippable item is available.");
            var id = item.StringId;

            // Start from a clean, locked state.
            item.Lock();
            unlocks.ProgressByItemId.Remove(id);

            unlocks.AddUnlockCounts(
                new Dictionary<ItemObject, int> { { item.Base, 2 } },
                addCultureBonuses: false
            );
            Tests.AssertEqual(2, UnlocksBehavior.GetProgress(id), "Progress accumulates.");
            Tests.AssertFalse(UnlocksBehavior.IsUnlocked(id), "Not unlocked below threshold.");
            Tests.AssertTrue(UnlocksBehavior.InProgress(id), "In progress below threshold.");

            unlocks.AddUnlockCounts(
                new Dictionary<ItemObject, int> { { item.Base, 1 } },
                addCultureBonuses: false
            );
            Tests.AssertEqual(3, UnlocksBehavior.GetProgress(id), "Progress reaches the threshold.");
            Tests.AssertTrue(UnlocksBehavior.IsUnlocked(id), "Unlocked at the threshold.");
            Tests.AssertFalse(
                UnlocksBehavior.InProgress(id),
                "No longer 'in progress' once unlocked."
            );
        }

        /// <summary>
        /// The kill-progress engine is a no-op when all equipment is globally unlocked.
        /// </summary>
        [GameTest(
            "AllUnlockedDisablesKillProgress",
            "unlocks",
            "AddUnlockCounts is a no-op when AllEquipmentUnlocked is set"
        )]
        public static void AllUnlockedDisablesKillProgress(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            var unlocks = UnlocksBehavior.Instance;
            Tests.AssertNotNull(unlocks, "Unlocks behavior is registered.");

            using var sandbox = new TestSandbox();
            using var _all = TestConfig.Set(Config.AllEquipmentUnlocked, true);
            using var _kills = TestConfig.Set(Config.UnlockItemsFromKills, true);

            var item = FirstItem(i => i.Slots.Count > 0 && !i.IsCrafted);
            Tests.AssertNotNull(item, "An equippable item is available.");
            var id = item.StringId;
            unlocks.ProgressByItemId.Remove(id);

            int before = UnlocksBehavior.GetProgress(id);
            unlocks.AddUnlockCounts(
                new Dictionary<ItemObject, int> { { item.Base, 5 } },
                addCultureBonuses: false
            );
            Tests.AssertEqual(
                before,
                UnlocksBehavior.GetProgress(id),
                "No progress added when all equipment is unlocked."
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Availability gate                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Paste availability respects slot match, explicit unlock, and the all-unlocked override.
        /// </summary>
        [GameTest(
            "IsUnlockedForPasteGating",
            "unlocks",
            "Paste availability respects slot match, explicit unlock, and the all-unlocked override"
        )]
        public static void IsUnlockedForPasteGating(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var vanilla = sandbox.NewFaction()?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A vanilla template troop is available.");
            var troop = sandbox.NewStub();
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: false, keepSkills: false);

            var bodyItem = FirstItem(i =>
                i.Slots.Contains(EquipmentIndex.Body)
                && !i.Slots.Contains(EquipmentIndex.Horse)
                && !i.IsCrafted
            );
            Tests.AssertNotNull(bodyItem, "A body-armor item is available.");

            // Slot mismatch is always rejected (checked before any config).
            Tests.AssertFalse(
                EquipmentManager.IsUnlockedForPaste(troop, EquipmentIndex.Horse, bodyItem),
                "An item cannot be pasted into a slot it does not fit."
            );

            using (TestConfig.Set(Config.AllEquipmentUnlocked, false))
            {
                bodyItem.Unlock();
                Tests.AssertTrue(
                    EquipmentManager.IsUnlockedForPaste(troop, EquipmentIndex.Body, bodyItem),
                    "An explicitly unlocked item is available for paste."
                );
                bodyItem.Lock();
            }

            using (TestConfig.Set(Config.AllEquipmentUnlocked, true))
            {
                Tests.AssertTrue(
                    EquipmentManager.IsUnlockedForPaste(troop, EquipmentIndex.Body, bodyItem),
                    "All-equipment-unlocked makes any slot-matching item available."
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Lookup mirror                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// IsUnlocked (backed by the O(1) lookup mirror) stays in agreement with the persisted
        /// UnlockedItemIds list across Unlock/Lock.
        /// </summary>
        [GameTest(
            "IsUnlockedMirrorsStore",
            "unlocks",
            "IsUnlocked agrees with the UnlockedItemIds list across Unlock/Lock"
        )]
        public static void IsUnlockedMirrorsStore(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            var unlocks = UnlocksBehavior.Instance;
            Tests.AssertNotNull(unlocks, "Unlocks behavior is registered.");

            using var sandbox = new TestSandbox();
            using var _all = TestConfig.Set(Config.AllEquipmentUnlocked, false);

            var item = FirstItem(i => i.Slots.Count > 0 && !i.IsCrafted);
            Tests.AssertNotNull(item, "An equippable item is available.");
            var id = item.StringId;

            item.Lock();
            Tests.AssertFalse(UnlocksBehavior.IsUnlocked(id), "Locked: IsUnlocked false.");
            Tests.AssertFalse(
                unlocks.UnlockedItemIds.Contains(id),
                "Locked: id absent from the store."
            );

            item.Unlock();
            Tests.AssertTrue(UnlocksBehavior.IsUnlocked(id), "Unlocked: IsUnlocked true.");
            Tests.AssertTrue(
                unlocks.UnlockedItemIds.Contains(id),
                "Unlocked: id present in the store (mirror matches list)."
            );

            item.Lock();
            Tests.AssertFalse(UnlocksBehavior.IsUnlocked(id), "Re-locked: IsUnlocked false.");
            Tests.AssertFalse(
                unlocks.UnlockedItemIds.Contains(id),
                "Re-locked: id removed from the store (mirror matches list)."
            );
        }

        /// <summary>
        /// Reset() clears unlocks, and the lookup reflects it immediately (no stale mirror).
        /// </summary>
        [GameTest("ResetClearsUnlocks", "unlocks", "Reset clears unlocks and the lookup follows")]
        public static void ResetClearsUnlocks(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            var unlocks = UnlocksBehavior.Instance;
            Tests.AssertNotNull(unlocks, "Unlocks behavior is registered.");

            using var sandbox = new TestSandbox();
            using var _all = TestConfig.Set(Config.AllEquipmentUnlocked, false);

            var item = FirstItem(i => i.Slots.Count > 0 && !i.IsCrafted);
            Tests.AssertNotNull(item, "An equippable item is available.");
            var id = item.StringId;

            item.Unlock();
            Tests.AssertTrue(UnlocksBehavior.IsUnlocked(id), "Item is unlocked before reset.");

            unlocks.Reset();
            Tests.AssertFalse(
                UnlocksBehavior.IsUnlocked(id),
                "After Reset the item reads as locked (mirror cleared)."
            );
        }

        /// <summary>
        /// Many unlocked ids all resolve correctly while an untouched id stays locked: exercises the
        /// O(1) membership path at scale.
        /// </summary>
        [GameTest(
            "BulkUnlockLookupConsistent",
            "unlocks",
            "Bulk-unlocked ids all read unlocked while an untouched id stays locked"
        )]
        public static void BulkUnlockLookupConsistent(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            var unlocks = UnlocksBehavior.Instance;
            Tests.AssertNotNull(unlocks, "Unlocks behavior is registered.");

            using var sandbox = new TestSandbox();
            using var _all = TestConfig.Set(Config.AllEquipmentUnlocked, false);

            // Collect a batch of eligible ids; hold the first one back as a locked control.
            var ids = new List<string>();
            foreach (var io in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                try
                {
                    var item = new WItem(io);
                    if (item.Slots.Count == 0 || item.IsCrafted)
                        continue;
                    ids.Add(item.StringId);
                }
                catch
                {
                    // Skip items that throw on property access.
                }

                if (ids.Count >= 200)
                    break;
            }

            Tests.AssertTrue(ids.Count >= 2, "At least two eligible items are available.");

            var control = ids[0];
            new WItem(control).Lock();

            for (int i = 1; i < ids.Count; i++)
                new WItem(ids[i]).Unlock();

            for (int i = 1; i < ids.Count; i++)
                Tests.AssertTrue(
                    UnlocksBehavior.IsUnlocked(ids[i]),
                    $"Every bulk-unlocked id reads unlocked: {ids[i]}"
                );

            Tests.AssertFalse(
                UnlocksBehavior.IsUnlocked(control),
                "The untouched control id stays locked."
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
