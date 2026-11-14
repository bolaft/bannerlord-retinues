using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Editor.MVC.Pages.Library.Services;
using Retinues.Exports;
using Retinues.Migration;
using Retinues.Migration.Legacy;
using TaleWorlds.Core;
#if BL13 || BL14
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
#endif

namespace Retinues.Tests.Cases
{
    public static class StableExportTests
    {
        private const string SparseXml = """
            <RetinuesTroops xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Factions>
                <clanData><RootBasic><StringId>clan_basic</StringId><Name>Clan recruit</Name><Level>6</Level></RootBasic></clanData>
                <kingdomData><RetinueElite><StringId>kingdom_guard</StringId><Name>Kingdom guard</Name></RetinueElite></kingdomData>
              </Factions>
              <Cultures>
                <FactionSaveData><CaravanMaster><StringId>aserai_caravan_master</StringId><Name>Edited master</Name></CaravanMaster></FactionSaveData>
                <FactionSaveData />
              </Cultures>
            </RetinuesTroops>
            """;

        [GameTest("StableExportSeparatesFactionsAndSparseSlots", "import", RequiresCampaign = false)]
        public static void StableExportSeparatesFactionsAndSparseSlots()
        {
            var sections = StableExportParser.Read(XElement.Parse(SparseXml));
            Tests.AssertEqual(3, sections.Count, "Empty stable culture records are ignored.");
            Tests.AssertEqual("clan", sections[0].Key);
            Tests.AssertEqual("kingdom", sections[1].Key);
            var slot = StableExportParser.Roots(sections[2].Faction).Single();
            Tests.AssertEqual("CaravanMaster", slot.Role, "Sparse records keep their named role, not their position.");
            Tests.AssertTrue(sections[2].Faction.VeteranCaravanGuard == null);
            Tests.AssertEqual(FormationClass.Unset, sections[0].Faction.RootBasic.FormationClassOverride);
            Tests.AssertTrue(sections[0].Faction.RootBasic.EquipmentData == null, "Older exports may omit optional fields.");
            Tests.AssertThrows<InvalidDataException>(() => StableExportParser.Read(XElement.Parse("<Retinues v='1' kind='faction'/>")));
        }

        [GameTest("StableExportPreservesEquipmentFlagsAndAppearance", "import", RequiresCampaign = false)]
        public static void StableExportPreservesEquipmentFlagsAndAppearance()
        {
            var root = XElement.Parse("""
                <RetinuesTroops><Factions><clanData><RetinueBasic>
                  <StringId>basic</StringId><VanillaStringId>aserai_recruit</VanillaStringId><Name>Guard</Name>
                  <Level>26</Level><IsFemale>true</IsFemale><CultureId>aserai</CultureId><Race>0</Race>
                  <FormationClassOverride>HorseArcher</FormationClassOverride><IsMariner>true</IsMariner>
                  <EquipmentData><Codes><string>+0-bow-@null</string><string>+0-@null-@null</string><string>+0-bow-@null</string></Codes><Civilians><boolean>false</boolean><boolean>false</boolean><boolean>true</boolean></Civilians></EquipmentData>
                  <SkillData><Code>Bow:200;Riding:0</Code></SkillData><SkillBaseline>200</SkillBaseline>
                  <BodyData><AgeMin>22</AgeMin><AgeMax>40</AgeMax><HeightMin>1.1</HeightMin><HeightMax>1.2</HeightMax></BodyData>
                </RetinueBasic></clanData></Factions></RetinuesTroops>
                """);
            var troop = StableExportParser.Read(root)[0].Faction.RetinueBasic;
            Tests.AssertEqual(3, troop.EquipmentData.Codes.Count);
            Tests.AssertFalse(troop.EquipmentData.Civilians[1], "Explicit flags override the old second-set civilian default.");
            Tests.AssertTrue(troop.EquipmentData.Civilians[2]);
            Tests.AssertEqual("Bow:200;Riding:0", troop.SkillData.Code);
            Tests.AssertEqual(200, troop.SkillBaseline);
            Tests.AssertEqual(1.1f, troop.BodyData.HeightMin);
            Tests.AssertEqual(FormationClass.HorseArcher, troop.FormationClassOverride);
            Tests.AssertTrue(troop.IsMariner && troop.IsFemale);
        }

