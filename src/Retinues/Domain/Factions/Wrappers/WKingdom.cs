using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Factions.Base;
using TaleWorlds.CampaignSystem;

namespace Retinues.Domain.Factions.Wrappers
{
    public sealed class WKingdom(Kingdom @base) : BaseMapFaction<WKingdom, Kingdom>(@base)
    {
        // Kingdom is handled by campaign data, not MBObjectManager, so we need to override Get and All.
        public static new WKingdom Get(string stringId) =>
            All.FirstOrDefault(c => c.StringId == stringId);

        public static new IEnumerable<WKingdom> All =>
            Kingdom.All.Select(Get).Where(c => c != null);
    }
}
