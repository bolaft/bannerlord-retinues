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
using CoreGame = TaleWorlds.Core.Game;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using ActiveModules = Retinues.Utils.ModuleChecker;
using ModuleEntry = Retinues.Utils.ModuleChecker.ModuleEntry;

namespace Retinues.Tests.Cases
{
    public static class Europe1700RecruitmentTests
    {
        [GameTest("Europe1700UsesRecruitTreesWithoutChangingMilitiaMatching", "recruitment", RequiresCampaign = false)]
        public static void Europe1700UsesRecruitTreesWithoutChangingMilitiaMatching()
        {
            using var f = new Fixture();
            var target = new TargetFaction { RootBasic = f.Custom("regular"), RootElite = f.Custom("elite"),
                MilitiaMelee = f.Custom("melee_militia"), MilitiaRanged = f.Custom("ranged_militia") };
            foreach (var co in new[] { f.Basic, f.Elite })
            {
                var troop = new WCharacter(co);
                var source = Europe1700Recruitment.GetAffectedSource(troop, out var elite);
                Tests.AssertEqual(troop, source);
                Tests.AssertEqual(elite ? target.RootElite : target.RootBasic,
                    Europe1700Recruitment.MatchVolunteer(target, source, elite));
                Tests.AssertEqual(elite ? target.MilitiaRanged : target.MilitiaMelee,
                    TroopMatcher.PickBestFromFaction(target, troop), "Actual militia swaps must retain their special-role mapping.");
            }
            target.RootElite = null;
            Tests.AssertEqual(new WCharacter(f.Elite), Europe1700Recruitment.MatchVolunteer(target, new WCharacter(f.Elite), true),
                "A locked elite tree keeps the original recruit instead of selecting basic troops or militia.");
            var ordinary = new WCharacter(f.Ordinary);
            Tests.AssertTrue(Europe1700Recruitment.GetAffectedSource(ordinary, out _) == null);
            Tests.AssertEqual(target.RootBasic, TroopMatcher.PickBestFromFaction(target, ordinary, sameTierOnly: false));
            f.Active(false);
            Tests.AssertTrue(Europe1700Recruitment.GetAffectedSource(new WCharacter(f.Basic), out _) == null);
            Tests.AssertEqual(target.MilitiaMelee, TroopMatcher.PickBestFromFaction(target, new WCharacter(f.Basic)),
                "Without Europe1700, the existing matcher is unchanged even for overlapping data.");
        }

