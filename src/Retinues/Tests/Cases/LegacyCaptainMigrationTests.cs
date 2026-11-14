using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Model;
using Retinues.Exports;
using Retinues.Migration;
using Retinues.Migration.Legacy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class LegacyCaptainMigrationTests
    {
        [GameTest("LegacyCaptainConversionMustNotOverwriteBaseProgression", "migration", RequiresCampaign = false)]
        public static void CaptainConversion()
        {
            foreach (bool alreadyLinked in new[] { false, true })
            foreach (bool captainFirst in new[] { false, true })
            {
                using var fixture = new Fixture(alreadyLinked);
                var records = captainFirst
                    ? new[] { fixture.CaptainSave, fixture.ParentSave }
                    : new[] { fixture.ParentSave, fixture.CaptainSave };
                foreach (var record in records)
                {
                    var target = WCharacter.Get(record.StringId);
                    LegacyCharacterConverter.Apply(target, record);
                    target.SkillPoints = record.IsCaptain ? 99 : 17;
                    target.SkillPointsExperience = record.IsCaptain ? 22 : 33;
                }
                foreach (var record in records)
                    LegacyCharacterConverter.LinkCaptain(WCharacter.Get(record.StringId), record, WCharacter.Get);
                fixture.AssertParent(17);
                Tests.AssertEqual(33, fixture.Parent.SkillPointsExperience);
                Tests.AssertEqual(17, fixture.Captain.SkillPoints, "A linked captain shares its base troop's pool.");
                Tests.AssertEqual(33, fixture.Captain.SkillPointsExperience);

                var parentXml = fixture.Parent.Serialize();
                var captainXml = fixture.Captain.Serialize();
                fixture.Parent.SkillBaseline = 1;
                fixture.Parent.SkillPoints = 0;
                fixture.Parent.Captain = null;
                fixture.Captain.CaptainBase = null;
                fixture.Captain.Deserialize(captainXml);
                fixture.Parent.Deserialize(parentXml);
                fixture.AssertParent(17);
                Tests.AssertEqual(33, fixture.Captain.SkillPointsExperience, "Converted captain links and shared XP survive reload.");
            }
        }

        [GameTest("LegacySaveMigrationPreservesCaptainBaseXpAndDefinition", "migration", RequiresCampaign = false)]
        public static void SaveMigration()
        {
            foreach (bool alreadyLinked in new[] { false, true })
            foreach (bool captainFirst in new[] { false, true })
            {
                using var fixture = new Fixture(alreadyLinked);
                var coordinator = new LegacyMigrationCoordinator(
                    new Migration.Shims.FactionBehavior { ClanTroops = new FactionSaveData {
                        Civilians = captainFirst ? [fixture.CaptainSave, fixture.ParentSave] : [fixture.ParentSave],
                    } },
                    new Migration.Shims.TroopXpBehavior { XpPools = new Dictionary<string, int> {
                        [fixture.Parent.StringId] = 1045, // Ten points at the stable default cost.
                        [fixture.Captain.StringId] = 100000,
                    } },
                    new Migration.Shims.TroopStatisticsBehavior(), new Migration.Shims.StocksBehavior(),
                    new Migration.Shims.UnlocksBehavior(), new Migration.Shims.DoctrineServiceBehavior(),
                    new Migration.Shims.AutoJoinBehavior(), new Migration.Shims.CombatAgentBehavior(),
                    new Migration.Shims.VersionBehavior());
                // Exercise the actual coordinator without modifying any campaign faction.
                AccessTools.Method(typeof(LegacyMigrationCoordinator), "MigrateCharacterData").Invoke(coordinator, null);
                fixture.AssertParent(10);
                Tests.AssertEqual(10, fixture.Captain.SkillPoints);
                Tests.AssertEqual(16, fixture.Captain.Level, "The captain keeps its separate troop definition.");
                Tests.AssertEqual("Captain", fixture.Captain.Name);
            }
        }

        [GameTest("StableImportPreservesExistingCaptainBaseAndXp", "import", RequiresCampaign = false)]
        public static void StableImport()
        {
            // Replace only the native race-count call; execute the real import and wrappers.
            var harmony = new Harmony("Retinues.Tests.LegacyCaptainImport");
            try
            {
                harmony.Patch(AccessTools.Method(typeof(FaceGen), "GetRaceCount"),
                    prefix: new HarmonyMethod(typeof(LegacyCaptainMigrationTests), nameof(RaceCount)));
                using var fixture = new Fixture(alreadyLinked: true);
                fixture.Parent.SkillPoints = 17;
                var section = new StableExportSection { Key = "clan",
                    Faction = new FactionSaveData { RootBasic = fixture.ParentSave } };
                Tests.AssertTrue(StableFactionImporter.TryApply(
                    new StableExportTests.Destination { RootBasic = fixture.Parent }, section,
                    out var report, out var error), error);
                Tests.AssertEqual(2, report.ImportedTroops);
                Tests.AssertEqual(0, report.SkippedTroops);
                fixture.AssertParent(17);
                Tests.AssertEqual(17, fixture.Captain.SkillPoints);
            }
            finally { harmony.UnpatchAll(harmony.Id); }
        }

        private static bool RaceCount(ref int __result) { __result = 1; return false; }

        private sealed class Fixture : IDisposable
        {
            private readonly Europe1700RecruitmentTests.Fixture _engine = new();
            private readonly IDictionary _resolvers;
            private readonly IDictionary _cache;
            private readonly Hashtable _oldResolvers;
            private readonly Hashtable _oldCache;
            internal readonly WCharacter Parent;
            internal readonly WCharacter Captain;
            internal readonly TroopSaveData ParentSave;
            internal readonly TroopSaveData CaptainSave;

            internal Fixture(bool alreadyLinked)
            {
                var wrapperType = typeof(WBase<WCharacter, CharacterObject>);
                _resolvers = (IDictionary)AccessTools.Field(wrapperType, "Resolvers").GetValue(null);
                _cache = (IDictionary)AccessTools.Field(wrapperType, "Cache").GetValue(null);
                _oldResolvers = new Hashtable(_resolvers);
                _oldCache = new Hashtable(_cache);
                var parentObject = _engine.Troop("migration_parent", custom: true);
                var captainObject = _engine.Troop("migration_captain", custom: true);
                var objects = new Dictionary<string, CharacterObject> {
                    [parentObject.StringId] = parentObject, [captainObject.StringId] = captainObject,
                };
                WCharacter.RegisterResolver<CharacterObject>(id => objects.TryGetValue(id, out var troop) ? troop : null);
                Parent = WCharacter.Get(parentObject);
                Captain = WCharacter.Get(captainObject);
                if (alreadyLinked)
                {
                    Parent.Captain = Captain;
                    Captain.IsCaptain = true;
                    Captain.CaptainBase = Parent;
                }
                CaptainSave = new TroopSaveData {
                    StringId = Captain.StringId, Name = "Captain", Level = 16, Race = -1,
                    IsCaptain = true, SkillBaseline = 999, FormationClassOverride = FormationClass.Infantry,
                    VanillaStringId = "captain_source",
                };
                ParentSave = new TroopSaveData {
                    StringId = Parent.StringId, Name = "Parent", Level = 11, Race = -1,
                    SkillBaseline = 111, FormationClassOverride = FormationClass.Ranged,
                    IsMariner = true, Captain = CaptainSave, CaptainEnabled = true,
                    VanillaStringId = "parent_source",
                };
            }

            internal void AssertParent(int points)
            {
                Tests.AssertEqual(111, Parent.SkillBaseline, "A captain's legacy baseline must not overwrite its base troop's saved budget.");
                Tests.AssertEqual(points, Parent.SkillPoints);
                Tests.AssertEqual(FormationClass.Ranged, Parent.FormationClassOverride);
                Tests.AssertEqual("parent_source", Parent.SourceStringId);
                Tests.AssertEqual(Parent.StringId, Captain.CaptainBase?.StringId);
                Tests.AssertEqual(Captain.StringId, Parent.Captain?.StringId);
            }

            public void Dispose()
            {
                _resolvers.Clear(); foreach (DictionaryEntry p in _oldResolvers) _resolvers[p.Key] = p.Value;
                _cache.Clear(); foreach (DictionaryEntry p in _oldCache) _cache[p.Key] = p.Value;
                _engine.Dispose();
            }
        }
    }
}
