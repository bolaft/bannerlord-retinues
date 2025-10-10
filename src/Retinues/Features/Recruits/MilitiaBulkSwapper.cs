using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Features.Recruits
{
    [SafeClass]
    public static class MilitiaBulkSwapper
    {
        public static void Swap(WFaction faction)
        {
            // Swap all militia in settlements of the faction
            foreach (var s in Campaign.Current.Settlements.Select(s => new WSettlement(s)))
            {
                var f = s.PlayerFaction;
                if (f == null || f != faction)
                    continue;

                s.MilitiaParty?.SwapTroops(f);
                s.GarrisonParty?.SwapTroops(f);
            }

            // Swap all militia mobile parties of the faction
            foreach (var mp in MobileParty.All.Select(mp => new WParty(mp)))
            {
                var f = mp.PlayerFaction;
                if (f == null || f != faction)
                    continue;

                if (mp.IsMilitia || mp.IsGarrison)
                    mp.SwapTroops(f);
            }
        }
    }
}