        [GameTest("StableExportValidatesGraphsAndRepeatedSubtrees", "import", RequiresCampaign = false)]
        public static void StableExportValidatesGraphsAndRepeatedSubtrees()
        {
            var shared1 = new TroopSaveData { StringId = "c", Name = "Shared" };
            var shared2 = new TroopSaveData { StringId = "c", Name = "Shared" };
            var a = new TroopSaveData { StringId = "a", UpgradeTargets = [shared1] };
            var b = new TroopSaveData { StringId = "b", UpgradeTargets = [shared2] };
            var root = new TroopSaveData { StringId = "r", UpgradeTargets = [a, b] };
            var faction = new FactionSaveData { RootBasic = root };
            Tests.AssertEqual(4, StableExportParser.Index(faction).Count, "Repeated stable subtrees represent one troop.");
            shared2.Name = "Conflicting";
            Tests.AssertThrows<InvalidDataException>(() => StableExportParser.Index(faction));
            shared2.Name = "Shared";
            shared1.UpgradeTargets = [new TroopSaveData { StringId = "r" }];
            Tests.AssertThrows<InvalidDataException>(() => StableExportParser.Index(faction), "Cycles by identity must be rejected before mutation.");
            shared1.UpgradeTargets = [null];
            Tests.AssertThrows<InvalidDataException>(() => StableExportParser.Index(faction), "Null edges must be rejected before placement.");
        }

        [GameTest("StableImportChecksMissingEquipmentBeforeApplying", "import", RequiresCampaign = false)]
        public static void StableImportChecksMissingEquipmentBeforeApplying()
        {
            StableFactionImporter.ValidateEquipmentCode("+0-bow-@null+1-@null-@null", id => id == "bow", _ => false);
            Tests.AssertThrows<InvalidDataException>(() => StableFactionImporter.ValidateEquipmentCode("+0-missing-@null", _ => false, _ => true));
            Tests.AssertThrows<InvalidDataException>(() => StableFactionImporter.ValidateEquipmentCode("+0-bow-missing_modifier", _ => true, _ => false));
            Tests.AssertThrows<InvalidDataException>(() => StableFactionImporter.ValidateEquipmentCode("+999-bow-@null", _ => true, _ => true));
            Tests.AssertThrows<InvalidDataException>(() => StableFactionImporter.ValidateEquipmentCode("not equipment", _ => true, _ => true));
        }

        [GameTest("StableLibraryListsAndPreviewsWithoutCampaign", "import", RequiresCampaign = false)]
        public static void StableLibraryListsAndPreviewsWithoutCampaign()
        {
            var dir = Environment.GetEnvironmentVariable("RETINUES_TEST_DIRECTORY") ?? Path.GetTempPath();
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "stable-export-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                File.WriteAllText(path, SparseXml);
                var args = new object[] { path, null };
                Tests.AssertTrue((bool)typeof(ExportLibrary).GetMethod("TryRead", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args));
                var entry = (ExportLibrary.Entry)args[1];
                Tests.AssertTrue(entry.IsStableExport);
                Tests.AssertEqual(ExportKind.Faction, entry.Kind);
                Tests.AssertEqual(3, entry.TroopCount);
                Tests.AssertTrue(ExportXMLReader.TryReadTroopNames(entry, out var names));
                Tests.AssertTrue(names.Contains("Edited master"));
                Tests.AssertEqual(SparseXml, File.ReadAllText(path), "Discovery leaves the source export intact.");
                File.WriteAllText(path, "<!DOCTYPE RetinuesTroops [<!ENTITY x SYSTEM 'file:///should-not-read'>]><RetinuesTroops>&x;</RetinuesTroops>");
                Tests.AssertFalse(StableExportParser.TryRead(path, out _, out _), "DTD input is rejected.");
            }
            finally { File.Delete(path); }
        }

