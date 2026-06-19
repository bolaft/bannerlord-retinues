namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Round-trip tests for the MAttribute persistence layer — the riskiest part of the rewrite.
    /// A wrapper's persisted attributes must survive serialize -> mutate -> deserialize.
    /// </summary>
    public static class PersistenceTests
    {
        [GameTest(
            "AttributeRoundTrip",
            "persistence",
            "Wrapper attributes survive serialize -> deserialize"
        )]
        public static void AttributeRoundTrip(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var wc = sandbox.NewStub();
            wc.Name = "RetinuesRoundTrip";
            wc.Level = 23;
            wc.SkillBaseline = 1234;
            wc.SourceStringId = "looter";

            var saved = wc.Serialize();
            Tests.AssertNotNull(saved, "Serialize produced data.");

            // Mutate the live attributes to prove the restore actually happens.
            wc.Name = "MUTATED";
            wc.Level = 1;
            wc.SkillBaseline = 0;
            wc.SourceStringId = "changed";

            wc.Deserialize(saved);

            Tests.AssertEqual("RetinuesRoundTrip", wc.Name, "Name round-trips.");
            Tests.AssertEqual(23, wc.Level, "Level round-trips.");
            Tests.AssertEqual(1234, wc.SkillBaseline, "SkillBaseline round-trips.");
            Tests.AssertEqual("looter", wc.SourceStringId, "SourceStringId round-trips.");
        }
    }
}
