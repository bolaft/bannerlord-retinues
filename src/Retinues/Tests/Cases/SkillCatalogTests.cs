using System.Linq;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the skill list a troop is offered in the editor. Troops must get the combat skills
    /// but never the hero-only skills (Leadership, Steward, ...).
    /// </summary>
    public static class SkillCatalogTests
    {
        [GameTest(
            "TroopSkillsIncludeCombatExcludeHero",
            "skills",
            "A troop's skill list has the combat skills and omits hero-only skills"
        )]
        public static void TroopSkillsIncludeCombatExcludeHero(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(looter, "A vanilla 'looter' troop exists.");

            var troop = WCharacter.Get(looter);
            Tests.AssertFalse(troop.IsHero, "The 'looter' is a non-hero troop.");

            var ids = SkillCatalog
                .GetSkills(troop)
                .Where(s => s != null)
                .Select(s => s.StringId)
                .ToHashSet();

            Tests.AssertTrue(ids.Count > 0, "Troop has a non-empty skill list.");
            Tests.AssertTrue(
                ids.Contains(DefaultSkills.OneHanded.StringId),
                "Troop skills include One-Handed."
            );
            Tests.AssertTrue(
                ids.Contains(DefaultSkills.Riding.StringId),
                "Troop skills include Riding."
            );
            Tests.AssertFalse(
                ids.Contains(DefaultSkills.Leadership.StringId),
                "Troop skills exclude the hero-only Leadership skill."
            );
            Tests.AssertFalse(
                ids.Contains(DefaultSkills.Steward.StringId),
                "Troop skills exclude the hero-only Steward skill."
            );
        }
    }
}
