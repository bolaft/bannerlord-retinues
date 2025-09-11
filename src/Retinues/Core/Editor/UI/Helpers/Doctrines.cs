using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Retinues.Core.Game.Features.Tech;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Editor.UI.Helpers
{
    public static class Doctrines
    {
        public static IEnumerable<DoctrineDef> BuildStarterDoctrinesForFaction(WFaction f)
        {
            // 4 columns x 4 rows, each requiring the one above:
            for (int c = 0; c < 4; c++)
            {
                for (int r = 0; r < 4; r++)
                {
                    var id = $"doc_{c}_{r}";
                    yield return new DoctrineDef
                    {
                        Id = id,
                        Title = $"Doctrine {c + 1}.{r + 1}",
                        Description = "TBD",
                        IconId = "cct_doctrine_generic",
                        Column = c,
                        Row = r,
                        GoldCost = 150 + 50 * r,
                        Duration = CampaignTime.Hours(12 + 6 * r),
                        PrerequisiteId = r > 0 ? $"doc_{c}_{r - 1}" : null,
                        FeatId = (r == 0) ? "feat_column_starter" : null,
                        RequiredSkill = DefaultSkills.Tactics,
                        RequiredSkillValue = 80 + 10 * r
                    };
                }
            }
        }
    }
}
