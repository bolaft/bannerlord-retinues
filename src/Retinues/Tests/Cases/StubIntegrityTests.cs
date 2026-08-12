using System.Runtime.Serialization;
using Retinues.Behaviors.Troops;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Rosters must never keep a non-canonical CharacterObject instance for a custom troop id.
    /// A save written while such an instance is referenced materializes a null-name skeleton twin
    /// on the next load, which crashes wage/food calculations and makes the save unloadable.
    /// </summary>
    public static class StubIntegrityTests
    {
        [GameTest(
            "RosterCanonicalizationHealsDuplicateInstance",
            "persistence",
            "A roster entry referencing a duplicate custom troop instance is merged onto the registered troop"
        )]
        public static void RosterCanonicalizationHealsDuplicateInstance(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var canonical = sandbox.NewStub();
            Tests.AssertNotNull(canonical, "Allocated a stub as the canonical troop.");

            // Forge the failure mode: a second CharacterObject instance carrying the same
            // StringId, exactly like the skeleton the save system materializes from a by-value
            // record (uninitialized, only the id set).
            var twin = (CharacterObject)
                FormatterServices.GetUninitializedObject(typeof(CharacterObject));
            twin.StringId = canonical.StringId;

            var roster = TroopRoster.CreateDummyTroopRoster();
            roster.AddToCounts(canonical.Base, 5, insertAtFront: false, woundedCount: 2);
            roster.AddToCounts(twin, 1, insertAtFront: false, woundedCount: 1);

            Tests.AssertEqual(2, roster.Count, "The twin occupies its own roster row.");

            var healed = StubIntegrityBehavior.CanonicalizeRoster(roster);

            Tests.AssertEqual(1, healed, "Exactly one entry was repaired.");
            Tests.AssertEqual(1, roster.Count, "The twin row was merged away.");
            Tests.AssertTrue(
                roster.FindIndexOfTroop(twin) < 0,
                "No roster row references the twin instance anymore."
            );

            var element = roster.GetElementCopyAtIndex(0);
            Tests.AssertTrue(
                ReferenceEquals(element.Character, canonical.Base),
                "The surviving row references the registered instance."
            );
            Tests.AssertEqual(6, element.Number, "Troop count was preserved (5 + 1).");
            Tests.AssertEqual(3, element.WoundedNumber, "Wounded count was preserved (2 + 1).");
        }
    }
}
