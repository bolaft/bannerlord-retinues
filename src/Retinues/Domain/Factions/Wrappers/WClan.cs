using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Factions.Base;
using TaleWorlds.CampaignSystem;

namespace Retinues.Domain.Factions.Wrappers
{
    public sealed class WClan(Clan @base) : BaseMapFaction<WClan, Clan>(@base)
    {
        public static new IEnumerable<WClan> All
        {
            get
            {
                foreach (
                    var clan in Clan
                        .All.OrderBy(c => c.Culture.ToString())
                        .ThenBy(c => c.Name.ToString())
                )
                    if (clan != null)
                        yield return Get(clan);
            }
        }
    }
}
