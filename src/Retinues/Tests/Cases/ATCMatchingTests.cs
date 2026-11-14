using Retinues.Compatibility;
using Retinues.Game.Wrappers;
using Retinues.Troops;

namespace Retinues.Tests.Cases
{
    public static class ATCMatchingTests
    {
        [GameTest("ATCMatchingUsesRecruitmentRoleWithoutChangingMilitiaOrUnknownTroops", "recruitment", RequiresCampaign = false)]
        public static void Matching()
        {
            using var f = new Europe1700RecruitmentTests.Fixture();
            f.Active(false);
            var source = new WCharacter(f.Troop("extra_atc"));
            var sharedMilitia = new WCharacter(f.Basic);
            var target = new Europe1700RecruitmentTests.TargetFaction {
                RootBasic = f.Custom("basic"), RootElite = f.Custom("elite"), MilitiaMelee = f.Custom("militia")
            };
            Tests.AssertTrue(TroopMatcher.PickBestFromFaction(target, source) == null);
            Tests.AssertEqual(target.RootBasic, ATCRecruitment.MatchVolunteer(target, source, ATCRecruitRole.Basic));
            Tests.AssertEqual(target.RootElite, ATCRecruitment.MatchVolunteer(target, source, ATCRecruitRole.Elite));
            Tests.AssertEqual(target.RootBasic, ATCRecruitment.MatchVolunteer(target, sharedMilitia, ATCRecruitRole.Basic));
            Tests.AssertEqual(target.MilitiaMelee, TroopMatcher.PickBestFromFaction(target, sharedMilitia));
            Tests.AssertEqual(source, ATCRecruitment.MatchVolunteer(target, source, ATCRecruitRole.None));
            Tests.AssertEqual(source, ATCRecruitment.MatchVolunteer(target, source, ATCRecruitRole.Ambiguous));
            target.RootElite = null;
            Tests.AssertEqual(source, ATCRecruitment.MatchVolunteer(target, source, ATCRecruitRole.Elite));
        }
    }
}