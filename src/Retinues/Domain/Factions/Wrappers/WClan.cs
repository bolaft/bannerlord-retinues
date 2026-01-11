using System.Collections.Generic;
using Retinues.Domain.Factions.Base;
using TaleWorlds.CampaignSystem;

namespace Retinues.Domain.Factions.Wrappers
{
    public sealed class WClan(Clan @base) : BaseMapFaction<WClan, Clan>(@base)
    {
        public static new WClan Get(string stringId) => GetFromCampaign(stringId, () => Clan.All);

        public static new IEnumerable<WClan> All => AllFromCampaign(() => Clan.All);

        public WKingdom Kingdom => WKingdom.Get(Base.Kingdom);
    }
}
