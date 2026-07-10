using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>Tests for skill persistence and clone isolation.</summary>
    public static class SkillTests
    {
        [GameTest(
            "MarinerSkillRoundTrips",
            "persistence",
            "A troop's Mariner skill value survives serialize -> rebuild -> deserialize (Naval DLC)"
        )]
        public static void MarinerSkillRoundTrips(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var mariner = MBObjectManager.Instance?.GetObject<SkillObject>(
                SkillCatalog.MarinerSkillId
            );
            if (mariner == null)
                return; // Naval DLC not loaded in this game; the Mariner skill doesn't exist.

            using var sandbox = new TestSandbox();

            var wc = sandbox.NewStub();
            wc.IsMariner = true;
            wc.Skills[mariner] = 66;

            var saved = wc.Serialize();

            // Wipe the value and force the skills container to rebuild from GetPersistedSkills, as it
            // does on load. If Mariner isn't in that list, the saved value is dropped on deserialize.
            wc.Skills[mariner] = 0;
            wc.ClearSkillsCache();
            wc.Deserialize(saved);

            Tests.AssertEqual(
                66,
                wc.Skills[mariner],
                "Mariner skill value round-trips even after the skills container is rebuilt."
            );
        }

        [GameTest(
            "SkillValueRoundTrips",
            "persistence",
            "A troop's skill value survives serialize -> deserialize"
        )]
        public static void SkillValueRoundTrips(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var wc = sandbox.NewStub();
            wc.Skills[DefaultSkills.OneHanded] = 137;

            var saved = wc.Serialize();
            wc.Skills[DefaultSkills.OneHanded] = 5;
            wc.Deserialize(saved);

            Tests.AssertEqual(
                137,
                wc.Skills[DefaultSkills.OneHanded],
                "OneHanded skill value round-trips."
            );
        }

        [GameTest(
            "CloneSkillsAreDetached",
            "clone",
            "Editing a clone's skills does not affect the source troop (no shared container)"
        )]
        public static void CloneSkillsAreDetached(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(looter, "A vanilla 'looter' troop exists to clone from.");

            var source = WCharacter.Get(looter);
            int sourceSkill = source.Skills[DefaultSkills.OneHanded];

            var clone = sandbox.Track(CharacterCloner.Clone(source));
            clone.Skills[DefaultSkills.OneHanded] = sourceSkill + 77;

            Tests.AssertEqual(
                sourceSkill,
                source.Skills[DefaultSkills.OneHanded],
                "Mutating the clone's skill leaves the source unchanged (detached container)."
            );
        }
    }
}
