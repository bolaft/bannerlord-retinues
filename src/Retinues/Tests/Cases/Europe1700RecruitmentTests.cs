using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Compatibility;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Game = TaleWorlds.Core.Game;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Characters.Services.Matching;
using ActiveModules = Retinues.Framework.Modules.ModuleManager;
using ModuleEntry = Retinues.Framework.Modules.ModuleManager.ModuleInfo;

namespace Retinues.Tests.Cases
{
    public static class Europe1700RecruitmentTests
    {
        [GameTest("Europe1700VolunteerFilterPreservesOtherCategoriesAndTiers", "recruitment", RequiresCampaign = false)]
        public static void Europe1700VolunteerFilterPreservesOtherCategoriesAndTiers()
        {
            using var f = new Fixture();
            var basic = f.Wrap(f.Basic, TroopSourceFlags.Basic | TroopSourceFlags.Militia);
            var elite = f.Wrap(f.Elite, TroopSourceFlags.Basic | TroopSourceFlags.Elite | TroopSourceFlags.Militia);
            var custom = f.Wrap(f.Troop("custom", custom: true), TroopSourceFlags.Basic);
            var customElite = f.Wrap(f.Troop("custom_elite", custom: true), TroopSourceFlags.Elite);
            foreach (var pair in new[] { (basic, custom), (elite, customElite) })
            {
                Tests.AssertTrue(CharacterMatcher.FindBest(pair.Item1, new[] { pair.Item2 }) == null,
                    "The general-purpose matcher must retain its original category rules.");
                Tests.AssertEqual(pair.Item2, CharacterMatcher.FindBestVolunteer(pair.Item1, new[] { pair.Item2 }));
                Tests.AssertEqual(pair.Item1, CharacterMatcher.FindBestVolunteer(pair.Item2, new[] { pair.Item1 }),
                    "AI and vanilla-mix fallback must also match in the reverse direction.");
            }
            var wrongTier = f.Wrap(f.Troop("higher_tier", 26, true), TroopSourceFlags.Basic);
            Tests.AssertTrue(CharacterMatcher.FindBestVolunteer(basic, new[] { wrongTier }) == null);
            Tests.AssertTrue(CharacterMatcher.FindBestVolunteer(basic, new[] { customElite }) == null);
            var mixedRole = f.Wrap(f.Troop("caravan", custom: true), TroopSourceFlags.Basic | TroopSourceFlags.Caravan);
            Tests.AssertTrue(CharacterMatcher.FindBestVolunteer(basic, new[] { mixedRole }) == null);
            var militia = f.Wrap(f.Troop("isolated"), TroopSourceFlags.Militia);
            Set(f.Culture, "MeleeEliteMilitiaTroop", militia.Base);
            Tests.AssertTrue(CharacterMatcher.FindBestVolunteer(militia, new[] { custom }) == null);
            var ordinary = f.Wrap(f.Ordinary, TroopSourceFlags.Basic);
            Tests.AssertEqual(custom, CharacterMatcher.FindBestVolunteer(ordinary, new[] { custom }));
            f.Active(false);
            Tests.AssertTrue(CharacterMatcher.FindBestVolunteer(basic, new[] { custom }) == null);
            Tests.AssertEqual(custom, CharacterMatcher.FindBestVolunteer(ordinary, new[] { custom }));
        }

        [GameTest("Europe1700RequiresActiveModuleAndSharedMilitiaRole", "recruitment", RequiresCampaign = false)]
        public static void Europe1700RequiresActiveModuleAndSharedMilitiaRole()
        {
            using var f = new Fixture();
            Tests.AssertTrue(Europe1700Recruitment.TryGetTreeKind(f.Basic, out var elite) && !elite);
            Tests.AssertTrue(Europe1700Recruitment.TryGetTreeKind(f.Elite, out elite) && elite,
                "An elite root may also be an upgrade of the basic root.");
            Tests.AssertFalse(Europe1700Recruitment.TryGetTreeKind(f.Ordinary, out _), "Ordinary recruits need no compatibility override.");
            var isolated = f.Troop("isolated_militia");
            Set(f.Culture, "MeleeEliteMilitiaTroop", isolated);
            Tests.AssertFalse(Europe1700Recruitment.TryGetTreeKind(isolated, out _), "Militia outside a normal tree stays militia.");
            // A third-party tree cycle must not hang recruitment.
            Set(f.Elite, "UpgradeTargets", new[] { f.Basic });
            Tests.AssertFalse(Europe1700Recruitment.TryGetTreeKind(isolated, out _));
            Tests.AssertFalse(Europe1700Recruitment.TryGetTreeKind(null, out _));
            f.Active(false);
            Tests.AssertFalse(Europe1700Recruitment.TryGetTreeKind(f.Basic, out _), "Another overhaul with the same data must remain untouched.");
        }

        private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
        private static void Set(object instance, string name, object value) =>
            AccessTools.PropertySetter(instance.GetType(), name).Invoke(instance, new[] { value });

