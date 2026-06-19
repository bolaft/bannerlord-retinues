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
    }
}
