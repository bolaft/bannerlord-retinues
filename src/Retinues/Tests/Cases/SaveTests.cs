using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using Retinues.Troops.Save;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Round-trip tests for troop serialization. These guard the save/load layer where the
    /// revolt-corruption bug lived: a serialized troop must deserialize back to identical state.
    /// </summary>
    public static class SaveTests
    {
        /// <summary>
        /// Serialize a troop, mutate the live object, then deserialize and assert the saved
        /// state is fully restored (name, level, skills, equipment). Also asserts that the
        /// faction-less deserialize path re-registers the stub as active (the fix-3 invariant).
        /// </summary>
        [GameTest(
            "TroopSaveRoundTrip",
            "save",
            "TroopSaveData serialize -> deserialize restores name, level, skills, equipment"
        )]
        public static void TroopSaveRoundTrip(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var vanilla = Player.Clan?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "Player culture has a basic root troop to clone from.");

            using var sandbox = new TestSandbox();

            // Throwaway custom troop on a free stub, not bound to any real faction root.
            var troop = sandbox.NewStub();
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: true, keepSkills: true);
            troop.Name = "RetinuesTestTroop";
            troop.Level = 17;

            // Capture expected state.
            var expectedName = troop.Name;
            var expectedLevel = troop.Level;
            var expectedSkills = troop.Skills.ToDictionary(kv => kv.Key, kv => kv.Value);
            var expectedCodes = troop.Loadout.Equipments.Select(e => e.Code).ToList();
            var stubId = troop.StringId;

            // Serialize, then mutate the live troop to prove the restore actually happens.
            var data = new TroopSaveData(troop);
            troop.Name = "MUTATED";
            troop.Level = 1;

            // Faction-less deserialize writes back onto the same stub id.
            var rebuilt = data.Deserialize();
            Tests.AssertNotNull(rebuilt, "Deserialize produced a troop.");

            Tests.AssertEqual(expectedName, rebuilt.Name, "Name round-trips.");
            Tests.AssertEqual(expectedLevel, rebuilt.Level, "Level round-trips.");

            foreach (var kv in expectedSkills)
            {
                Tests.AssertEqual(
                    kv.Value,
                    rebuilt.GetSkill(kv.Key),
                    $"Skill '{kv.Key.StringId}' round-trips."
                );
            }

            var rebuiltCodes = rebuilt.Loadout.Equipments.Select(e => e.Code).ToList();
            Tests.AssertEqual(
                string.Join("|", expectedCodes),
                string.Join("|", rebuiltCodes),
                "Equipment codes round-trip."
            );

            // Fix-3: the faction-less deserialize path must claim the stub.
            Tests.AssertTrue(
                WCharacter.ActiveStubIds.Contains(stubId),
                "Deserialize registered the stub as active (stub-recycling guard)."
            );
        }

        /// <summary>
        /// Build a whole faction tree, serialize it to FactionSaveData, then Apply it to a fresh
        /// faction and assert the trees are reconstructed faithfully (ids, names, tree sizes).
        /// This is the exact serialize/deserialize path the campaign save relies on.
        /// </summary>
        [GameTest(
            "FactionSaveDataRoundTrip",
            "save",
            "FactionSaveData serialize -> Apply rebuilds the faction's troop trees faithfully"
        )]
        public static void FactionSaveDataRoundTrip(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var source = sandbox.NewFaction();
            Tests.AssertNotNull(source, "A non-player faction with troop roots is available.");
            var target = sandbox.NewFaction(source);
            Tests.AssertNotNull(target, "A second, distinct non-player faction is available.");

            TroopBuilder.CreateTroops(source, isElite: true, copyWholeTree: true);
            TroopBuilder.CreateTroops(source, isElite: false, copyWholeTree: false);

            var eliteId = source.RootElite?.StringId;
            var eliteName = source.RootElite?.Name;
            var eliteCount = source.RootElite?.Tree.Count() ?? 0;
            var basicId = source.RootBasic?.StringId;

            Tests.AssertNotNull(eliteId, "Source elite root exists.");
            Tests.AssertNotNull(basicId, "Source basic root exists.");
            Tests.AssertTrue(eliteCount > 1, "Source elite tree has multiple troops.");

            var data = new FactionSaveData(source);

            data.Apply(target);

            Tests.AssertNotNull(target.RootElite, "Elite root rebuilt on the target faction.");
            Tests.AssertNotNull(target.RootBasic, "Basic root rebuilt on the target faction.");
            Tests.AssertEqual(eliteId, target.RootElite.StringId, "Elite root id preserved.");
            Tests.AssertEqual(eliteName, target.RootElite.Name, "Elite root name preserved.");
            Tests.AssertEqual(
                eliteCount,
                target.RootElite.Tree.Count(),
                "Elite tree size preserved."
            );
            Tests.AssertEqual(basicId, target.RootBasic.StringId, "Basic root id preserved.");
        }

        /// <summary>
        /// Gender (a CharacterObject-backed field) survives a TroopSaveData round-trip.
        /// </summary>
        [GameTest(
            "TroopGenderRoundTrip",
            "save",
            "Gender (IsFemale) survives a TroopSaveData round-trip"
        )]
        public static void TroopGenderRoundTrip(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var vanilla = sandbox.NewFaction()?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A vanilla template troop is available.");

            var troop = sandbox.NewStub();
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: false, keepSkills: false);
            troop.IsFemale = true;

            var data = new TroopSaveData(troop);
            troop.IsFemale = false; // mutate live

            data.Deserialize();
            Tests.AssertTrue(troop.IsFemale, "Gender restored from save data.");
        }

        /// <summary>
        /// Toggling NeedsPersistence on a vanilla troop updates the EditedVanillaRootIds set
        /// (how the mod decides which vanilla troops to keep persisting).
        /// </summary>
        [GameTest(
            "EditedVanillaNeedsPersistence",
            "save",
            "NeedsPersistence on a vanilla troop tracks EditedVanillaRootIds"
        )]
        public static void EditedVanillaNeedsPersistence(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var vanilla = sandbox.NewFaction()?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A vanilla template troop is available.");
            Tests.AssertTrue(vanilla.IsVanilla, "Template troop is vanilla.");

            var rootId = vanilla.Root.StringId;

            vanilla.NeedsPersistence = true;
            Tests.AssertTrue(vanilla.NeedsPersistence, "Vanilla troop is marked needs-persistence.");
            Tests.AssertTrue(
                WCharacter.EditedVanillaRootIds.Contains(rootId),
                "Root id added to the edited-vanilla set."
            );

            vanilla.NeedsPersistence = false;
            Tests.AssertFalse(vanilla.NeedsPersistence, "Vanilla troop is unmarked.");
            Tests.AssertFalse(
                WCharacter.EditedVanillaRootIds.Contains(rootId),
                "Root id removed from the edited-vanilla set."
            );
        }

        /// <summary>
        /// Exporting player troops writes a file that is recognized as a valid unified export.
        /// Export is non-mutating (it reads the player factions); the temp file is cleaned up.
        /// </summary>
        [GameTest(
            "ExportProducesValidUnifiedFile",
            "save",
            "Exporting player troops writes a file recognized as a valid unified export"
        )]
        public static void ExportProducesValidUnifiedFile(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            string path = null;
            try
            {
                path = TroopImportExport.ExportUnified(
                    "retinues_test_export",
                    includeCustom: true,
                    includeCultures: false
                );
                Tests.AssertNotNull(path, "Export returned a path.");
                Tests.AssertTrue(System.IO.File.Exists(path), "Export file was created.");

                var files = TroopImportExport.ListValidUnifiedFilesNewestFirst();
                bool recognized = files.Any(f =>
                    f.IndexOf("retinues_test_export", System.StringComparison.OrdinalIgnoreCase) >= 0
                );
                Tests.AssertTrue(
                    recognized,
                    "The exported file is recognized as a valid unified export."
                );
            }
            finally
            {
                try
                {
                    if (path != null && System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }
                catch
                {
                    // best-effort cleanup
                }
            }
        }

        /// <summary>
        /// A save record captured from an untouched vanilla troop must be recognised as redundant,
        /// so it is not re-applied and not re-marked as edited. This is what lets a vanilla tree
        /// that was flagged once drain back out of the save instead of sticking forever. A record
        /// that differs in any persisted field must still be treated as a real edit.
        /// </summary>
        [GameTest(
            "UneditedVanillaRecordIsRedundant",
            "save",
            "An unchanged vanilla troop record is detected as redundant, a changed one is not"
        )]
        public static void UneditedVanillaRecordIsRedundant(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var vanilla = sandbox.NewFaction()?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A vanilla template troop is available.");
            Tests.AssertTrue(vanilla.IsVanilla, "Template troop is vanilla.");

            // Captured straight from the live troop: nothing was edited.
            var pristine = new TroopSaveData(vanilla);
            Tests.AssertTrue(
                pristine.MatchesLiveTroop(vanilla),
                "A record captured from an untouched troop matches the live troop."
            );

            // Any difference in a persisted field must count as an edit, so real edits survive.
            var edited = new TroopSaveData(vanilla) { Name = vanilla.Name + " (edited)" };
            Tests.AssertFalse(
                edited.MatchesLiveTroop(vanilla),
                "A record with a changed name is not treated as redundant."
            );

            var levelled = new TroopSaveData(vanilla) { Level = vanilla.Level + 1 };
            Tests.AssertFalse(
                levelled.MatchesLiveTroop(vanilla),
                "A record with a changed level is not treated as redundant."
            );

            // A difference anywhere in the subtree must also count: the whole tree is persisted
            // from its root, so a deep edit has to keep the root marked.
            var child = new TroopSaveData(vanilla).UpgradeTargets.FirstOrDefault();
            if (child != null)
            {
                var deep = new TroopSaveData(vanilla);
                deep.UpgradeTargets[0].Name = child.Name + " (edited)";
                Tests.AssertFalse(
                    deep.MatchesLiveTroop(vanilla),
                    "A record with an edited upgrade target is not treated as redundant."
                );
            }
        }
    }
}
