using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Managers;

namespace Retinues.Tests.Cases
{
    public static class HighTierSkillCapTests
    {
        [GameTest("HighTierSkillCapsAreIndependent", "tiers", RequiresCampaign = false)]
        public static void HighTierSkillCapsAreIndependent()
        {
            using var scope = new SettingsScope();
            Config.SkillCapTier7Plus.Value = 370;
            Config.SkillCapTier8.Value = 480;
            Config.SkillCapTier9.Value = 590;
            Config.SkillCapTier10.Value = 700;
            Tests.AssertEqual(370, SkillManager.BaseSkillCapByTier(7));
            Tests.AssertEqual(480, SkillManager.BaseSkillCapByTier(8));
            Tests.AssertEqual(590, SkillManager.BaseSkillCapByTier(9));
            Tests.AssertEqual(700, SkillManager.BaseSkillCapByTier(10));
            Config.SkillCapTier7Plus.Value = 999;
            Tests.AssertEqual(480, SkillManager.BaseSkillCapByTier(8));
            Tests.AssertEqual(590, SkillManager.BaseSkillCapByTier(9));
            Tests.AssertEqual(700, SkillManager.BaseSkillCapByTier(10));
            foreach (var cap in scope.Options.Skip(1))
            {
                cap.ApplyPreset(ConfigPreset.Freeform);
                Tests.AssertEqual(999, cap.Value, "Freeform must include the new caps.");
            }
        }

        [GameTest("HighTierCapsPreserveOldAndNewSettings", "tiers", RequiresCampaign = false)]
        public static void HighTierCapsPreserveOldAndNewSettings()
        {
            using var scope = new SettingsScope();
            Config.SkillCapTier7Plus.Value = 520;
            Config.InitializeMissingHighTierSkillCaps(new HashSet<string> { Config.SkillCapTier7Plus.Key });
            foreach (var cap in scope.Options)
                Tests.AssertEqual(520, cap.Value, "An old shared cap must carry over to all higher tiers.");

            Config.SkillCapTier7Plus.Value = 430;
            Config.SkillCapTier9.Value = 610;
            Config.InitializeMissingHighTierSkillCaps(new HashSet<string> { Config.SkillCapTier7Plus.Key, Config.SkillCapTier9.Key });
            Tests.AssertEqual(430, Config.SkillCapTier8.Value);
            Tests.AssertEqual(610, Config.SkillCapTier9.Value, "An explicit cap must not be overwritten.");
            Tests.AssertEqual(430, Config.SkillCapTier10.Value);

            Config.SkillCapTier7Plus.Value = 999;
            Tests.AssertFalse(Config.InitializeMissingHighTierSkillCaps(new HashSet<string>(scope.Options.Select(o => o.Key))));
            Tests.AssertEqual(430, Config.SkillCapTier8.Value);
            Tests.AssertEqual(610, Config.SkillCapTier9.Value);
            Tests.AssertEqual(430, Config.SkillCapTier10.Value, "Later loads must not relink the caps to T7.");
        }

        // Isolate the option accessors so tests cannot write MCM settings or fire gameplay events.
        private sealed class SettingsScope : IDisposable
        {
            internal readonly Option<int>[] Options = { Config.SkillCapTier7Plus, Config.SkillCapTier8, Config.SkillCapTier9, Config.SkillCapTier10 };
            private readonly Func<int>[] _getters;
            private readonly Action<int>[] _setters;

            internal SettingsScope()
            {
                _getters = Options.Select(o => o.Getter).ToArray();
                _setters = Options.Select(o => o.Setter).ToArray();
                foreach (var option in Options)
                {
                    int value = option.DefaultTyped;
                    option.Getter = () => value;
                    option.Setter = v => value = v;
                }
            }

            public void Dispose()
            {
                for (int i = 0; i < Options.Length; i++)
                {
                    Options[i].Getter = _getters[i];
                    Options[i].Setter = _setters[i];
                }
            }
        }
    }
}
