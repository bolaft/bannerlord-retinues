using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Safety.Uninstall;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace Retinues.Tests.Cases
{
    public static class UninstallTests
    {
        private sealed class SavedReferences
        {
            [SaveableField(1)] public CharacterObject[] Volunteers;
            [SaveableField(2)] public TroopRoster SettlementPrisoners;
            [SaveableField(3)] public List<SavedReferences> Children;
            [SaveableField(4)] public Dictionary<CharacterObject, FormationClass> Preferences;
            [SaveableProperty(5)] public CharacterObject QuestTroop { get; private set; }
            public SavedReferences(CharacterObject troop) { Volunteers = new[] { troop, null }; QuestTroop = troop; }
        }

        private static CharacterObject Troop(string id)
        {
            var troop = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
            troop.StringId = id;
            return troop;
        }

        private static TroopRoster Roster(params TroopRosterElement[] rows)
        {
            var roster = TroopRoster.CreateDummyTroopRoster();
            AccessTools.Field(typeof(TroopRoster), "data").SetValue(roster, rows);
            AccessTools.Field(typeof(TroopRoster), "_count").SetValue(roster, rows.Length);
            AccessTools.Method(typeof(TroopRoster), "InitializeCachedData").Invoke(roster, null);
            return roster;
        }

        [GameTest("UninstallCleansVolunteersQuestsAndSettlementRosters", "uninstall", RequiresCampaign = false)]
        public static void UninstallCleansVolunteersQuestsAndSettlementRosters()
        {
            var custom = Troop("retinues_custom_uninstall_test");
            var native = Troop("other_mod_soldier");
            var legacy = Troop("ret_old_guard");
            var root = new SavedReferences(custom)
            {
                SettlementPrisoners = Roster(new TroopRosterElement(legacy) { Number = 3, WoundedNumber = 1, Xp = 91 }),
                Preferences = new Dictionary<CharacterObject, FormationClass>
                    { [custom] = FormationClass.Ranged, [native] = FormationClass.Infantry }
            };
            root.Children = new List<SavedReferences> { root }; // Cyclic saved graphs terminate.
            var plan = new UninstallReferencePlan(_ => native, false);
            plan.Visit(root, "CampaignFixture");
            Tests.AssertTrue(ReferenceEquals(custom, root.Volunteers[0]), "Preflight makes no changes.");
            plan.Apply();
            Tests.AssertTrue(ReferenceEquals(native, root.Volunteers[0]));
            Tests.AssertTrue(root.Volunteers[1] == null, "Empty recruitment slots remain empty.");
            Tests.AssertTrue(ReferenceEquals(native, root.QuestTroop));
            Tests.AssertTrue(ReferenceEquals(native, root.SettlementPrisoners.GetCharacterAtIndex(0)));
            Tests.AssertEqual(3, root.SettlementPrisoners.TotalManCount);
            Tests.AssertEqual(1, root.SettlementPrisoners.TotalWounded);
            Tests.AssertEqual(91, root.SettlementPrisoners.GetElementXp(0));
            Tests.AssertEqual(1, root.Preferences.Count);
            Tests.AssertEqual(FormationClass.Infantry, root.Preferences[native]);
            var verify = new UninstallReferencePlan(_ => native, false);
            verify.Visit(root, "CampaignFixture");
            Tests.AssertEqual(0, verify.Changes, "A second scan proves the references are gone.");
        }

        [GameTest("UninstallRosterMergeAndRemovalPreserveNativeRows", "uninstall", RequiresCampaign = false)]
        public static void UninstallRosterMergeAndRemovalPreserveNativeRows()
        {
            foreach (bool remove in new[] { false, true })
            {
                var native = Troop("native_guard");
                var roster = Roster(
                    new TroopRosterElement(native) { Number = 2, WoundedNumber = 1, Xp = 70 },
                    new TroopRosterElement(Troop("retinues_custom_inactive")) { Number = 3, WoundedNumber = 2, Xp = 90 },
                    new TroopRosterElement(Troop("ret_old_inactive")) { Number = 1, Xp = 10 });
                // Ordinary XP callbacks cannot run against this skeleton party.
                AccessTools.Property(typeof(TroopRoster), "OwnerParty").SetValue(roster,
                    FormatterServices.GetUninitializedObject(typeof(PartyBase)));
                var plan = new UninstallReferencePlan(_ => native, remove);
                plan.Visit(roster, "Roster");
                plan.Apply();
                Tests.AssertEqual(1, roster.Count);
                Tests.AssertEqual(remove ? 2 : 6, roster.TotalManCount);
                Tests.AssertEqual(remove ? 1 : 3, roster.TotalWounded);
                Tests.AssertEqual(remove ? 70 : 170, roster.GetElementXp(0));
            }
        }

        [GameTest("UninstallFailsBeforeChangingUnsupportedOrConflictingReferences", "uninstall", RequiresCampaign = false)]
        public static void UninstallFailsBeforeChangingUnsupportedOrConflictingReferences()
        {
            var custom = Troop("retinues_custom_uninstall_test");
            var native = Troop("native_guard");
            var missing = new UninstallReferencePlan(_ => null, false);
            bool rejected = false;
            try { missing.Visit(new[] { custom }, "Volunteers"); } catch (InvalidOperationException) { rejected = true; }
            Tests.AssertTrue(rejected, "Missing replacements are reported, not silently discarded.");

            var counts = new Dictionary<CharacterObject, int> { [custom] = 4, [native] = 8 };
            rejected = false;
            try { new UninstallReferencePlan(_ => native, false).Visit(counts, "QuestCounts"); }
            catch (InvalidOperationException) { rejected = true; }
            Tests.AssertTrue(rejected, "Unknown dictionary merge semantics cannot destroy quest progress.");
            Tests.AssertEqual(4, counts[custom]);
            Tests.AssertEqual(8, counts[native]);

            var roster = Roster(new TroopRosterElement(custom) { Number = 1, Xp = int.MaxValue },
                new TroopRosterElement(native) { Number = 1, Xp = 1 });
            rejected = false;
            try { new UninstallReferencePlan(_ => native, false).Visit(roster, "Roster"); }
            catch (OverflowException) { rejected = true; }
            Tests.AssertTrue(rejected);
            Tests.AssertEqual(2, roster.Count, "Overflow leaves the original roster untouched.");
        }

        [GameTest("UninstallCleansBehaviorFieldsTemplatesAndUpgradeLinks", "uninstall", RequiresCampaign = false)]
        public static void UninstallCleansBehaviorFieldsTemplatesAndUpgradeLinks()
        {
            var custom = Troop("retinues_custom_test");
            var native = Troop("native_soldier");
            var source = Troop("native_recruit");
            AccessTools.Property(typeof(CharacterObject), "UpgradeTargets").SetValue(source, new[] { native, custom });
            var templates = new List<PartyTemplateStack> { new PartyTemplateStack(custom, 2, 7) };
            var behavior = new BehaviorReferences { PendingRecruit = custom, Templates = templates };
            var plan = new UninstallReferencePlan(_ => native, false);
            plan.VisitBehavior(behavior);
            plan.PlanUpgradeTargets(source);
            plan.Apply();
            Tests.AssertTrue(ReferenceEquals(native, behavior.PendingRecruit));
            Tests.AssertTrue(ReferenceEquals(native, templates[0].Character));
            Tests.AssertEqual(2, templates[0].MinValue);
            Tests.AssertEqual(7, templates[0].MaxValue);
            Tests.AssertEqual(1, source.UpgradeTargets.Length);
            Tests.AssertTrue(ReferenceEquals(native, source.UpgradeTargets[0]));
        }

        private sealed class BehaviorReferences
        {
            public CharacterObject PendingRecruit;
            public List<PartyTemplateStack> Templates;
        }

        private struct SavedRow
        {
            [SaveableField(1)] public CharacterObject Troop;
            [SaveableField(2)] public int Progress;
        }

        [GameTest("UninstallCleansBoxedRowsTuplesAndRejectsReadOnlyTroopLists", "uninstall", RequiresCampaign = false)]
        public static void UninstallCleansBoxedRowsTuplesAndRejectsReadOnlyTroopLists()
        {
            var custom = Troop("retinues_custom_test");
            var native = Troop("native_guard");
            var rows = new List<SavedRow> { new SavedRow { Troop = custom, Progress = 19 } };
            var tuples = new[] { (custom, 7) };
            var referenceTuple = Tuple.Create(custom, 23);
            var plan = new UninstallReferencePlan(_ => native, false);
            plan.Visit(rows, "SavedRows");
            plan.Visit(tuples, "TupleRows");
            plan.Visit(referenceTuple, "ReferenceTuple");
            plan.Apply();
            Tests.AssertTrue(ReferenceEquals(native, rows[0].Troop));
            Tests.AssertEqual(19, rows[0].Progress);
            Tests.AssertTrue(ReferenceEquals(native, tuples[0].Item1));
            Tests.AssertEqual(7, tuples[0].Item2);
            Tests.AssertTrue(ReferenceEquals(native, referenceTuple.Item1));
            Tests.AssertEqual(23, referenceTuple.Item2);
            bool rejected = false;
            try { new UninstallReferencePlan(_ => native, false).Visit(Array.AsReadOnly(new[] { custom }), "ReadOnlyTroops"); }
            catch (InvalidOperationException) { rejected = true; }
            Tests.AssertTrue(rejected, "An unsupported collection cannot produce a false success.");
        }

        private sealed class PersistenceProbe : CampaignBehaviorBase
        {
            public int Calls;
            public override void RegisterEvents() { }
            public override void SyncData(IDataStore store) { Calls++; int marker = 71; store.SyncData("probe", ref marker); }
        }

        [GameTest("UninstallPreservesNativePrisonerRecords", "uninstall", RequiresCampaign = false)]
        public static void UninstallPreservesNativePrisonerRecords()
        {
            var custom = Troop("retinues_custom_prisoner");
            var native = Troop("native_prisoner");
            var behavior = new TaleWorlds.CampaignSystem.CampaignBehaviors.PrisonerRecruitCampaignBehavior();
            var field = AccessTools.Field(behavior.GetType(), "PrisonerTalkRecords");
            var records = new Dictionary<CharacterObject, float> { [custom] = 12f, [native] = 31f };
            field.SetValue(behavior, records);
            var plan = new UninstallReferencePlan(_ => native, false);
            plan.VisitBehavior(behavior);
            plan.Apply();
            Tests.AssertEqual(1, records.Count);
            Tests.AssertEqual(31f, records[native], "The replacement keeps its own prisoner conversation cooldown.");
        }

        [GameTest("UninstallGuardsBindAndExcludeModPersistenceOnlyAfterPreparation", "uninstall", RequiresCampaign = false)]
        public static void UninstallGuardsBindAndExcludeModPersistenceOnlyAfterPreparation()
        {
            if (Campaign.Current != null) Tests.Skip("Headless-only: uses an isolated campaign singleton.");
            var current = AccessTools.Property(typeof(Campaign), "Current");
            var previous = Campaign.Current;
            var harmony = new Harmony("Retinues.Tests.UninstallGuards");
            try
            {
                harmony.CreateClassProcessor(typeof(UninstallPersistenceGuard)).Patch();
                harmony.CreateClassProcessor(typeof(UninstallSaveGuard)).Patch();
                harmony.CreateClassProcessor(typeof(UninstallWriteGuard)).Patch();
                UninstallPersistenceGuard.EnsureInstalled();
                UninstallSaveGuard.EnsureInstalled();
                var prepared = new UninstallBehavior();
                var probe = new PersistenceProbe();
                var managerType = typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.CampaignBehaviorManager);
                var manager = FormatterServices.GetUninitializedObject(managerType);
                AccessTools.Field(managerType, "_campaignBehaviors").SetValue(manager, new List<CampaignBehaviorBase> { prepared, probe });
                var campaign = (Campaign)FormatterServices.GetUninitializedObject(typeof(Campaign));
                current.SetValue(null, campaign);
                Tests.AssertFalse(UninstallBehavior.IsPrepared, "Early campaign initialization has no behavior manager yet.");
                Tests.AssertFalse(UninstallBehavior.HasFailed);
                AccessTools.Field(typeof(Campaign), "_campaignBehaviorManager").SetValue(campaign, manager);
                var ordinarySave = new SaveHandler();
                ordinarySave.SaveAs("headless-queued-only");
                Tests.AssertTrue(ordinarySave.IsSaving, "Unprepared saves still enter the native save queue.");
                var storeType = typeof(Campaign).Assembly.GetType("TaleWorlds.CampaignSystem.CampaignBehaviorDataStore", true);
                var store = Activator.CreateInstance(storeType, true);
                var save = UninstallPersistenceGuard.TargetMethods().Single(m => m.Name == "SaveBehaviorData");
                var load = UninstallPersistenceGuard.TargetMethods().Single(m => m.Name == "LoadBehaviorData");
                save.Invoke(store, new object[] { probe });
                // The engine's readonly flag defaults to false when the save record is deserialized.
                var savedRecords = (IDictionary)AccessTools.Field(storeType, "_behaviorDict").GetValue(store);
                var savedProbe = savedRecords[probe.StringId];
                AccessTools.Field(savedProbe.GetType(), "_isSaving").SetValue(savedProbe, false);
                load.Invoke(store, new object[] { probe });
                Tests.AssertEqual(2, probe.Calls, "Normal saves keep existing persistence behavior.");
                AccessTools.Field(typeof(UninstallBehavior), "_prepared").SetValue(prepared, true);
                AccessTools.Method(storeType, "ClearBehaviorData").Invoke(store, null);
                save.Invoke(store, new object[] { probe });
                load.Invoke(store, new object[] { probe });
                Tests.AssertEqual(2, probe.Calls, "Prepared saves neither write nor restore mod payloads.");
                save.Invoke(store, new object[] { prepared });
                var records = (IDictionary)AccessTools.Field(storeType, "_behaviorDict").GetValue(store);
                Tests.AssertEqual(1, records.Count, "Only the primitive uninstall marker remains.");
                Tests.AssertTrue(records.Contains(prepared.StringId));
                var nativeType = typeof(Campaign).Assembly.GetType("TaleWorlds.CampaignSystem.CampaignBehaviors.MobilePartyTrainingBehavior", true);
                var native = (CampaignBehaviorBase)FormatterServices.GetUninitializedObject(nativeType);
                Tests.AssertTrue(UninstallPersistenceGuard.ShouldSync(native, true), "Native/other-module behaviors still save.");
                prepared.CleanupFailed = true;
                var blockedSave = new SaveHandler();
                blockedSave.SaveAs("headless-must-not-queue");
                Tests.AssertFalse(blockedSave.IsSaving, "Incomplete cleanup cannot overwrite a save.");
                var game = (TaleWorlds.Core.Game)FormatterServices.GetUninitializedObject(typeof(TaleWorlds.Core.Game));
                int completions = 0;
                game.Save(null, "headless-must-not-write", null, result =>
                {
                    completions++;
                    Tests.AssertEqual(TaleWorlds.Library.SaveResult.GeneralFailure, result);
                });
                Tests.AssertEqual(1, completions, "A blocked final write releases the engine's save queue.");
            }
            finally
            {
                current.SetValue(null, previous);
                harmony.UnpatchAll(harmony.Id);
            }
        }

        [GameTest("UninstallRejectsRetinuesPayloadsInFinalSaveSnapshot", "uninstall", RequiresCampaign = false)]
        public static void UninstallRejectsRetinuesPayloadsInFinalSaveSnapshot()
        {
            bool rejected = false;
            try
            {
                new UninstallReferencePlan(_ => null, false, includeBehaviorSnapshots: true)
                    .Visit(new Dictionary<string, object> { ["other_mod_record"] = new SavedReferences(null) }, "Snapshot");
            }
            catch (InvalidOperationException) { rejected = true; }
            Tests.AssertTrue(rejected, "References to Retinues-only saved types must not survive in other modules' records.");
        }

        private sealed class MemoryStore : IDataStore
        {
            public bool IsSaving { get; set; }
            public bool IsLoading => !IsSaving;
            public readonly Dictionary<string, object> Values = new();
            public bool SyncData<T>(string key, ref T data)
            {
                if (IsSaving) Values[key] = data;
                else if (Values.TryGetValue(key, out var value)) data = (T)value;
                else return false;
                return true;
            }
        }

        [GameTest("UninstallStatePersistsWithoutAffectingOlderSaves", "uninstall", RequiresCampaign = false)]
        public static void UninstallStatePersistsWithoutAffectingOlderSaves()
        {
            var field = typeof(UninstallBehavior).GetField("_prepared", BindingFlags.Instance | BindingFlags.NonPublic);
            var behavior = new UninstallBehavior();
            var store = new MemoryStore();
            behavior.SyncData(store);
            Tests.AssertFalse((bool)field.GetValue(behavior), "Older saves default to normal operation.");
            field.SetValue(behavior, true);
            store.IsSaving = true;
            behavior.SyncData(store);
            store.IsSaving = false;
            var loaded = new UninstallBehavior();
            loaded.SyncData(store);
            Tests.AssertTrue((bool)field.GetValue(loaded), "Prepared state survives reload.");
            Tests.AssertFalse((bool)field.GetValue(new UninstallBehavior()), "Preparation does not leak into another campaign.");
        }
    }
}
