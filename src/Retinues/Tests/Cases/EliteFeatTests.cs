using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Behaviors.Doctrines;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Behaviors.Doctrines.Feats.Training;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests.Cases
{
    public static class EliteFeatTests
    {
        [GameTest("ElitePromotionFeatCreditsBatchesAndResumesSavedProgress", "doctrines", RequiresCampaign = false)]
        public static void ElitePromotions()
        {
            using var engine = new Europe1700RecruitmentTests.Fixture();
            using var enabled = TestConfig.Set(Configuration.EnableDoctrines, true);
            using var required = TestConfig.Set(Configuration.EnableFeatRequirements, true);
            DoctrinesRegistry.EnsureRegistered();
            var doctrines = DoctrinesRegistry.GetDoctrines().ToDictionary(d => d.Id, d => (d.IsAcquired, d.Progress));
            var feats = DoctrinesRegistry.GetFeats().ToDictionary(f => f.Id, f => f.Progress);
            var cache = (Dictionary<string, List<IBaseFaction>>)AccessTools.Field(typeof(FactionCache), "ByTroopId").GetValue(null);
            var oldCache = new Dictionary<string, List<IBaseFaction>>(cache);
            var built = AccessTools.Field(typeof(FactionCache), "_built");
            var oldBuilt = built.GetValue(null);
            try
            {
                // Install the real doctrine behavior in the isolated campaign. Its notifications
                // remain queued: no map ticks or native UI run in this fixture.
                var managerType = typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.CampaignBehaviorManager);
                var manager = FormatterServices.GetUninitializedObject(managerType);
                AccessTools.Field(managerType, "_campaignBehaviors").SetValue(manager,
                    new List<CampaignBehaviorBase> { new DoctrinesBehavior() });
                AccessTools.Field(typeof(Campaign), "_campaignBehaviorManager").SetValue(Campaign.Current, manager);
                var clan = (Clan)FormatterServices.GetUninitializedObject(typeof(Clan));
                clan.StringId = "elite_feat_fixture";
                var faction = new WClan(clan);
                cache.Clear(); built.SetValue(null, true);
                var source = engine.Wrap(engine.Troop("elite_t5", 26, custom: true), TroopSourceFlags.Elite);
                var target = engine.Wrap(engine.Troop("elite_t6", 31, custom: true), TroopSourceFlags.Elite);
                var basic = engine.Wrap(engine.Troop("basic", custom: true), TroopSourceFlags.Basic);
                foreach (var troop in new[] { source, target, basic }) cache[troop.StringId] = new List<IBaseFaction> { faction };
                var feat = Feat.Get(FeatCatalog.MA_DistinguishedService.Id);
                var doctrine = feat.Doctrine;
                doctrine.IsAcquired = false; doctrine.ForceSet(0); feat.ForceSet(0);
                for (var p = doctrine.Prerequisite; p != null; p = p.Prerequisite) p.IsAcquired = true;
                Tests.AssertTrue(feat.IsInProgress);
                var behavior = new Feat_MastersAtArms_DistinguishedService();
                var handler = AccessTools.Method(behavior.GetType(), "OnPlayerUpgradedTroops");
                var invoke = AccessTools.Method(typeof(BaseCampaignBehavior), "SafeInvoke", new[] { typeof(Action) });
                void Promote(Retinues.Domain.Characters.Wrappers.WCharacter from, int count) =>
                    invoke.Invoke(behavior, new object[] { (Action)(() => handler.Invoke(behavior, new object[] { from, target, count })) });
                Promote(source, 60);
                Tests.AssertEqual(60, feat.Progress);
                Promote(basic, 20);
                Tests.AssertEqual(60, feat.Progress, "Only elite-to-elite promotions count.");
                Promote(source, 40);
                Tests.AssertEqual(20, doctrine.Progress);
                Tests.AssertEqual(0, feat.Progress, "The repeatable feat resets after awarding its worth.");
                // This is also the path used when an older save restores a parked completion.
                feat.ForceSet(100);
                Promote(source, 5);
                Tests.AssertEqual(40, doctrine.Progress);
                Tests.AssertEqual(5, feat.Progress);
                doctrine.Prerequisite.IsAcquired = false;
                Promote(source, 5);
                Tests.AssertEqual(5, feat.Progress, "Locked doctrines do not receive progression events.");
            }
            finally
            {
                cache.Clear(); foreach (var entry in oldCache) cache.Add(entry.Key, entry.Value);
                built.SetValue(null, oldBuilt);
                foreach (var d in DoctrinesRegistry.GetDoctrines()) { d.IsAcquired = doctrines[d.Id].IsAcquired; d.ForceSet(doctrines[d.Id].Progress); }
                foreach (var f in DoctrinesRegistry.GetFeats()) f.ForceSet(feats[f.Id]);
            }
        }
    }
}
