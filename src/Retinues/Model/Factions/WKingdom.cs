using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace Retinues.Model.Factions
{
    public sealed class WKingdom(Kingdom @base) : BaseMapFaction<WKingdom, Kingdom>(@base)
    {
        public static new IEnumerable<WKingdom> All
        {
            get
            {
                foreach (
                    var kingdom in Kingdom
                        .All.OrderBy(c => c.Culture.ToString())
                        .ThenBy(c => c.Name.ToString())
                )
                    if (kingdom != null)
                        yield return Get(kingdom);
            }
        }
    }
}
