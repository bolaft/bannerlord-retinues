using Retinues.Game.Wrappers;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for stub allocation invariants. These guard the core anti-corruption rule:
    /// a stub that holds live data must never be handed out again.
    /// </summary>
    public static class StubTests
    {
        /// <summary>
        /// AllocateStub must never return a stub id that is currently registered as active.
        /// </summary>
        [GameTest(
            "AllocateStubSkipsActive",
            "stubs",
            "AllocateStub never returns an id already in ActiveStubIds"
        )]
        public static void AllocateStubSkipsActive()
        {
            var stub = WCharacter.AllocateStub();
            var id = stub.StringId;

            bool added = false;
            try
            {
                if (!WCharacter.ActiveStubIds.Contains(id))
                {
                    WCharacter.ActiveStubIds.Add(id);
                    added = true;
                }

                var next = WCharacter.AllocateStub();
                Tests.AssertTrue(
                    next.StringId != id,
                    $"AllocateStub returned a claimed stub id '{id}'."
                );
            }
            finally
            {
                if (added)
                    WCharacter.ActiveStubIds.Remove(id);
            }
        }
    }
}

