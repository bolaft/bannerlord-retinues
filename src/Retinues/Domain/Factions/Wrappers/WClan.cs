using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Factions.Base;
using TaleWorlds.CampaignSystem;

namespace Retinues.Domain.Factions.Wrappers
{
    public sealed class WClan(Clan @base) : BaseMapFaction<WClan, Clan>(@base)
    {
        // Clan is handled by campaign data, not MBObjectManager, so we need to override Get and All.
        public static new WClan Get(string stringId) =>
            All.FirstOrDefault(c => c.StringId == stringId);

        public static new IEnumerable<WClan> All => Clan.All.Select(Get).Where(c => c != null);
    }
}
