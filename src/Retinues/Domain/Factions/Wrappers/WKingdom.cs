using System.Collections.Generic;
using Retinues.Domain.Factions.Base;
using TaleWorlds.CampaignSystem;

namespace Retinues.Domain.Factions.Wrappers
{
    public sealed class WKingdom(Kingdom @base) : BaseMapFaction<WKingdom, Kingdom>(@base)
    {
        public static new WKingdom Get(string stringId) =>
            GetFromCampaign(stringId, () => Kingdom.All);

        public static new IEnumerable<WKingdom> All => AllFromCampaign(() => Kingdom.All);
    }
}
