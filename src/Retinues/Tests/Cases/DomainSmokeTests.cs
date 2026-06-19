using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>Broad domain invariants.</summary>
    public static class DomainSmokeTests
    {
        [GameTest(
            "CustomVsVanillaClassification",
            "domain",
            "Cloned troops are custom; vanilla troops are not"
        )]
        public static void CustomVsVanillaClassification(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(looter, "A vanilla 'looter' troop exists.");

            var vanilla = WCharacter.Get(looter);
            Tests.AssertTrue(vanilla.IsVanilla, "Looter is vanilla.");
            Tests.AssertFalse(vanilla.IsCustom, "Looter is not custom.");

            var clone = sandbox.Track(CharacterCloner.Clone(vanilla));
            Tests.AssertTrue(clone.IsCustom, "A clone is a custom troop.");
            Tests.AssertFalse(clone.IsVanilla, "A clone is not vanilla.");
        }

        [GameTest("SkillRulesBounds", "skills", "Skill cap/total stay within engine bounds")]
        public static void SkillRulesBounds(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var wc = sandbox.NewStub();
            wc.SkillBaseline = 0;

            int cap = SkillRules.GetSkillCap(wc);
            int total = SkillRules.GetSkillTotal(wc);

            Tests.AssertTrue(
                cap > 0 && cap <= SkillRules.MaxSkillLevel,
                $"Skill cap is within (0, {SkillRules.MaxSkillLevel}]: {cap}"
            );
            Tests.AssertTrue(total > 0, $"Skill total is positive: {total}");
            Tests.AssertTrue(wc.SkillTotalRemaining >= 0, "Skill total remaining is non-negative.");
        }
    }
}
