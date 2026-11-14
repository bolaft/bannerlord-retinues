using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Retinues.Behaviors.Unlocks;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Model.Attributes;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for v2 item unlock state: the MAttribute-backed UnlockProgress / IsUnlocked round-trip
    /// and the "no unlocking required" config override that forces everything unlocked.
    /// </summary>
    public static class UnlocksTests
    {
        private static WItem IsolatedItem(GameTestContext ctx, int progress, int stock)
        {
            var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            item.StringId = "reset_unlocks_test_" + Guid.NewGuid().ToString("N");
            // WItem attributes share storage by ID, even for unregistered fixtures.
            // Remove only these test keys; never clear the campaign's attribute store.
            ctx.Defer(() =>
            {
                var values = (IDictionary)typeof(MAttribute<int>.Store)
                    .GetField("Values", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                values.Remove($"{typeof(WItem).FullName}:{item.StringId}:UnlockProgressAttribute");
                values.Remove($"{typeof(WItem).FullName}:{item.StringId}:StockAttribute");
            });
            return new WItem(item) { UnlockProgress = progress, Stock = stock };
        }

        [GameTest("ResetUnlockProgressSurvivesReloadWithAccessOverride", "unlocks", RequiresCampaign = false)]
        public static void ResetUnlockProgressSurvivesReloadWithAccessOverride(GameTestContext ctx)
        {
            using var _need = TestConfig.Set(Configuration.EquipmentNeedsUnlocking, false);
            var items = new[] { IsolatedItem(ctx, WItem.UnlockThreshold, 7), IsolatedItem(ctx, 250, 3), IsolatedItem(ctx, 0, 1) };

            Tests.AssertEqual(2, WItem.ResetUnlockProgress(items), "Both full unlocks and partial progress are reset.");
            Tests.AssertFalse(Configuration.EquipmentNeedsUnlocking.Value, "The command does not change the player's settings.");
            Tests.AssertEqual(0, WItem.ResetUnlockProgress(items), "Repeating the reset makes no further changes.");

            foreach (var original in items)
            {
                Tests.AssertTrue(original.IsUnlocked, "The setting still grants access after earned progress is cleared.");
                var item = original;
                for (int load = 0; load < 3; load++)
                {
                    string saved = item.Serialize();
                    Tests.AssertTrue(!string.IsNullOrEmpty(saved), "The reset remains eligible for saving.");
                    Tests.AssertEqual("0", XDocument.Parse(saved).Root.Element("UnlockProgressAttribute")?.Value,
                        "An explicit zero is saved despite the access override.");
                    item = IsolatedItem(ctx, WItem.UnlockThreshold, 99);
                    item.Deserialize(saved);
                    Tests.AssertEqual(original.Stock, item.Stock, "Stocks survive reset and reload unchanged.");
                }
                Tests.AssertEqual(0, WItem.ResetUnlockProgress(new[] { item }), "Reloaded earned progress is still zero.");
            }
        }

        [GameTest("ResetUnlocksCommandRejectsArgumentsAndMissingCampaign", "unlocks", RequiresCampaign = false)]
        public static void ResetUnlocksCommandRejectsArgumentsAndMissingCampaign()
        {
            Tests.AssertEqual("Usage: retinues.reset_unlocks", Cheats.ResetUnlocks(new List<string> { "unexpected" }));
            if (Campaign.Current == null)
                Tests.AssertEqual("No campaign loaded.", Cheats.ResetUnlocks(new List<string>()));
        }

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