        [GameTest("StableImportSharesSaveConversionAndPersists", "import")]
        public static void StableImportSharesSaveConversionAndPersists(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            var target = sandbox.NewStub();
            var captain = sandbox.NewStub();
            target.Captain = captain;
            captain.IsCaptain = true;
            captain.CaptainBase = target;
            target.SkillPoints = 31;
            target.Skills[DefaultSkills.Riding] = 150;
            var troop = new TroopSaveData
            {
                StringId = "old_campaign_stub", VanillaStringId = "looter", Name = "Imported stable guard",
                Level = 21, Race = 0, FormationClassOverride = FormationClass.Ranged,
                SkillData = new TroopSkillData { Code = "OneHanded:111;Riding:0" },
                SkillBaseline = 500, IsMariner = true,
                Captain = new TroopSaveData { StringId = "old_captain", IsCaptain = true,
                    SkillBaseline = 999, FormationClassOverride = FormationClass.Infantry,
                    VanillaStringId = "imperial_recruit" }, CaptainEnabled = true,
                EquipmentData = new TroopEquipmentData
                {
                    Codes = [new Equipment().CalculateEquipmentCode(), new Equipment().CalculateEquipmentCode()],
                    Civilians = [true, false],
                },
            };
            var section = new StableExportSection { Key = "clan", Faction = new FactionSaveData { RootBasic = troop } };
            Tests.AssertTrue(FactionImporter.TryApplyFactionExport(new Destination { RootBasic = target },
                new FactionExportData { StableSection = section }, out var report, out var error), error);
            Tests.AssertEqual(2, report.ImportedTroops);
            Tests.AssertEqual(0, report.SkippedTroops);
            Tests.AssertEqual(0, target.Skills[DefaultSkills.Riding], "Explicit zero skills are not inherited from the target.");
            Tests.AssertEqual(captain.StringId, target.Captain.StringId, "Captain IDs are remapped to the destination campaign.");
            Tests.AssertEqual(target.StringId, captain.CaptainBase.StringId);
            Tests.AssertEqual(500, target.SkillBaseline, "An existing captain cannot overwrite the imported base budget.");
            Tests.AssertEqual(FormationClass.Ranged, target.FormationClassOverride);
            if (Retinues.Compatibility.Mods.NavalDLC.IsLoaded)
                Tests.AssertTrue(target.IsMariner);
            Tests.AssertEqual("looter", target.SourceStringId);
            Tests.AssertEqual(31, target.SkillPoints, "Importing a definition preserves the campaign's unspent points.");
            Tests.AssertTrue(target.EquipmentRoster.Equipments[0].IsCivilian);
            Tests.AssertFalse(target.EquipmentRoster.Equipments[1].IsCivilian);
            var persisted = target.Serialize();
            target.Name = "wiped";
            target.Skills[DefaultSkills.OneHanded] = 0;
            target.Deserialize(persisted);
            Tests.AssertEqual(troop.Name, target.Name);
            Tests.AssertEqual(111, target.Skills[DefaultSkills.OneHanded]);
            Tests.AssertEqual(500, target.SkillBaseline);

            // Compare the actual save-migration route, not a second hand-written conversion.
            var migrated = sandbox.NewStub();
            migrated.Skills[DefaultSkills.Riding] = 150;
            troop.StringId = migrated.StringId;
            var coordinator = new LegacyMigrationCoordinator(
                new Migration.Shims.FactionBehavior { ClanTroops = new FactionSaveData { Civilians = [troop] } },
                new Migration.Shims.TroopXpBehavior(), new Migration.Shims.TroopStatisticsBehavior(),
                new Migration.Shims.StocksBehavior(), new Migration.Shims.UnlocksBehavior(),
                new Migration.Shims.DoctrineServiceBehavior(), new Migration.Shims.AutoJoinBehavior(),
                new Migration.Shims.CombatAgentBehavior(), new Migration.Shims.VersionBehavior());
            coordinator.RunMigration();
            Tests.AssertEqual(target.Name, migrated.Name);
            Tests.AssertEqual(target.Level, migrated.Level);
            Tests.AssertEqual(target.Skills[DefaultSkills.OneHanded], migrated.Skills[DefaultSkills.OneHanded]);
            Tests.AssertEqual(0, migrated.Skills[DefaultSkills.Riding]);
            Tests.AssertEqual(target.SkillBaseline, migrated.SkillBaseline);
            Tests.AssertEqual(target.FormationClassOverride, migrated.FormationClassOverride);
            Tests.AssertEqual(target.EquipmentRoster.Equipments[0].IsCivilian, migrated.EquipmentRoster.Equipments[0].IsCivilian);
        }

