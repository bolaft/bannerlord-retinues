using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>Tests for the cloning service: source tracking and the skill-budget baseline.</summary>
    public static class CloneTests
    {
        [GameTest(
            "CloneTracksTransitiveSource",
            "clone",
            "CharacterCloner records the transitive source troop id (used by TOR sync)"
        )]
        public static void CloneTracksTransitiveSource(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(looter, "A vanilla 'looter' troop exists to clone from.");

            var clone = sandbox.Track(CharacterCloner.Clone(WCharacter.Get(looter)));
            Tests.AssertNotNull(clone, "Clone produced a custom troop.");
            Tests.AssertEqual(
                looter.StringId,
                clone.SourceStringId,
                "Clone records its vanilla source id."
            );

            // Clone of a clone keeps the original vanilla origin (transitive).
            var clone2 = sandbox.Track(CharacterCloner.Clone(clone));
            Tests.AssertEqual(
                looter.StringId,
                clone2.SourceStringId,
                "Clone-of-clone keeps the original source id."
            );
        }

        [GameTest(
            "CloneSetsSkillBaseline",
            "clone",
            "A cloned troop's skill baseline equals its seeded skill sum"
        )]
        public static void CloneSetsSkillBaseline(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(looter, "A vanilla 'looter' troop exists to clone from.");

            var clone = sandbox.Track(CharacterCloner.Clone(WCharacter.Get(looter)));
            Tests.AssertNotNull(clone, "Clone produced a custom troop.");
            Tests.AssertEqual(
                clone.SkillTotalUsed,
                clone.SkillBaseline,
                "Clone baseline equals its seeded skill sum."
            );
        }
    }
}
