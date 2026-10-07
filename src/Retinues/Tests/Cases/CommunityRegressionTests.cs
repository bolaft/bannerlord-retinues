using System;
using System.Reflection;
using System.Runtime.Serialization;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Behaviors.Doctrines.Feats.Equipments;
using Retinues.Behaviors.Doctrines.Feats.Retinues;
using Retinues.Behaviors.Retinues;
using Retinues.Behaviors.Experience.Patches;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace Retinues.Tests.Cases
{
    public static class CommunityRegressionTests
    {
        [GameTest("XpFaultDetailsHandleBrokenTroops", "community", RequiresCampaign = false)]
        public static void XpFaultDetailsHandleBrokenTroops()
        {
            Tests.AssertTrue(XpFaultDiagnosticsPatch.DescribeElement(default).Contains("troop=<null>"),
                "A null roster character can be diagnosed without dereferencing it.");
            var troop = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
            troop.StringId = "community_xp_probe";
            var element = new TroopRosterElement(troop) { Number = 4, Xp = 123 };
            Tests.AssertTrue(XpFaultDiagnosticsPatch.DescribeElement(element).Contains("<null array>"),
                "Missing upgrade arrays are distinguished from terminal troops.");
            typeof(CharacterObject).GetProperty("UpgradeTargets").GetSetMethod(true)
                .Invoke(troop, new object[] { new CharacterObject[] { null, troop } });
            var details = XpFaultDiagnosticsPatch.DescribeElement(element);
            Tests.AssertTrue(details.Contains("<null target>,community_xp_probe"), "Broken and valid target IDs remain visible.");
            Tests.AssertTrue(details.Contains("count=4") && details.Contains("xp=123"), "Stack size and XP are retained.");
            Tests.AssertEqual(123, element.Xp, "Diagnostics do not repair or clamp the saved XP.");
        }

        [GameTest("PlayerRecruitmentDispatchesOnceAndRespectsActivity", "community", RequiresCampaign = false)]
        public static void PlayerRecruitmentDispatchesOnceAndRespectsActivity()
        {
            var recruited = new MbEvent<CharacterObject, int>();
            Tests.AssertFalse(new RecruitmentProbe().IsEnabled, "The test probe cannot register in a real campaign.");
            var listener = new RecruitmentProbe { Enabled = true };
            listener.RegisterPlayerRecruitmentListener(recruited);
            // Invoke is internal in 1.2 and public in later engines.
            var invoke = recruited.GetType().GetMethod("Invoke", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Tests.AssertNotNull(invoke, "The real engine event supports dispatch.");
            void Recruit(CharacterObject character, int amount) => invoke.Invoke(recruited, new object[] { character, amount });
            var troop = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
            // No ID: the wrapper factory does not retain this temporary object in its ID cache.
            troop.StringId = string.Empty;
            Recruit(troop, 5);
            Tests.AssertEqual(1, listener.Calls, "One engine recruitment event produces one callback.");
            Tests.AssertEqual(5, listener.Amount, "The actual recruitment count is preserved.");
            Tests.AssertTrue(ReferenceEquals(troop, listener.Troop.Base), "The recruited troop is preserved.");
            Recruit(null, 5);
            Recruit(troop, 0);
            Recruit(troop, -1);
            listener.Active = false;
            Recruit(troop, 5);
            Tests.AssertEqual(1, listener.Calls, "Missing, non-positive and inactive events cannot award progress.");

            foreach (var type in new[] { typeof(Feat_RoyalPatronage_RoyalLevy), typeof(Feat_Vanguard_RaiseTheVanguard) })
            {
                var hook = type.GetMethod("OnPlayerRecruitedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
                Tests.AssertEqual(type, hook?.DeclaringType, "Player recruitment feats subscribe to the player's event: " + type.Name);
                var aiHook = type.GetMethod("OnTroopRecruited", BindingFlags.Instance | BindingFlags.NonPublic);
                Tests.AssertEqual(typeof(BaseCampaignBehavior), aiHook?.DeclaringType,
                    "The same recruitment cannot also be counted through the AI event.");
            }
        }

        private sealed class RecruitmentProbe : BaseCampaignBehavior
        {
            public bool Enabled;
            public override bool IsEnabled => Enabled;
            public bool Active = true;
            public override bool IsActive => Active;
            public int Calls;
            public int Amount;
            public WCharacter Troop;
            protected override void OnPlayerRecruitedTroops(WCharacter troop, int amount)
            {
                Calls++;
                Amount += amount;
                Troop = troop;
            }
        }

        [GameTest("AIEquipmentRejectsOrdinaryTroopLinks", "community", RequiresCampaign = false)]
        public static void AIEquipmentRejectsOrdinaryTroopLinks()
        {
            WCharacter Troop(string id)
            {
                var character = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
                character.StringId = id;
                return new WCharacter(character);
            }
            Tests.AssertFalse(AIClanRetinuesBehavior.IsEditableRetinue(null), "Missing retinue links are rejected.");
            Tests.AssertFalse(AIClanRetinuesBehavior.IsEditableRetinue(Troop("imperial_legionary")),
                "A stale retinue link cannot authorize edits to an ordinary cultural troop.");
            Tests.AssertFalse(AIClanRetinuesBehavior.IsEditableRetinue(Troop("rot_house_guard")),
                "Overhaul troops are protected too, regardless of their displayed name.");
            Tests.AssertTrue(AIClanRetinuesBehavior.IsEditableRetinue(Troop(WCharacter.CustomTroopPrefix + "probe")),
                "The safety guard still permits the mod's custom retinue objects.");
        }

        [GameTest("LoadedRepeatableFeatCanResume", "community", RequiresCampaign = false)]
        public static void LoadedRepeatableFeatCanResume()
        {
            using var requirements = TestConfig.Set(Configuration.EnableFeatRequirements, true);
            var category = new Category("community_probe", new TextObject("Probe"));
            var doctrine = category.Add("community_probe", new TextObject("Probe"), new TextObject("Probe"), "", null, null);
            var repeatable = doctrine.Add("community_repeatable", new TextObject("Probe"), new TextObject("Probe"), 100, 15, true);
            var single = doctrine.Add("community_single", new TextObject("Probe"), new TextObject("Probe"), 100, 15, false);
            var zeroTarget = doctrine.Add("community_zero", new TextObject("Probe"), new TextObject("Probe"), 0, 15, true);
            Tests.AssertFalse(zeroTarget.IsInProgress, "A zero-target definition must not enter the repeatable completion loop.");
            repeatable.ForceSet(100);
            single.ForceSet(100);
            Tests.AssertTrue(repeatable.IsInProgress,
                "A repeatable feat parked at its target by an older save must still receive events.");
            Tests.AssertTrue(single.IsCompleted, "A completed one-time feat must stay completed.");
            repeatable.Add(1);
            Tests.AssertEqual(1, repeatable.Progress, "The old completion wraps while the new event is retained.");
            Tests.AssertEqual(15, doctrine.Progress, "The parked completion awards its previously missing worth once.");
            doctrine.IsAcquired = true;
            Tests.AssertFalse(repeatable.IsInProgress, "Acquired doctrines cannot keep receiving feat events.");
        }

        [GameTest("VanguardCountsOnlyNewRetinueConversions", "community", RequiresCampaign = false)]
        public static void VanguardCountsOnlyNewRetinueConversions()
        {
            Tests.AssertTrue(Feat_Vanguard_RaiseTheVanguard.CountsAsNewRetinues(TroopSourceFlags.Basic, TroopSourceFlags.Retinue, 5),
                "A five-unit regular-to-retinue conversion recruits five new retinues.");
            Tests.AssertTrue(Feat_Vanguard_RaiseTheVanguard.CountsAsNewRetinues(TroopSourceFlags.None, TroopSourceFlags.Retinue, 1),
                "Eligible overhaul troops need not have a standard source category.");
            Tests.AssertFalse(Feat_Vanguard_RaiseTheVanguard.CountsAsNewRetinues(TroopSourceFlags.Retinue | TroopSourceFlags.Elite, TroopSourceFlags.Retinue, 5),
                "Retinue-to-retinue upgrades cannot inflate recruitment progress.");
            Tests.AssertFalse(Feat_Vanguard_RaiseTheVanguard.CountsAsNewRetinues(TroopSourceFlags.Basic, TroopSourceFlags.Elite, 5),
                "Ordinary troop upgrades do not recruit retinues.");
            Tests.AssertFalse(Feat_Vanguard_RaiseTheVanguard.CountsAsNewRetinues(TroopSourceFlags.Basic, TroopSourceFlags.Retinue, 0),
                "A no-op conversion awards nothing.");
            Tests.AssertFalse(Feat_Vanguard_RaiseTheVanguard.CountsAsNewRetinues(TroopSourceFlags.Basic, TroopSourceFlags.Retinue, -1),
                "Invalid negative amounts award nothing.");
            var hook = typeof(Feat_Vanguard_RaiseTheVanguard).GetMethod("OnPlayerUpgradedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
            Tests.AssertEqual(typeof(Feat_Vanguard_RaiseTheVanguard), hook?.DeclaringType,
                "The feat listens to the event emitted when party-screen conversions are committed.");
        }

        [GameTest("RoyalStewardshipSkipsSettlementsWithoutGovernors", "community", RequiresCampaign = false)]
        public static void RoyalStewardshipSkipsSettlementsWithoutGovernors()
        {
            var culture = (CultureObject)FormatterServices.GetUninitializedObject(typeof(CultureObject));
            culture.StringId = "community_test_culture";
            var village = (Settlement)FormatterServices.GetUninitializedObject(typeof(Settlement));
            var wrapped = new WSettlement(village);
            Tests.AssertTrue(wrapped.Town == null, "A settlement without a Town component reproduces a bound village.");
            Tests.AssertFalse(Feat_RoyalPatronage_RoyalStewardship.HasMatchingGovernor(
                new[] { null, wrapped }, new WCulture(culture)),
                "Village and missing settlement entries cannot interrupt the daily governor scan.");
            Tests.AssertFalse(Feat_RoyalPatronage_RoyalStewardship.HasMatchingGovernor(null, new WCulture(culture)),
                "A missing clan settlement list has no eligible governor.");
            Tests.AssertFalse(Feat_RoyalPatronage_RoyalStewardship.HasMatchingGovernor(new[] { wrapped }, null),
                "An unknown kingdom culture cannot qualify a governor.");
        }
    }
}
