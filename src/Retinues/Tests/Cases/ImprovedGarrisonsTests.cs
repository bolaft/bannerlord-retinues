using System;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Compatibility.Interops.ImprovedGarrisons;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests.Cases
{
    public static class ImprovedGarrisonsTests
    {
        private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));

        [GameTest("ImprovedGarrisonsHonorsRecruiterAndCultureRestrictions", "recruitment", RequiresCampaign = false)]
        public static void Restrictions()
        {
            foreach (bool sameFaction in new[] { false, true })
            {
                Tests.AssertFalse(RecruitmentOffers.AllowsCustom(Configuration.AllowedRecruitersMode.PlayerOnly, sameFaction, false, true));
                Tests.AssertTrue(RecruitmentOffers.AllowsCustom(Configuration.AllowedRecruitersMode.Everyone, sameFaction, false, false));
                Tests.AssertFalse(RecruitmentOffers.AllowsCustom(Configuration.AllowedRecruitersMode.Everyone, sameFaction, true, false));
                Tests.AssertEqual(sameFaction, RecruitmentOffers.AllowsCustom(Configuration.AllowedRecruitersMode.FactionOnly, sameFaction, true, true));
            }
        }

        [GameTest("ImprovedGarrisonsTransfersPreserveStockAndRollbackFailures", "recruitment", RequiresCampaign = false)]
        public static void Stock()
        {
            var owner = Empty<Hero>();
            var notable = Empty<Hero>();
            var original = Empty<CharacterObject>();
            var custom = Empty<CharacterObject>();
            var changed = Empty<CharacterObject>();
            notable.VolunteerTypes = new[] { original, original };
            var offer = new RecruitmentOffers.Offer(owner, notable, 1, original, custom);
            using (offer.Apply())
            {
                Tests.AssertTrue(notable.VolunteerTypes[1] == custom);
                Tests.AssertTrue(notable.VolunteerTypes[0] == original, "Only the selected slot changes.");
            }
            Tests.AssertTrue(notable.VolunteerTypes[1] == original, "Unconsumed/failed transfers restore stock.");
            using (offer.Apply()) notable.VolunteerTypes[1] = null;
            Tests.AssertTrue(notable.VolunteerTypes[1] == null, "Consumed slots must not refill.");
            Tests.AssertTrue(offer.Apply() == null, "A second transfer cannot duplicate the troop.");
            notable.VolunteerTypes[1] = changed;
            Tests.AssertTrue(offer.Apply() == null, "Stale offers cannot consume changed stock.");
            notable.VolunteerTypes[1] = original;
            using (offer.Apply()) notable.VolunteerTypes[1] = changed;
            Tests.AssertTrue(notable.VolunteerTypes[1] == changed, "Another mod's stock change must survive.");
        }

        [GameTest("ImprovedGarrisonsTemplateAncestryHandlesCustomTreesAndCycles", "recruitment", RequiresCampaign = false)]
        public static void Trees()
        {
            var root = Empty<CharacterObject>();
            var target = Empty<CharacterObject>();
            var unrelated = Empty<CharacterObject>();
            AccessTools.PropertySetter(typeof(CharacterObject), "UpgradeTargets").Invoke(root, new object[] { new[] { target } });
            AccessTools.PropertySetter(typeof(CharacterObject), "UpgradeTargets").Invoke(target, new object[] { new[] { root, null } });
            Tests.AssertTrue(RecruitmentOffers.Reaches(root, target));
            Tests.AssertTrue(RecruitmentOffers.Reaches(target, target));
            Tests.AssertFalse(RecruitmentOffers.Reaches(root, unrelated), "Malformed cycles must terminate.");
            Tests.AssertFalse(RecruitmentOffers.Reaches(null, target));
        }
    }
}
