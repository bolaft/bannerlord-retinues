using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
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

        [GameTest(
            "ConvergentTreeImportSharesNode",
            "import",
            "A troop reachable from two upgrade paths imports once and is linked from both parents"
        )]
        public static void ConvergentTreeImportSharesNode(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            // Source structure: R -> [A, B]; A -> [C]; B -> [C]. A DAG like the War Sails
            // marine lines, where each marine is the upgrade target of both the previous
            // marine and a same-tier land troop. The mirror import used to clone a second
            // child for B's slot and never re-skin it (the source id was already visited via
            // A), leaving a bare copy of B where the shared troop belonged.
            var srcR = sandbox.NewStub();
            var srcA = sandbox.NewStub();
            var srcB = sandbox.NewStub();
            var srcC = sandbox.NewStub();
            srcR.Name = "SrcRoot";
            srcA.Name = "SrcPathA";
            srcB.Name = "SrcPathB";
            srcC.Name = "SrcShared";
            srcR.UpgradeTargets = [srcA, srcB];
            srcA.UpgradeTargets = [srcC];
            srcB.UpgradeTargets = [srcC];

            static CharacterExportEntry Entry(WCharacter w) =>
                new() { SourceId = w.StringId, PayloadXml = w.SerializeAll() };

            var rootEntry = Entry(srcR);
            var srcById = new Dictionary<string, CharacterExportEntry>(StringComparer.Ordinal)
            {
                [srcR.StringId] = rootEntry,
                [srcA.StringId] = Entry(srcA),
                [srcB.StringId] = Entry(srcB),
                [srcC.StringId] = Entry(srcC),
            };
            var childrenById = new Dictionary<string, List<string>>(StringComparer.Ordinal)
            {
                [srcR.StringId] = [srcA.StringId, srcB.StringId],
                [srcA.StringId] = [srcC.StringId],
                [srcB.StringId] = [srcC.StringId],
                [srcC.StringId] = [],
            };

            var dstRoot = sandbox.NewStub();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            FactionImporter.MirrorTree(rootEntry, dstRoot, srcById, childrenById, visited);

            // Track mirror-created nodes for release.
            foreach (var node in dstRoot.Tree)
                if (node != null && node.StringId != dstRoot.StringId)
                    sandbox.Track(node);

            var kids = dstRoot.UpgradeTargets;
            Tests.AssertEqual(2, kids.Count, "Root imported both branches.");

            var aTargets = kids[0].UpgradeTargets;
            var bTargets = kids[1].UpgradeTargets;
            Tests.AssertEqual(1, aTargets.Count, "First branch has its upgrade.");
            Tests.AssertEqual(1, bTargets.Count, "Second branch has its upgrade.");
            Tests.AssertEqual(
                aTargets[0].StringId,
                bTargets[0].StringId,
                "Both branches link to the same imported troop."
            );
            Tests.AssertEqual(
                "SrcShared",
                aTargets[0].Name,
                "The shared troop is re-skinned from its payload, not left a copy of a parent."
            );
            Tests.AssertEqual(
                4,
                dstRoot.Tree.Count,
                "No orphan duplicate was created for the second path."
            );
        }
    }
}
