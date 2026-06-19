using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Settings;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for v2 item unlock state: the MAttribute-backed UnlockProgress / IsUnlocked round-trip
    /// and the "no unlocking required" config override that forces everything unlocked.
    /// </summary>
    public static class UnlocksTests
    {
        [GameTest(
            "UnlockProgressRoundTripsAndClamps",
            "unlocks",
            "UnlockProgress persists, clamps to [0, threshold], and drives IsUnlocked"
        )]
        public static void UnlockProgressRoundTripsAndClamps(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var _need = TestConfig.Set(Configuration.EquipmentNeedsUnlocking, true);

            var item = WItem.GetEquipmentsForSlot(EquipmentIndex.Body).FirstOrDefault();
            Tests.AssertNotNull(item, "A body item is available.");

            var original = item.UnlockProgress;
            try
            {
                item.UnlockProgress = 0;
                Tests.AssertFalse(item.IsUnlocked, "Zero progress reads as locked.");
                Tests.AssertEqual(0, item.UnlockProgress, "Progress reads back zero.");

                item.UnlockProgress = 250;
                Tests.AssertEqual(250, item.UnlockProgress, "Mid progress round-trips.");
                Tests.AssertFalse(item.IsUnlocked, "Below threshold is still locked.");

                // Setter clamps above the threshold.
                item.UnlockProgress = WItem.UnlockThreshold + 5000;
                Tests.AssertEqual(
                    WItem.UnlockThreshold,
                    item.UnlockProgress,
                    "Progress clamps at the threshold."
                );
                Tests.AssertTrue(item.IsUnlocked, "At the threshold the item is unlocked.");

                // Setter clamps below zero.
                item.UnlockProgress = -100;
                Tests.AssertEqual(0, item.UnlockProgress, "Progress clamps at zero.");

                item.Unlock();
                Tests.AssertTrue(item.IsUnlocked, "Unlock() unlocks the item.");
            }
            finally
            {
                item.UnlockProgress = original;
            }
        }

        [GameTest(
            "NoUnlockingRequiredOverride",
            "unlocks",
            "With EquipmentNeedsUnlocking off, items always read as unlocked"
        )]
        public static void NoUnlockingRequiredOverride(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var item = WItem.GetEquipmentsForSlot(EquipmentIndex.Body).FirstOrDefault();
            Tests.AssertNotNull(item, "A body item is available.");

            int original;
            using (TestConfig.Set(Configuration.EquipmentNeedsUnlocking, true))
                original = item.UnlockProgress;

            try
            {
                using (TestConfig.Set(Configuration.EquipmentNeedsUnlocking, true))
                {
                    item.UnlockProgress = 0;
                    Tests.AssertFalse(
                        item.IsUnlocked,
                        "Locked when unlocking is required and progress is zero."
                    );
                }

                using (TestConfig.Set(Configuration.EquipmentNeedsUnlocking, false))
                {
                    Tests.AssertTrue(
                        item.IsUnlocked,
                        "Always unlocked when unlocking is not required."
                    );
                    Tests.AssertEqual(
                        WItem.UnlockThreshold,
                        item.UnlockProgress,
                        "Progress reports the threshold under the override."
                    );
                }
            }
            finally
            {
                using (TestConfig.Set(Configuration.EquipmentNeedsUnlocking, true))
                    item.UnlockProgress = original;
            }
        }
    }
}
