using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests.Cases
{
    public static class EliteFeatTests
    {
        private static DoctrineServiceBehavior _service;
        private static bool Service(ref DoctrineServiceBehavior __result) { __result = _service; return false; }

        [GameTest("EliteUpgradeFeatCountsOnlyEligibleTerminalPromotions", "doctrines", RequiresCampaign = false)]
        public static void EliteUpgrades()
        {
            using var engine = new Europe1700RecruitmentTests.Fixture();
            using var required = new OptionScope<bool>(Config.EnableFeatRequirements, true);
            using var branches = new OptionScope<int>(Config.MaxEliteUpgrades, 1);
            var harmony = new Harmony("Retinues.Tests.EliteFeat");
            var old = _service;
            try
            {
                _service = new DoctrineServiceBehavior();
                AccessTools.Method(typeof(DoctrineServiceBehavior), "BuildCatalogIfNeeded").Invoke(_service, null);
                harmony.Patch(AccessTools.PropertyGetter(typeof(DoctrineAPI), "Svc"),
                    prefix: new HarmonyMethod(typeof(EliteFeatTests), nameof(Service)));
                var dispatcher = new FeatServiceBehavior();
                var dispatch = AccessTools.Method(typeof(FeatServiceBehavior), "PlayerUpgradedTroops");
                var key = typeof(MastersAtArms.MAA_Upgrade100EliteToMax).FullName;
                var unlocked = (HashSet<string>)AccessTools.Field(typeof(DoctrineServiceBehavior), "_unlocked").GetValue(_service);
                var source = engine.Troop("elite_t5", 26);
                var target = engine.Troop("elite_t6", 31);
                AccessTools.PropertySetter(typeof(CharacterObject), "UpgradeTargets").Invoke(engine.Elite, new object[] { new[] { source } });
                AccessTools.PropertySetter(typeof(CharacterObject), "UpgradeTargets").Invoke(source, new object[] { new[] { target } });
                Tests.AssertTrue(new WCharacter(target).IsElite, "Native noble troops are recognized as elite.");

                dispatcher.RefreshActiveFeats(_service);
                dispatch.Invoke(dispatcher, new object[] { source, target, 10 });
                Tests.AssertEqual(0, _service.GetFeatProgress(key), "Masters-at-Arms remains locked before its prerequisite is acquired.");
                unlocked.Add(typeof(IronDiscipline).FullName);
                unlocked.Add(typeof(SteadfastSoldiers).FullName);
                dispatcher.RefreshActiveFeats(_service);
                dispatch.Invoke(dispatcher, new object[] { source, target, 25 });
                Tests.AssertEqual(25, _service.GetFeatProgress(key), "Native T5 to terminal T6 promotions count in batches.");

                var next = engine.Troop("elite_t7", 36);
                AccessTools.PropertySetter(typeof(CharacterObject), "UpgradeTargets").Invoke(target, new object[] { new[] { next } });
                dispatch.Invoke(dispatcher, new object[] { source, target, 25 });
                Tests.AssertEqual(25, _service.GetFeatProgress(key), "T6 is no longer terminal when a T7 upgrade exists.");
                dispatch.Invoke(dispatcher, new object[] { target, next, 25 });
                Tests.AssertEqual(50, _service.GetFeatProgress(key), "Higher terminal tiers also count.");

                var custom = engine.Custom("custom_elite_t6");
                var faction = new Europe1700RecruitmentTests.TargetFaction { RootElite = custom };
                custom.Faction = faction;
                Tests.AssertTrue(custom.IsElite);
                dispatch.Invoke(dispatcher, new object[] { source, custom.Base, 50 });
                Tests.AssertEqual(100, _service.GetFeatProgress(key), "Custom elites complete the same feat.");
                Tests.AssertTrue(_service.IsFeatComplete(key));
            }
            finally { harmony.UnpatchAll(harmony.Id); _service = old; }
        }

        private sealed class OptionScope<T> : IDisposable
        {
            private readonly Option<T> _option;
            private readonly Func<T> _get;
            private readonly Action<T> _set;
            internal OptionScope(Option<T> option, T value)
            {
                _option = option; _get = option.Getter; _set = option.Setter;
                option.Getter = () => value; option.Setter = next => value = next;
            }
            public void Dispose() { _option.Getter = _get; _option.Setter = _set; }
        }
    }
}
