using Retinues.Exports;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Regression tests for export -> import persistence. An imported troop's attributes must stay
    /// dirty so they are written to the next save. A generic import runs MBase.Deserialize outside
    /// the persistence path, and a per-generic restore flag once let that path mark the imported
    /// attributes clean, so imported troops reloaded blank.
    /// </summary>
    public static class ImportTests
    {
        [GameTest(
            "ImportedTroopSurvivesSave",
            "persistence",
            "An imported troop's data stays dirty and survives a save/reload round-trip"
        )]
        public static void ImportedTroopSurvivesSave(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            // Build a source troop with distinctive data, then export it.
            var src = sandbox.NewStub();
            src.Name = "ImportRoundTripSource";
            src.Level = 21;
            src.Skills[DefaultSkills.OneHanded] = 123;

            var entry = new CharacterExportEntry
            {
                SourceId = "test-source",
                PayloadXml = src.SerializeAll(),
            };

            // Import onto a fresh target (the reporter's export-then-import case).
            var target = sandbox.NewStub();
            bool ok = CharacterImporter.TryApplyCharacterExport(target, entry, out var err);
            Tests.AssertTrue(ok, $"Import applied successfully ({err}).");
            Tests.AssertEqual(
                "ImportRoundTripSource",
                target.Name,
                "Import applied the name to the live troop."
            );

            // A save writes only DIRTY attributes. If the import left them clean, this diff would
            // omit the imported data and the troop would reload blank. Prove they persist by wiping
            // the live values and restoring the diff.
            var diff = target.Serialize();

            target.Name = "WIPED";
            target.Skills[DefaultSkills.OneHanded] = 0;
            target.Deserialize(diff);

            Tests.AssertEqual(
                "ImportRoundTripSource",
                target.Name,
                "Imported name is saved (dirty) and restored on reload."
            );
            Tests.AssertEqual(
                123,
                target.Skills[DefaultSkills.OneHanded],
                "Imported skill is saved and restored on reload."
            );
        }
    }
}
