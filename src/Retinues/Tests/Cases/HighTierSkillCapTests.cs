using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Editor.MVC.Pages.Settings.Views.Panel;
using Retinues.Settings;

namespace Retinues.Tests.Cases
{
    public static class HighTierSkillCapTests
    {
        [GameTest("HighTierSkillCapsAreIndependent", "tiers", RequiresCampaign = false)]
        public static void HighTierSkillCapsAreIndependent()
        {
            using var scope = new SettingsScope();
            Configuration.MaxTroopTier.Value = 10;
            Configuration.SkillCapT7.Value = 370;
            Configuration.SkillCapT8.Value = 480;
            Configuration.SkillCapT9.Value = 590;
            Configuration.SkillCapT10.Value = 700;
            Tests.AssertEqual(370, SkillRules.GetBaseSkillCap(7));
            Tests.AssertEqual(480, SkillRules.GetBaseSkillCap(8));
            Tests.AssertEqual(590, SkillRules.GetBaseSkillCap(9));
            Tests.AssertEqual(700, SkillRules.GetBaseSkillCap(10));
            Configuration.SkillCapT7.Value = 999;
            Tests.AssertEqual(480, SkillRules.GetBaseSkillCap(8), "Changing T7 must not change T8.");
            Configuration.MaxTroopTier.Value = 8;
            Tests.AssertEqual(480, SkillRules.GetBaseSkillCap(10), "The existing maximum-tier clamp still applies.");
            Configuration.MaxTroopTier.Value = 10;
            Tests.AssertEqual(700, SkillRules.GetBaseSkillCap(10), "Temporarily lowering the tier limit must retain higher caps.");
        }

        [GameTest("SkillCapAndTotalControlsFollowMaximumTier", "tiers", RequiresCampaign = false)]
        public static void SkillCapAndTotalControlsFollowMaximumTier()
        {
            using var scope = new SettingsScope(includeTotals: true);
            var options = new[] { "SkillCapT", "SkillTotalT" }
                .SelectMany(prefix => Enumerable.Range(0, 11)
                    .Select(tier => (Option<int>)typeof(Configuration).GetField(prefix + tier).GetValue(null)))
                .ToArray();
            for (int tier = 7; tier <= 10; tier++)
            {
                options[tier].Value = 400 + tier;
                options[11 + tier].Value = 2000 + tier;
            }
            var values = options.Select(option => option.Value).ToArray();
            var controls = options.Select(OptionVM.Create).ToArray();
            try
            {
                foreach (int maximum in new[] { 6, 7, 8, 9, 10, 9, 8, 7, 6, 10 })
                {
                    Configuration.MaxTroopTier.Value = maximum;
                    for (int i = 0; i < controls.Length; i++)
                    {
                        int tier = i % 11;
                        string kind = i < 11 ? "cap" : "total";
                        Tests.AssertEqual(maximum >= tier, controls[i].IsVisible, $"T{tier} {kind} visibility at max tier {maximum}.");
                        Tests.AssertEqual(maximum >= tier, controls[i].IsEnabled);
                        Tests.AssertEqual(values[i], options[i].Value, $"Hiding T{tier} {kind} must preserve its configured value.");
                    }
                }
            }
            finally
            {
                foreach (var control in controls)
                    control.OnFinalize();
            }
        }

        [GameTest("HighTierCapsPreserveOldAndNewSettings", "tiers", RequiresCampaign = false)]
        public static void HighTierCapsPreserveOldAndNewSettings()
        {
            using var scope = new SettingsScope();
            var apply = typeof(ConfigurationPersistence).GetMethod("ApplyValues", BindingFlags.NonPublic | BindingFlags.Static);
            var caps = scope.Options.Skip(1).Cast<IOption>().ToArray();
            void Load(Dictionary<string, string> values) => apply.Invoke(null, new object[] { values, caps });

            Load(new Dictionary<string, string> { [nameof(Configuration.SkillCapT7)] = "520" });
            foreach (var cap in caps)
                Tests.AssertEqual(520, (int)cap.GetObject(), "Old shared T7 caps must carry over to all higher tiers.");

            Load(new Dictionary<string, string>
            {
                [nameof(Configuration.SkillCapT7)] = "430",
                [nameof(Configuration.SkillCapT9)] = "610",
            });
            Tests.AssertEqual(430, Configuration.SkillCapT8.Value);
            Tests.AssertEqual(610, Configuration.SkillCapT9.Value, "An explicit higher-tier cap must win over the legacy fallback.");
            Tests.AssertEqual(430, Configuration.SkillCapT10.Value);

            var saved = caps.ToDictionary(o => o.Key, o => Convert.ToString(o.GetObject(), CultureInfo.InvariantCulture));
            saved[nameof(Configuration.SkillCapT7)] = "999";
            Load(saved);
            Tests.AssertEqual(430, Configuration.SkillCapT8.Value);
            Tests.AssertEqual(610, Configuration.SkillCapT9.Value);
            Tests.AssertEqual(430, Configuration.SkillCapT10.Value, "Reloading explicit settings must not relink the caps to T7.");
        }

        // Suppress configuration events/IO and restore keys and values, including in live tests.
        private sealed class SettingsScope : IDisposable
        {
            internal readonly Option<int>[] Options = {
                Configuration.MaxTroopTier, Configuration.SkillCapT7, Configuration.SkillCapT8,
                Configuration.SkillCapT9, Configuration.SkillCapT10,
            };
            private readonly FieldInfo _loading = typeof(ConfigurationManager).GetField("_loadingConfig", BindingFlags.NonPublic | BindingFlags.Static);
            private readonly object _wasLoading;
            private readonly int[] _values;
            private readonly string[] _keys;

            internal SettingsScope(bool includeTotals = false)
            {
                if (includeTotals)
                    Options = Options.Concat(new[] { Configuration.SkillTotalT7, Configuration.SkillTotalT8,
                        Configuration.SkillTotalT9, Configuration.SkillTotalT10 }).ToArray();
                _wasLoading = _loading.GetValue(null);
                _loading.SetValue(null, true);
                _values = Options.Select(o => o.Value).ToArray();
                _keys = Options.Select(o => o.Key).ToArray();
                string[] keys = { nameof(Configuration.MaxTroopTier), nameof(Configuration.SkillCapT7), nameof(Configuration.SkillCapT8), nameof(Configuration.SkillCapT9), nameof(Configuration.SkillCapT10) };
                if (includeTotals)
                    keys = keys.Concat(new[] { nameof(Configuration.SkillTotalT7), nameof(Configuration.SkillTotalT8),
                        nameof(Configuration.SkillTotalT9), nameof(Configuration.SkillTotalT10) }).ToArray();
                for (int i = 0; i < Options.Length; i++)
                    Options[i].Key = keys[i];
            }

            public void Dispose()
            {
                try
                {
                    for (int i = 0; i < Options.Length; i++)
                    {
                        Options[i].Value = _values[i];
                        Options[i].Key = _keys[i];
                    }
                }
                finally { _loading.SetValue(null, _wasLoading); }
            }
        }
    }
}
