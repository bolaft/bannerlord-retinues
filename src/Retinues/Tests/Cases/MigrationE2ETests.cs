using Retinues.Domain.Characters.Wrappers;
using Retinues.Migration;
using TaleWorlds.Core;
using LegacyData = Retinues.Migration.Legacy;
using Shims = Retinues.Migration.Shims;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// End-to-end test of the v1 -> v2 migration coordinator: build a minimal legacy faction blob,
    /// run the migration, and assert the data landed on the v2 stub.
    ///
    /// The legacy troop is placed in the faction's Civilians slot (not a root), so MigrateFactionRoots
    /// has nothing to wire and the live player clan is left untouched — the test stays isolated.
    /// </summary>
    public static class MigrationE2ETests
    {
        [GameTest(
            "MigrateAppliesTroopData",
            "migration",
            "Coordinator applies legacy troop data (name/level/skill/source) onto the v2 stub"
        )]
        public static void MigrateAppliesTroopData(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var stub = sandbox.NewStub();
            Tests.AssertNotNull(stub, "Allocated a free stub to migrate onto.");
            var id = stub.StringId;

            var legacyTroop = new LegacyData.TroopSaveData
            {
                StringId = id,
                VanillaStringId = "looter",
                Name = "MigratedTroop",
                Level = 19,
                Race = 0,
                SkillData = new LegacyData.TroopSkillData { Code = "OneHanded:111" },
                SkillBaseline = 0,
            };

            // Civilians slot (not a root) keeps MigrateFactionRoots from touching the player clan.
            var faction = new LegacyData.FactionSaveData { Civilians = [legacyTroop] };

            var coordinator = new LegacyMigrationCoordinator(
                new Shims.FactionBehavior { ClanTroops = faction },
                new Shims.TroopXpBehavior(),
                new Shims.TroopStatisticsBehavior(),
                new Shims.StocksBehavior(),
                new Shims.UnlocksBehavior(),
                new Shims.DoctrineServiceBehavior(),
                new Shims.AutoJoinBehavior(),
                new Shims.CombatAgentBehavior(),
                new Shims.VersionBehavior()
            );

            coordinator.RunMigration();

            var wc = WCharacter.Get(id);
            Tests.AssertNotNull(wc, "Migrated stub resolves.");
            Tests.AssertEqual("MigratedTroop", wc.Name, "Name migrated.");
            Tests.AssertEqual(19, wc.Level, "Level migrated.");
            Tests.AssertEqual(111, wc.Skills[DefaultSkills.OneHanded], "OneHanded skill migrated.");
            Tests.AssertEqual(
                "looter",
                wc.SourceStringId,
                "Source id carried from the v1 VanillaStringId."
            );
        }
    }
}