        // An isolated destination prevents the campaign integration test from changing the
        // player's clan or any native faction. Only TestSandbox stubs are ever assigned.
        internal sealed class Destination : IBaseFaction
        {
            public string StringId => "stable-import-test";
            public string Name => "Test faction";
            public string Serialize() => string.Empty;
            public string SerializeAll() => string.Empty;
            public string Deserialize(string xml) => null;
            public uint Color => 0;
            public uint Color2 => 0;
            public Banner Banner => null;
            public WHero Leader => null;
#if BL13 || BL14
            public BannerImageIdentifierVM Image => null;
#else
            public ImageIdentifierVM Image => null;
#endif
            public ImageIdentifier ImageIdentifier => null;
            public WCharacter RootBasic { get; set; }
            public WCharacter RootElite => null;
            public List<WCharacter> RosterBasic => RootBasic?.Tree ?? [];
            public List<WCharacter> RosterElite => [];
            public List<WCharacter> RosterHeroes => [];
            public WCharacter Villager => null;
            public WCharacter MeleeMilitiaTroop => null;
            public WCharacter MeleeEliteMilitiaTroop => null;
            public WCharacter RangedMilitiaTroop => null;
            public WCharacter RangedEliteMilitiaTroop => null;
            public WCharacter CaravanGuard => null;
            public WCharacter VeteranCaravanGuard => null;
            public WCharacter CaravanMaster => null;
            public WCharacter ArmedTrader => null;
            public List<WCharacter> RosterRetinues => [];
            public List<WCharacter> RosterMercenary => [];
            public List<WCharacter> RosterMilitia => [];
            public List<WCharacter> RosterCaravan => [];
            public List<WCharacter> RosterConvoy => [];
            public List<WCharacter> RosterVillager => [];
            public List<WCharacter> RosterBandit => [];
            public List<WCharacter> RosterCivilian => [];
            public IEnumerable<WCharacter> Troops => RosterBasic;
        }

        [GameTest("StableImportRemapsConvergentTreesAndCaptains", "import")]
        public static void StableImportRemapsConvergentTreesAndCaptains(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            var unrelated = sandbox.NewStub();
            unrelated.Name = "Do not edit";
            var shared = new TroopSaveData { StringId = unrelated.StringId, Name = "Shared imported troop" };
            Tests.AssertTrue(StableFactionImporter.ResolveSlot(new Destination { RootBasic = unrelated },
                nameof(FactionSaveData.Extras), shared) == null,
                "A reused stub number is not a native identity match, even inside the selected faction.");
            var root = new TroopSaveData { StringId = "source-root", UpgradeTargets = [
                new TroopSaveData { StringId = "a", UpgradeTargets = [shared] },
                new TroopSaveData { StringId = "b", UpgradeTargets = [shared] }],
                Captain = new TroopSaveData { StringId = "source-captain", IsCaptain = true } };
            var destination = sandbox.NewStub();
            var placed = new Dictionary<string, WCharacter> { [root.StringId] = destination };
            try
            {
                StableFactionImporter.PlaceTree(root, destination, placed,
                    new HashSet<string> { destination.StringId }, new HashSet<string>());
                Tests.AssertEqual(5, placed.Count);
                Tests.AssertEqual(destination.UpgradeTargets[0].UpgradeTargets[0].StringId,
                    destination.UpgradeTargets[1].UpgradeTargets[0].StringId);
                Tests.AssertTrue(placed[unrelated.StringId].StringId != unrelated.StringId);
                Tests.AssertEqual("Do not edit", unrelated.Name, "Source IDs never resolve arbitrary existing stubs.");
            }
            finally
            {
                foreach (var node in placed.Values.Where(t => t != destination)) sandbox.Track(node);
            }
        }
    }
}
