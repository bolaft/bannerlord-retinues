using System;
using System.Runtime.Serialization;
using Retinues.Features.Unlocks;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class DiscardUnlockTests
    {
        private static EquipmentElement Item(ItemModifier modifier = null, ItemObject item = null)
        {
            if (item == null)
            {
                item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
                item.StringId = "discard_test_" + Guid.NewGuid().ToString("N");
            }
            return new EquipmentElement(item, modifier);
        }

        private static int Count(ItemRoster roster, EquipmentElement item)
        {
            int index = roster.FindIndexOfElement(item);
            return index < 0 ? 0 : roster[index].Amount;
        }

        [GameTest("CheatCatalogueOnlyCreditsAdditionalDiscards", "unlocks", RequiresCampaign = false)]
        public static void CheatCatalogueOnlyCreditsAdditionalDiscards()
        {
            var bow = Item();
            var armor = Item();
            var roster = new ItemRoster();
            roster.AddToCounts(bow, 10);
            roster.AddToCounts(armor, 10);
            CheatDiscardFilter.Capture(roster, true);
            Tests.AssertEqual(0, CheatDiscardFilter.ForUnlocks(roster).Count, "Opening and closing must not unlock the catalogue.");
            roster.AddToCounts(bow, 1);
            for (int repeat = 0; repeat < 2; repeat++)
            {
                var actual = CheatDiscardFilter.ForUnlocks(roster);
                Tests.AssertEqual(1, actual.Count);
                Tests.AssertEqual(1, Count(actual, bow));
                Tests.AssertEqual(0, Count(actual, armor));
                Tests.AssertEqual(11, Count(roster, bow), "Other event listeners must receive the untouched roster.");
                Tests.AssertEqual(10, Count(roster, armor));
            }
        }

        [GameTest("CheatTransfersAndCancellationGiveNoFreeProgress", "unlocks", RequiresCampaign = false)]
        public static void CheatTransfersAndCancellationGiveNoFreeProgress()
        {
            var bow = Item();
            var roster = new ItemRoster();
            roster.AddToCounts(bow, 10);
            CheatDiscardFilter.Capture(roster, true);
            roster.AddToCounts(bow, -4);
            Tests.AssertEqual(0, CheatDiscardFilter.ForUnlocks(roster).Count, "Taking cheat items earns no progress.");
            roster.AddToCounts(bow, 4);
            Tests.AssertEqual(0, CheatDiscardFilter.ForUnlocks(roster).Count, "Returning the same items earns no progress.");
            roster.AddToCounts(bow, 3);
            roster.AddToCounts(bow, -3);
            Tests.AssertEqual(0, CheatDiscardFilter.ForUnlocks(roster).Count, "Undoing a discard earns no progress.");
            roster.Clear();
            roster.AddToCounts(bow, 10); // The engine's Reset restores the initial roster in place.
            Tests.AssertEqual(0, CheatDiscardFilter.ForUnlocks(roster).Count, "Cancel/reset must not expose the catalogue.");
        }

        [GameTest("CheatDiscardPreservesModifiersAndNewItems", "unlocks", RequiresCampaign = false)]
        public static void CheatDiscardPreservesModifiersAndNewItems()
        {
            var bow = Item();
            var modifier = (ItemModifier)FormatterServices.GetUninitializedObject(typeof(ItemModifier));
            modifier.StringId = "discard_test_modifier";
            var modified = Item(modifier, bow.Item);
            var newItem = Item();
            var roster = new ItemRoster();
            roster.AddToCounts(bow, 10);
            CheatDiscardFilter.Capture(roster, true);
            roster.AddToCounts(bow, -3);
            roster.AddToCounts(modified, 2);
            roster.AddToCounts(newItem, 1);
            var actual = CheatDiscardFilter.ForUnlocks(roster);
            Tests.AssertEqual(2, actual.Count);
            Tests.AssertEqual(0, Count(actual, bow));
            Tests.AssertEqual(2, Count(actual, modified), "A real modified item must not be offset by an unmodified cheat item.");
            Tests.AssertEqual(1, Count(actual, newItem));
        }

        [GameTest("NormalDiscardsAndLootKeepTheirFullContents", "unlocks", RequiresCampaign = false)]
        public static void NormalDiscardsAndLootKeepTheirFullContents()
        {
            var item = Item();
            var cheat = new ItemRoster();
            cheat.AddToCounts(item, 10);
            CheatDiscardFilter.Capture(cheat, true);
            var loot = new ItemRoster();
            loot.AddToCounts(item, 10);
            CheatDiscardFilter.Capture(loot, false);
            Tests.AssertTrue(ReferenceEquals(loot, CheatDiscardFilter.ForUnlocks(loot)), "A previous cheat screen must not affect battle loot.");
            var normal = new ItemRoster();
            CheatDiscardFilter.Capture(normal, false);
            normal.AddToCounts(item, 3);
            Tests.AssertTrue(ReferenceEquals(normal, CheatDiscardFilter.ForUnlocks(normal)));
            CheatDiscardFilter.Capture(cheat, false); // Reused roster belonging to a new normal screen.
            Tests.AssertTrue(ReferenceEquals(cheat, CheatDiscardFilter.ForUnlocks(cheat)));
            Tests.AssertTrue(CheatDiscardFilter.ForUnlocks(null) == null);
            CheatDiscardFilter.Capture(null, true);
        }
    }
}
