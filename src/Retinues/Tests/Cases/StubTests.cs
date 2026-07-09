using Retinues.Domain;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Tests.Cases
{
    /// <summary>Tests for custom-stub allocation invariants.</summary>
    public static class StubTests
    {
        [GameTest(
            "FreeStubsAreDistinctAndActive",
            "stubs",
            "GetFreeStub returns distinct, active, custom stubs"
        )]
        public static void FreeStubsAreDistinctAndActive(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var a = sandbox.NewStub();
            var b = sandbox.NewStub();

            Tests.AssertNotNull(a, "Allocated a stub.");
            Tests.AssertNotNull(b, "Allocated a second stub.");
            Tests.AssertTrue(a.StringId != b.StringId, "Two allocations are distinct stubs.");
            Tests.AssertTrue(a.IsCustom, "Allocated stub is a custom troop.");
            Tests.AssertTrue(a.IsActiveStub, "Allocated stub is marked active.");
        }

        [GameTest(
            "InUseStubNotReusedWhenFlagLost",
            "stubs",
            "A stub still assigned to a faction roster is never handed out again, even if its active flag was lost"
        )]
        public static void InUseStubNotReusedWhenFlagLost(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var clan = Player.Clan;
            Tests.AssertNotNull(clan?.Base, "Player has a clan.");

            // Preserve whatever custom militia troop the clan currently has (usually none).
            var previous = clan.MeleeMilitiaTroop;

            var stub = sandbox.NewStub();

            try
            {
                // Assign the stub to a faction roster, then simulate the "lost active flag" that used
                // to let the faction importer clone over an in-use militia troop (cross-linking it
                // into the elite/basic tree).
                clan.SetMeleeMilitiaTroop(stub);
                stub.IsActiveStub = false;
                WCharacter.InvalidateTroopSourceCaches();

                // Precondition: the source-flag cache now recognises the stub as in-use militia.
                Tests.AssertTrue(
                    stub.SourceFlags != TroopSourceFlags.None,
                    "A stub assigned as clan militia is flagged as in-use."
                );

                // Allocate several stubs; none may be the in-use militia stub.
                for (int i = 0; i < 5; i++)
                {
                    var got = sandbox.NewStub();
                    Tests.AssertTrue(
                        got == null || got.StringId != stub.StringId,
                        "GetFreeStub never hands out a stub that is in use as a faction troop."
                    );
                }
            }
            finally
            {
                clan.SetMeleeMilitiaTroop(previous);
                WCharacter.InvalidateTroopSourceCaches();
            }
        }
    }
}
