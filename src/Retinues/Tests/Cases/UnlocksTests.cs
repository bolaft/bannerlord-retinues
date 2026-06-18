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