        [GameTest("Europe1700RepairsOnlyProvenLeftoverMilitiaRecruits", "recruitment", RequiresCampaign = false)]
        public static void Europe1700RepairsOnlyProvenLeftoverMilitiaRecruits()
        {
            using var f = new Fixture();
            var target = new TargetFaction { RootBasic = f.Custom("regular"), RootElite = f.Custom("elite") };
            var leftover = f.Custom("leftover");
            leftover.Faction = target;
            target.MilitiaMelee = leftover;
            Tests.AssertTrue(Europe1700Recruitment.GetAffectedSource(leftover, out _) == null, "No recorded original: do not guess.");
            WCharacter.VanillaStringIdMap[leftover.StringId] = f.Basic.StringId;
            var source = Europe1700Recruitment.GetAffectedSource(leftover, out var elite);
            Tests.AssertEqual(new WCharacter(f.Basic), source);
            Tests.AssertFalse(elite);
            Tests.AssertEqual(target.RootBasic, Europe1700Recruitment.MatchVolunteer(target, source, elite));
            target.RootBasic = null;
            Tests.AssertEqual(source, Europe1700Recruitment.MatchVolunteer(target, source, elite), "Restore the native recruit when its custom tree is unavailable.");
            WCharacter.VanillaStringIdMap[leftover.StringId] = "missing_original";
            Tests.AssertTrue(Europe1700Recruitment.GetAffectedSource(leftover, out _) == null);
            WCharacter.VanillaStringIdMap[leftover.StringId] = f.Basic.StringId;
            target.MilitiaMelee = null;
            Tests.AssertTrue(Europe1700Recruitment.GetAffectedSource(leftover, out _) == null, "Never reinterpret a non-militia custom troop.");
            target.MilitiaMelee = leftover;
            f.Active(false);
            Tests.AssertTrue(Europe1700Recruitment.GetAffectedSource(leftover, out _) == null);
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

        [GameTest("Europe1700NotableRepairsExistingPoolsWithoutTouchingOtherSlots", "recruitment", RequiresCampaign = false)]
        public static void Europe1700NotableRepairsExistingPoolsWithoutTouchingOtherSlots()
        {
            using var f = new Fixture();
            var target = new TargetFaction { RootBasic = f.Custom("regular"), MilitiaMelee = f.Custom("leftover") };
            target.RootBasic.Faction = target;
            target.MilitiaMelee.Faction = target;
            WCharacter.VanillaStringIdMap[target.MilitiaMelee.StringId] = f.Basic.StringId;
            var hero = Empty<Hero>();
            hero.StringId = "recruit_fixture_notable";
            AccessTools.Field(typeof(Hero), "_characterObject").SetValue(hero, f.Troop("notable"));
            AccessTools.Field(typeof(Hero), "_name").SetValue(hero, new TextObject("Fixture notable"));
            var settlement = Empty<TaleWorlds.CampaignSystem.Settlements.Settlement>();
            settlement.StringId = "recruit_fixture_settlement";
            Set(settlement, "Party", Empty<TaleWorlds.CampaignSystem.Party.PartyBase>());
            AccessTools.Field(settlement.GetType(), "_name").SetValue(settlement, new TextObject("Fixture settlement"));
            var notable = new WNotable(hero, new WSettlement(settlement));
            hero.VolunteerTypes = new[] { target.MilitiaMelee.Base, target.RootBasic.Base, null, f.Basic };
            notable.SwapVolunteers(target);
            Tests.AssertTrue(ReferenceEquals(target.RootBasic.Base, hero.VolunteerTypes[0]), "Already-custom militia must bypass the ordinary skip and be repaired.");
            Tests.AssertTrue(ReferenceEquals(target.RootBasic.Base, hero.VolunteerTypes[1]), "Existing regular recruits stay unchanged.");
            Tests.AssertTrue(hero.VolunteerTypes[2] == null, "Do not fill empty recruit slots.");
            Tests.AssertTrue(ReferenceEquals(target.RootBasic.Base, hero.VolunteerTypes[3]));
            f.Active(false);
            hero.VolunteerTypes[0] = target.MilitiaMelee.Base;
            notable.SwapVolunteers(target);
            Tests.AssertTrue(ReferenceEquals(target.MilitiaMelee.Base, hero.VolunteerTypes[0]), "Do not repair other mod setups.");
        }

        private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
        private static void Set(object instance, string name, object value) =>
            AccessTools.PropertySetter(instance.GetType(), name).Invoke(instance, new[] { value });

        internal sealed class Fixture : IDisposable
        {
            private readonly CoreGame _game = CoreGame.Current;
            private readonly Campaign _campaign = Campaign.Current;
            private readonly FieldInfo _modules = AccessTools.Field(typeof(ActiveModules), "_cachedActiveModules");
            private readonly object _oldModules;
            private readonly string _prefix = "retinues_recruit_test_" + Guid.NewGuid().ToString("N") + "_";
            internal readonly CultureObject Culture;
            internal readonly CharacterObject Basic;
            internal readonly CharacterObject Elite;
            internal readonly CharacterObject Ordinary;
            private readonly List<string> _customIds = new();
            internal Fixture()
            {
                if (Campaign.Current != null)
                    Tests.Skip("Isolated engine fixture: run this case through the headless suite.");
                _oldModules = _modules.GetValue(null);
                var game = Empty<CoreGame>();
                AccessTools.Field(typeof(CoreGame), "_current").SetValue(null, game);
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
            internal WCharacter Custom(string name)
            {
                var co = Troop(name, custom: true);
                _customIds.Add(co.StringId);
                WCharacter.ActiveStubIds.Add(co.StringId);
                return new WCharacter(co);
            }

            public void Dispose()
            {
                foreach (var id in _customIds)
                {
                    WCharacter.ActiveStubIds.Remove(id);
                    WCharacter.VanillaStringIdMap.Remove(id);
                    BaseFaction.TroopFactionMap.Remove(id);
                }
                BaseFaction.TouchTroopFactionMap();
                _modules.SetValue(null, _oldModules);
                AccessTools.PropertySetter(typeof(Campaign), "Current").Invoke(null, new object[] { _campaign });
                AccessTools.Field(typeof(CoreGame), "_current").SetValue(null, _game);
            }
        }
        internal sealed class TargetFaction : WFaction
        {
            public TargetFaction() : base(null) { }
            public override WCharacter RootBasic { get; set; }
            public override WCharacter RootElite { get; set; }
            public override WCharacter MilitiaMelee { get; set; }
            public override WCharacter MilitiaRanged { get; set; }
            public override string StringId => "recruit_fixture_faction";
            public override string Name => "Recruit fixture";
            public override uint Color => 0;
            public override uint Color2 => 0;
        }

    }
}
