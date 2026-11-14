using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.Troops.Save;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class FormationPersistenceTests
    {
        [GameTest("FormationChoiceSurvivesFreshWrappers", "save-contracts", RequiresCampaign = false)]
        public static void FreshWrappers()
        {
            using var fixture = new Fixture();
            using var enabled = TestConfig.Set(Config.AllowFormationOverrides, true);
            var troop = fixture.Troop("formation");
            try
            {
                troop.FormationClassOverride = FormationClass.Ranged;
                var fresh = new WCharacter(troop.Base);
                Tests.AssertEqual(FormationClass.Ranged, fresh.FormationClassOverride);
                Tests.AssertEqual(FormationClass.Ranged, fresh.ComputeFormationClass(), "Equipment recalculation respects the shared choice.");
                fresh.FormationClassOverride = FormationClass.Unset;
                Tests.AssertEqual(FormationClass.Unset, troop.FormationClassOverride, "Choosing Auto clears every wrapper's override.");
                Tests.AssertEqual(FormationClass.Infantry, troop.ComputeFormationClass());
            }
            finally { troop.FormationClassOverride = FormationClass.Unset; }
        }

        [GameTest("FormationChoicesReachEverySavedUpgrade", "save-contracts", RequiresCampaign = false)]
        public static void SavedUpgradeTree()
        {
            using var fixture = new Fixture();
            using var enabled = TestConfig.Set(Config.AllowFormationOverrides, true);
            using var customization = TestConfig.Set(Config.EnableTroopCustomization, false);
            var root = fixture.Troop("root");
            var child = fixture.Troop("child");
            var bow = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            bow.StringId = "formation_test_bow";
            bow.AddWeapon(new WeaponComponentData(bow, WeaponClass.Bow, WeaponFlags.RangedWeapon), null);
            child.Base.Equipment[EquipmentIndex.Weapon0] = new EquipmentElement(bow);
            AccessTools.PropertySetter(typeof(TaleWorlds.CampaignSystem.CharacterObject), "UpgradeTargets")
                .Invoke(root.Base, new object[] { new[] { child.Base } });
            try
            {
                root.FormationClassOverride = FormationClass.Ranged;
                child.FormationClassOverride = FormationClass.Infantry;
                Tests.AssertEqual(FormationClass.Ranged, child.Loadout.Battle.ComputeFormationClass());
                Tests.AssertEqual(FormationClass.Infantry, new WCharacter(child.Base).ComputeFormationClass());
                var saved = new TroopSaveData(new WCharacter(root.Base));
                Tests.AssertEqual(FormationClass.Ranged, saved.FormationClassOverride);
                Tests.AssertEqual(1, saved.UpgradeTargets.Count);
                Tests.AssertEqual(FormationClass.Infantry, saved.UpgradeTargets[0].FormationClassOverride,
                    "Recursive save traversal creates fresh wrappers for upgrade troops.");
            }
            finally
            {
                root.FormationClassOverride = FormationClass.Unset;
                child.FormationClassOverride = FormationClass.Unset;
            }
        }

        [GameTest("FormationOverridesDoNotLeakToReusedStubsOrNewCampaigns", "save-contracts", RequiresCampaign = false)]
        public static void Lifecycle()
        {
            using var fixture = new Fixture();
            using var enabled = TestConfig.Set(Config.AllowFormationOverrides, true);
            var troop = fixture.Troop("reused");
            var other = fixture.Troop("other");
            try
            {
                troop.FormationClassOverride = FormationClass.Ranged;
                other.FormationClassOverride = FormationClass.Cavalry;
                AccessTools.Method(typeof(WCharacter), "Initialize").Invoke(troop, new object[] { null });
                Tests.AssertEqual(FormationClass.Unset, new WCharacter(troop.Base).FormationClassOverride);
                Tests.AssertEqual(FormationClass.Cavalry, other.FormationClassOverride, "Reusing one stub leaves other choices intact.");
                var clear = AccessTools.Method(typeof(WCharacter), "ClearFormationOverrides");
                Tests.AssertNotNull(clear);
                clear.Invoke(null, null);
                Tests.AssertEqual(FormationClass.Unset, new WCharacter(other.Base).FormationClassOverride);
            }
            finally
            {
                troop.FormationClassOverride = FormationClass.Unset;
                other.FormationClassOverride = FormationClass.Unset;
            }
        }

        [GameTest("FormationChoiceSurvivesCampaignSaveRoundTrip", "save", "Manual formation survives troop save restoration and the next save")]
        public static void CampaignRoundTrip(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            using var enabled = TestConfig.Set(Config.AllowFormationOverrides, true);
            var troop = sandbox.NewStub();
            troop.FillFrom(Retinues.Game.Player.Clan.Culture.RootBasic, keepUpgrades: false);
            var chosen = troop.Loadout.Battle.ComputeFormationClass() == FormationClass.Infantry
                ? FormationClass.Ranged : FormationClass.Infantry;
            troop.FormationClassOverride = chosen;
            var saved = new TroopSaveData(new WCharacter(troop.Base));
            for (int load = 0; load < 3; load++)
            {
                troop.FormationClassOverride = FormationClass.Unset;
                troop = saved.Deserialize();
                Tests.AssertEqual(chosen, troop.FormationClassOverride);
                Tests.AssertEqual(chosen, troop.FormationClass);
                saved = new TroopSaveData(new WCharacter(troop.Base));
                Tests.AssertEqual(chosen, saved.FormationClassOverride);
            }
        }

        [GameTest("DisabledFormationOverridesKeepSavedChoice", "save-contracts", RequiresCampaign = false)]
        public static void DisabledOption()
        {
            using var fixture = new Fixture();
            using var customization = TestConfig.Set(Config.EnableTroopCustomization, false);
            var troop = fixture.Troop("disabled");
            try
            {
                using (TestConfig.Set(Config.AllowFormationOverrides, false))
                {
                    // Save restoration must store the choice even before settings are enabled.
                    troop.FormationClassOverride = FormationClass.HorseArcher;
                    Tests.AssertEqual(FormationClass.Infantry, troop.ComputeFormationClass(), "Disabled overrides use equipment classification.");
                    Tests.AssertEqual(FormationClass.HorseArcher, new TroopSaveData(new WCharacter(troop.Base)).FormationClassOverride,
                        "Saving with the option disabled must not erase the choice.");
                }
                using (TestConfig.Set(Config.AllowFormationOverrides, true))
                    Tests.AssertEqual(FormationClass.HorseArcher, new WCharacter(troop.Base).ComputeFormationClass());
            }
            finally { troop.FormationClassOverride = FormationClass.Unset; }
        }

        private sealed class Fixture : IDisposable
        {
            internal readonly Europe1700RecruitmentTests.Fixture Engine = new();
            private readonly object _modded = AccessTools.Field(typeof(WCharacter), "_moddedSkills").GetValue(null);
            private readonly object _naval = AccessTools.Field(typeof(WCharacter), "_navalDlcSkills").GetValue(null);
            private readonly Dictionary<string, FormationClass> _overrides =
                (Dictionary<string, FormationClass>)AccessTools.Field(typeof(WCharacter), "FormationOverrides")?.GetValue(null)
                    ?? new Dictionary<string, FormationClass>();
            private readonly Dictionary<string, FormationClass> _originalOverrides;
            private readonly Func<bool> _allowGetter = Config.AllowFormationOverrides.Getter;
            private readonly Action<bool> _allowSetter = Config.AllowFormationOverrides.Setter;
            private bool _allow;
            public Fixture()
            {
                _originalOverrides = new Dictionary<string, FormationClass>(_overrides);
                // Headless fixtures do not initialize MCM's runtime-backed option store.
                Config.AllowFormationOverrides.Getter = () => _allow;
                Config.AllowFormationOverrides.Setter = value => _allow = value;
                AccessTools.Field(typeof(WCharacter), "_moddedSkills").SetValue(null, new List<SkillObject>());
                AccessTools.Field(typeof(WCharacter), "_navalDlcSkills").SetValue(null, new List<SkillObject>());
            }
            internal WCharacter Troop(string name)
            {
                var troop = Engine.Custom(name);
                AccessTools.Field(typeof(TaleWorlds.CampaignSystem.CharacterObject), "DefaultCharacterSkills")
                    .SetValue(troop.Base, Activator.CreateInstance(typeof(MBCharacterSkills), nonPublic: true));
                return troop;
            }
            public void Dispose()
            {
                _overrides.Clear();
                foreach (var pair in _originalOverrides)
                    _overrides.Add(pair.Key, pair.Value);
                Config.AllowFormationOverrides.Getter = _allowGetter;
                Config.AllowFormationOverrides.Setter = _allowSetter;
                AccessTools.Field(typeof(WCharacter), "_moddedSkills").SetValue(null, _modded);
                AccessTools.Field(typeof(WCharacter), "_navalDlcSkills").SetValue(null, _naval);
                Engine.Dispose();
            }
        }
    }
}
