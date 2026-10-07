using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Doctrines.Model;

namespace Retinues.Tests.Cases
{
    public static class CommunityRegressionTests
    {
        [GameTest("DisabledDoctrinesDoNotReceiveFeatEvents", "community", RequiresCampaign = false)]
        public static void DisabledDoctrinesDoNotReceiveFeatEvents()
        {
            using var feats = new OptionScope<bool>(Config.EnableFeatRequirements, true);
            using var multiplier = new OptionScope<float>(Config.EquipmentCostMultiplier, 1f);
            using var costs = new OptionScope<bool>(Config.EquippingTroopsCostsGold, true);
            var service = new DoctrineServiceBehavior();
            // Use the real doctrine/feat models without registering unrelated campaign hooks.
            var model = new CulturalPride();
            var definition = new DoctrineDefinition
            {
                Key = model.Key,
                Feats = model.InstantiateFeats().Select(f => new FeatDefinition
                    { Key = f.Key, Target = f.Target }).ToList(),
            };
            AccessTools.Field(typeof(DoctrineServiceBehavior), "_defsByKey").SetValue(service,
                new Dictionary<string, DoctrineDefinition> { [model.Key] = definition });
            AccessTools.Field(typeof(DoctrineServiceBehavior), "_modelsByKey").SetValue(service,
                new Dictionary<string, Doctrine> { [model.Key] = model });
            var owners = (Dictionary<string, string>)AccessTools.Field(
                typeof(DoctrineServiceBehavior), "_featToDoctrine").GetValue(service);
            foreach (var feat in definition.Feats)
                owners.Add(feat.Key, model.Key);
            Tests.AssertTrue(definition.Feats.Count > 0, "The fixture discovers Cultural Pride's real feats.");
            Tests.AssertFalse(service.IsDoctrineDisabled(model.Key), "Equipment costs enable Cultural Pride.");
            Tests.AssertEqual(DoctrineStatus.Unlockable, service.GetDoctrineStatus(model.Key), "The fixture has incomplete, available feats.");
            var dispatcher = new FeatServiceBehavior();
            var activeField = AccessTools.Field(typeof(FeatServiceBehavior), "_activeFeats");
            int CulturalCount() => ((List<Feat>)activeField.GetValue(dispatcher))
                .Count(f => f.DoctrineType == typeof(CulturalPride));

            dispatcher.RefreshActiveFeats(service);
            Tests.AssertTrue(CulturalCount() > 0, "Enabled Cultural Pride receives events for its unfinished feats.");
            using (new OptionScope<bool>(Config.EquippingTroopsCostsGold, false))
            {
                dispatcher.RefreshActiveFeats(service);
                Tests.AssertEqual(0, CulturalCount(), "Disabled Cultural Pride cannot emit battle/tournament messages.");
            }
            dispatcher.RefreshActiveFeats(service);
            Tests.AssertTrue(CulturalCount() > 0, "Re-enabling costs restores the unfinished feat handlers.");
            using (new OptionScope<float>(Config.EquipmentCostMultiplier, 0f))
            {
                dispatcher.RefreshActiveFeats(service);
                Tests.AssertEqual(0, CulturalCount(), "A zero cost multiplier also disables the handlers.");
            }
        }

        // MCM does not bind option accessors in the headless host. Override only these
        // accessors, restoring the originals even when the test runs inside a campaign.
        private sealed class OptionScope<T> : IDisposable
        {
            private readonly Option<T> _option;
            private readonly Func<T> _getter;
            private readonly Action<T> _setter;

            public OptionScope(Option<T> option, T value)
            {
                _option = option;
                _getter = option.Getter;
                _setter = option.Setter;
                option.Getter = () => value;
                option.Setter = next => value = next;
            }

            public void Dispose()
            {
                _option.Getter = _getter;
                _option.Setter = _setter;
            }
        }
    }
}
