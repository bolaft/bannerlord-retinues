using System;
using Retinues.Compatibility;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Characters.Services.Matching;

namespace Retinues.Tests.Cases
{
    public static class ATCMatchingTests
    {
        [GameTest("ATCMatchingPreservesTierEliteAndNonRecruitmentRules", "recruitment", RequiresCampaign = false)]
        public static void Matching()
        {
            using var f = new Europe1700RecruitmentTests.Fixture();
            f.Active(false);
            var source = f.Wrap(f.Troop("extra_atc"), TroopSourceFlags.None);
            var basic = f.Wrap(f.Troop("basic", custom: true), TroopSourceFlags.Basic);
            var elite = f.Wrap(f.Troop("elite", custom: true), TroopSourceFlags.Elite);
            var higher = f.Wrap(f.Troop("higher", 26, true), TroopSourceFlags.Basic);
            var retinue = f.Wrap(f.Troop("retinue", custom: true), TroopSourceFlags.Elite | TroopSourceFlags.Retinue);
            Tests.AssertTrue(CharacterMatcher.FindBestVolunteer(source, new[] { basic }) == null);
            Tests.AssertEqual(basic, ATCRecruitment.MatchVolunteer(source, new[] { elite, higher, basic }, ATCRecruitRole.Basic));
            Tests.AssertEqual(elite, ATCRecruitment.MatchVolunteer(source, new[] { basic, retinue, elite }, ATCRecruitRole.Elite));
            Tests.AssertTrue(ATCRecruitment.MatchVolunteer(source, new[] { basic }, ATCRecruitRole.Elite) == null);
            Tests.AssertTrue(ATCRecruitment.MatchVolunteer(source, new[] { higher }, ATCRecruitRole.Basic) == null);
            Tests.AssertTrue(ATCRecruitment.MatchVolunteer(source, new[] { basic }, ATCRecruitRole.Ambiguous) == null);
            Tests.AssertTrue(ATCRecruitment.MatchVolunteer(source, new[] { basic }, ATCRecruitRole.None) == null);
            var sharedMilitia = f.Wrap(f.Basic, TroopSourceFlags.Basic | TroopSourceFlags.Militia);
            Tests.AssertEqual(basic, ATCRecruitment.MatchVolunteer(sharedMilitia, new[] { basic }, ATCRecruitRole.Basic));
            Tests.AssertTrue(CharacterMatcher.FindBestVolunteer(sharedMilitia, new[] { basic }) == null,
                "ATC's role override must not escape its explicit recruitment call.");
        }
    }
}