        internal sealed class Fixture : IDisposable
        {
            private readonly Game _game = Game.Current;
            private readonly Campaign _campaign = Campaign.Current;
            private readonly FieldInfo _modules = AccessTools.Field(typeof(ActiveModules), "_cachedActiveModules");
            private readonly object _oldModules;
            private readonly string _prefix = "retinues_recruit_test_" + Guid.NewGuid().ToString("N") + "_";
            internal readonly CultureObject Culture;
            internal readonly CharacterObject Basic;
            internal readonly CharacterObject Elite;
            internal readonly CharacterObject Ordinary;
            private readonly FieldInfo _built = AccessTools.Field(typeof(SourceFlagCache), "_built");
            private readonly Dictionary<string, TroopSourceFlags> _flags = (Dictionary<string, TroopSourceFlags>)AccessTools.Field(typeof(SourceFlagCache), "ById").GetValue(null);
            private readonly Dictionary<string, TroopSourceFlags> _oldFlags;
            private readonly object _oldBuilt;
            internal Fixture()
            {
                if (Campaign.Current != null)
                    Tests.Skip("Isolated engine fixture: run this case through the headless suite.");
                _oldModules = _modules.GetValue(null);
                var game = Empty<Game>();
                AccessTools.Field(typeof(Game), "_current").SetValue(null, game);
                var skills = Empty<DefaultSkills>();
                foreach (var field in typeof(DefaultSkills).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (field.FieldType != typeof(SkillObject)) continue;
                    var skill = Empty<SkillObject>();
                    skill.StringId = field.Name;
                    field.SetValue(skills, skill);
                }
                Set(game, "DefaultSkills", skills);
                var campaign = Empty<Campaign>();
                AccessTools.PropertySetter(typeof(Campaign), "Current").Invoke(null, new object[] { campaign });
                var models = Empty<GameModels>();
                Set(models, "CharacterStatsModel", new DefaultCharacterStatsModel());
                AccessTools.Field(typeof(Campaign), "_gameModels").SetValue(campaign, models);
                Active(true);
                Culture = Empty<CultureObject>(); Culture.StringId = _prefix + "culture";
                Basic = Troop("moroccan_levy");
                Elite = Troop("moroccan_bukhari_infantry");
                Ordinary = Troop("ordinary_recruit");
                Set(Basic, "UpgradeTargets", new[] { Ordinary, Elite });
                Set(Culture, "BasicTroop", Basic);
                Set(Culture, "EliteBasicTroop", Elite);
                Set(Culture, "MeleeMilitiaTroop", Basic);
                Set(Culture, "RangedMilitiaTroop", Elite);
                _oldFlags = new Dictionary<string, TroopSourceFlags>(_flags);
                _oldBuilt = _built.GetValue(null);
                _flags.Clear(); _built.SetValue(null, true);
            }

            internal void Active(bool active) => _modules.SetValue(null, active
                ? new List<ModuleEntry> { new ModuleEntry { Id = "Europe1700" } }
                : new List<ModuleEntry> { new ModuleEntry { Id = "AnotherOverhaul" } });

            internal CharacterObject Troop(string name, int level = 11, bool custom = false)
            {
                var co = Empty<CharacterObject>();
                co.StringId = (custom ? "retinues_custom_" : "") + _prefix + name;
                AccessTools.Field(typeof(BasicCharacterObject), "_basicName").SetValue(co, new TextObject("Fixture " + name));
                AccessTools.PropertySetter(typeof(BasicCharacterObject), "Culture").Invoke(co, new object[] { Culture });
                AccessTools.PropertySetter(typeof(BasicCharacterObject), "Level").Invoke(co, new object[] { level });
                AccessTools.PropertySetter(typeof(BasicCharacterObject), "Age").Invoke(co, new object[] { 25f });
                Set(co, "UpgradeTargets", Array.Empty<CharacterObject>());
                var roster = Empty<MBEquipmentRoster>();
                AccessTools.Field(typeof(MBEquipmentRoster), "_equipments").SetValue(roster, new MBList<Equipment> { new Equipment() });
                AccessTools.Field(typeof(BasicCharacterObject), "_equipmentRoster").SetValue(co, roster);
                return co;
            }
            internal WCharacter Wrap(CharacterObject co, TroopSourceFlags flags)
            {
                _flags[co.StringId] = flags;
                return new WCharacter(co);
            }

            public void Dispose()
            {
                _flags.Clear();
                foreach (var entry in _oldFlags) _flags.Add(entry.Key, entry.Value);
                _built.SetValue(null, _oldBuilt);
                _modules.SetValue(null, _oldModules);
                AccessTools.PropertySetter(typeof(Campaign), "Current").Invoke(null, new object[] { _campaign });
                AccessTools.Field(typeof(Game), "_current").SetValue(null, _game);
            }
        }

    }
}
