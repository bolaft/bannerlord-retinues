using System;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Behaviors.Troops;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    public static class RosterSaveCompatibilityTests
    {
        private static CharacterObject Troop(string id)
        {
            var troop = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
            troop.StringId = id;
            return troop;
        }

        private static CharacterObject RegisteredTroop(string id)
        {
            var troop = Troop(id);
            troop.Initialize();
            // Mimic saved registration without inserting test objects into the game's manager.
            AccessTools.Property(typeof(MBObjectBase), "IsRegistered").SetValue(troop, true);
            return troop;
        }

        private static void AddLoadedRow(TroopRoster roster, CharacterObject troop, int count,
            bool insertAtFront = false, int woundedCount = 0, int xpChange = 0)
        {
            // Seed saved rows directly, as deserialization does. In BL12 even adding a zero-XP
            // dummy stack calls ClampXp, which requires a fully initialized campaign troop.
            var data = (TroopRosterElement[])AccessTools.Field(typeof(TroopRoster), "data").GetValue(roster);
            int previousCount = roster.Count;
            Array.Resize(ref data, previousCount + 1);
            int index = insertAtFront ? 0 : previousCount;
            if (insertAtFront) Array.Copy(data, 0, data, 1, previousCount);
            data[index] = new TroopRosterElement(troop) { Number = count, WoundedNumber = woundedCount, Xp = xpChange };
            AccessTools.Field(typeof(TroopRoster), "data").SetValue(roster, data);
            AccessTools.Field(typeof(TroopRoster), "_count").SetValue(roster, previousCount + 1);
            AccessTools.Method(typeof(TroopRoster), "InitializeCachedData").Invoke(roster, null);
            roster.UpdateVersion();
        }

        [GameTest("RosterRepairConservesGeneratedDuplicateStacks", "save-contracts", RequiresCampaign = false)]
        public static void RosterRepairConservesGeneratedDuplicateStacks(GameTestContext ctx)
        {
            for (int trial = 0; trial < 100; trial++)
            {
                var canonical = Troop("retinues_custom_generated_roster");
                var roster = TroopRoster.CreateDummyTroopRoster();
                int total = 0, wounded = 0, xp = 0;
                int twins = ctx.Random.Next(1, 9);
                for (int i = 0; i <= twins; i++)
                {
                    int men = ctx.Random.Next(1, 40), injured = ctx.Random.Next(men + 1), earned = ctx.Random.Next(100000);
                    AddLoadedRow(roster, i == 0 ? canonical : Troop(canonical.StringId), men,
                        insertAtFront: ctx.Random.Next(2) == 0, woundedCount: injured, xpChange: earned);
                    total += men; wounded += injured; xp += earned;
                }
                Tests.AssertEqual(twins, StubIntegrityBehavior.CanonicalizeRoster(roster, _ => canonical));
                Tests.AssertEqual(1, roster.Count);
                Tests.AssertEqual(total, roster.GetTroopCount(canonical));
                Tests.AssertEqual(wounded, roster.GetElementWoundedNumber(0));
                Tests.AssertEqual(xp, roster.GetElementXp(canonical));
                Tests.AssertEqual(total, roster.TotalManCount);
                Tests.AssertEqual(wounded, roster.TotalWounded);
            }
        }

        [GameTest("RosterRepairRemovesEmptyObsoleteOrdinaryTroops", "save-contracts", RequiresCampaign = false)]
        public static void RosterRepairRemovesEmptyObsoleteOrdinaryTroops(GameTestContext ctx)
        {
            foreach (string id in new[] { "aserai_mameluke_axeman", "ne_imperial_trained_archer" })
            for (int trial = 0; trial < 30; trial++)
            {
                var canonical = RegisteredTroop(id);
                var roster = TroopRoster.CreateDummyTroopRoster();
                int men = ctx.Random.Next(2, 50), xp = ctx.Random.Next(1, 100000);
                AddLoadedRow(roster, canonical, men, woundedCount: 1, xpChange: xp);
                int ghosts = ctx.Random.Next(1, 12);
                for (int i = 0; i < ghosts; i++)
                    AddLoadedRow(roster, Troop(id), 0, insertAtFront: ctx.Random.Next(2) == 0);
                // Populate the cached enumeration, just as daily training does.
                roster.GetTroopRoster();
                int version = roster.VersionNo;
                Tests.AssertEqual(ghosts, StubIntegrityBehavior.CanonicalizeRoster(roster, _ => canonical, out int removed));
                Tests.AssertEqual(ghosts, removed, "Only committed removals are reported for logging.");
                Tests.AssertEqual(1, roster.Count);
                Tests.AssertTrue(ReferenceEquals(canonical, roster.GetCharacterAtIndex(0)));
                Tests.AssertEqual(men, roster.GetElementNumber(0));
                Tests.AssertEqual(1, roster.GetElementWoundedNumber(0));
                Tests.AssertEqual(xp, roster.GetElementXp(0));
                Tests.AssertEqual(men, roster.TotalManCount);
                Tests.AssertEqual(1, roster.TotalWounded);
                Tests.AssertTrue(roster.VersionNo > version);
                Tests.AssertEqual(1, roster.GetTroopRoster().Count);
                Tests.AssertEqual(0, StubIntegrityBehavior.CanonicalizeRoster(roster, _ => canonical, out removed));
                Tests.AssertEqual(0, removed, "Reloading a repaired roster does not repeat removals.");
            }
        }

        [GameTest("RosterRepairPreservesOrdinaryTroopVariantsAndProgress", "save-contracts", RequiresCampaign = false)]
        public static void RosterRepairPreservesOrdinaryTroopVariantsAndProgress()
        {
            foreach (string scenario in new[] { "soldiers", "wounded", "xp", "initialized", "registered",
                "canonical", "missing", "wrong-id", "unregistered-target", "uninitialized-target", "hero" })
            {
                var canonical = RegisteredTroop("ordinary_repair_probe");
                var source = Troop(canonical.StringId);
                int men = scenario == "soldiers" ? 3 : 0;
                int wounded = scenario == "wounded" ? 1 : 0;
                int xp = scenario == "xp" ? 123 : 0;
                if (scenario == "initialized") source.Initialize();
                if (scenario == "registered") AccessTools.Property(typeof(MBObjectBase), "IsRegistered").SetValue(source, true);
                if (scenario == "canonical") source = canonical;
                var roster = TroopRoster.CreateDummyTroopRoster();
                AddLoadedRow(roster, source, men, woundedCount: wounded, xpChange: xp);
                // Attach the hero after seeding: cache initialization reads campaign health
                // models in newer engines, which are deliberately absent in this fixture.
                if (scenario == "hero") AccessTools.Property(typeof(CharacterObject), "HeroObject").SetValue(source,
                    (Hero)FormatterServices.GetUninitializedObject(typeof(Hero)));
                if (scenario == "missing") canonical = null;
                if (scenario == "wrong-id") canonical.StringId = "different_id";
                if (scenario == "unregistered-target") AccessTools.Property(typeof(MBObjectBase), "IsRegistered").SetValue(canonical, false);
                if (scenario == "uninitialized-target") AccessTools.Property(typeof(MBObjectBase), "IsInitialized").SetValue(canonical, false);
                int version = roster.VersionNo;
                Tests.AssertEqual(0, StubIntegrityBehavior.CanonicalizeRoster(roster, _ => canonical, out int removed), scenario);
                Tests.AssertEqual(0, removed, scenario);
                Tests.AssertEqual(1, roster.Count, scenario);
                Tests.AssertTrue(ReferenceEquals(source, roster.GetCharacterAtIndex(0)), scenario);
                Tests.AssertEqual(men, roster.GetElementNumber(0), scenario);
                if (scenario != "hero") Tests.AssertEqual(wounded, roster.GetElementWoundedNumber(0), scenario);
                Tests.AssertEqual(xp, roster.GetElementXp(0), scenario);
                Tests.AssertEqual(version, roster.VersionNo, scenario);
            }
        }

        [GameTest("RosterRemovalFailureLeavesSavedRowsUntouched", "save-contracts", RequiresCampaign = false)]
        public static void RosterRemovalFailureLeavesSavedRowsUntouched()
        {
            var canonical = RegisteredTroop("ordinary_repair_failure");
            var roster = TroopRoster.CreateDummyTroopRoster();
            var first = Troop(canonical.StringId);
            var second = Troop(canonical.StringId);
            AddLoadedRow(roster, first, 0);
            AddLoadedRow(roster, second, 0);
            int calls = 0, removed = -1, version = roster.VersionNo;
            try
            {
                Tests.AssertEqual(0, StubIntegrityBehavior.CanonicalizeRoster(roster, _ =>
                {
                    if (++calls == 2) throw new InvalidOperationException("Synthetic resolver failure");
                    return canonical;
                }, out removed));
            }
            catch (InvalidOperationException) { }
            Tests.AssertEqual(2, calls);
            Tests.AssertEqual(0, removed, "An aborted plan must not report removals in the log.");
            Tests.AssertEqual(2, roster.Count);
            Tests.AssertTrue(ReferenceEquals(first, roster.GetCharacterAtIndex(0)));
            Tests.AssertTrue(ReferenceEquals(second, roster.GetCharacterAtIndex(1)));
            Tests.AssertEqual(version, roster.VersionNo);
        }

        [GameTest("RosterRepairDoesNotOverflowSavedXp", "save-contracts", RequiresCampaign = false)]
        public static void RosterRepairDoesNotOverflowSavedXp()
        {
            var canonical = Troop("retinues_custom_overflow_roster");
            var twin = Troop(canonical.StringId);
            var roster = TroopRoster.CreateDummyTroopRoster();
            AddLoadedRow(roster, canonical, 2, xpChange: int.MaxValue);
            AddLoadedRow(roster, twin, 1, xpChange: 1);
            int version = roster.VersionNo;
            try
            {
                int repaired = StubIntegrityBehavior.CanonicalizeRoster(roster, _ => canonical);
                // In-game SafeClass finalizers may turn the rejected plan into a zero result.
                Tests.AssertEqual(0, repaired, "An overflowing merge cannot report a successful repair.");
            }
            catch (OverflowException) { }
            Tests.AssertEqual(2, roster.Count);
            Tests.AssertEqual(int.MaxValue, roster.GetElementXp(canonical));
            Tests.AssertEqual(1, roster.GetElementXp(twin));
            Tests.AssertEqual(3, roster.TotalManCount);
            Tests.AssertEqual(version, roster.VersionNo);
        }

        [GameTest("RosterRepairPreservesSavedValuesWithoutXpCallbacks", "save-contracts", RequiresCampaign = false)]
        public static void RosterRepairPreservesSavedValuesWithoutXpCallbacks()
        {
            foreach (bool merged in new[] { false, true })
            foreach (bool reversed in new[] { false, true })
            {
                var canonical = Troop("retinues_custom_save_audit");
                var twin = Troop(canonical.StringId);
                var other = Troop("ordinary_save_audit");
                var roster = TroopRoster.CreateDummyTroopRoster();
                AddLoadedRow(roster, other, 2, woundedCount: 1, xpChange: 77);
                if (merged) AddLoadedRow(roster, canonical, 5, woundedCount: 2, xpChange: 6000);
                AddLoadedRow(roster, twin, 3, woundedCount: 1, xpChange: 9000);
                if (reversed) roster.ShiftTroopToIndex(roster.FindIndexOfTroop(twin), 0);
                // Load-time skeletons cannot execute ordinary XP/party callbacks. Repair must
                // replace references without going through recruitment or changing saved XP.
                var party = (PartyBase)FormatterServices.GetUninitializedObject(typeof(PartyBase));
                AccessTools.Property(typeof(TroopRoster), "OwnerParty").SetValue(roster, party);
                int total = roster.TotalManCount, wounded = roster.TotalWounded;
                roster.GetTroopRoster(); // Populate the engine's versioned snapshot cache.
                int version = roster.VersionNo;
                Tests.AssertEqual(1, StubIntegrityBehavior.CanonicalizeRoster(roster, _ => canonical));
                Tests.AssertEqual(merged ? 8 : 3, roster.GetTroopCount(canonical));
                Tests.AssertEqual(merged ? 3 : 1, roster.GetElementWoundedNumber(roster.FindIndexOfTroop(canonical)));
                Tests.AssertEqual(merged ? 15000 : 9000, roster.GetElementXp(canonical));
                Tests.AssertEqual(77, roster.GetElementXp(other));
                Tests.AssertEqual(total, roster.TotalManCount);
                Tests.AssertEqual(wounded, roster.TotalWounded);
                Tests.AssertEqual(2, roster.Count);
                Tests.AssertFalse(roster.Contains(twin));
                Tests.AssertTrue(roster.VersionNo > version);
                foreach (var row in roster.GetTroopRoster()) Tests.AssertFalse(ReferenceEquals(twin, row.Character));
                Tests.AssertEqual(0, StubIntegrityBehavior.CanonicalizeRoster(roster, _ => canonical), "A second load/save repair is a no-op.");
            }
        }

        [GameTest("RosterRepairFailureLeavesAllSavedRowsUntouched", "save-contracts", RequiresCampaign = false)]
        public static void RosterRepairFailureLeavesAllSavedRowsUntouched()
        {
            var first = Troop("retinues_custom_save_first");
            var second = Troop("retinues_custom_save_second");
            var replacement = Troop(second.StringId);
            var roster = TroopRoster.CreateDummyTroopRoster();
            AddLoadedRow(roster, first, 2, woundedCount: 1, xpChange: 90);
            AddLoadedRow(roster, second, 3, woundedCount: 2, xpChange: 200);
            int version = roster.VersionNo;
            int calls = 0;
            try
            {
                int repaired = StubIntegrityBehavior.CanonicalizeRoster(roster, id =>
                {
                    if (++calls == 2) throw new InvalidOperationException("Synthetic resolver failure");
                    return replacement;
                });
                Tests.AssertEqual(0, repaired, "A rejected plan cannot report a successful repair.");
            }
            catch (InvalidOperationException) { }
            Tests.AssertEqual(2, calls, "The fixture reaches the failure after planning the first replacement.");
            Tests.AssertEqual(2, roster.Count);
            Tests.AssertTrue(ReferenceEquals(first, roster.GetCharacterAtIndex(0)));
            Tests.AssertTrue(ReferenceEquals(second, roster.GetCharacterAtIndex(1)));
            Tests.AssertEqual(90, roster.GetElementXp(first));
            Tests.AssertEqual(200, roster.GetElementXp(second));
            Tests.AssertEqual(5, roster.TotalManCount);
            Tests.AssertEqual(3, roster.TotalWounded);
            Tests.AssertEqual(version, roster.VersionNo);
        }
    }
}
