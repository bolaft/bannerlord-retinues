using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Game.Helpers
{
    /// <summary>
    /// Reverts existing custom troops to their vanilla original.
    /// </summary>
    [SafeClass]
    public static class TroopReverter
    {
        public static void SwapToVanilla(WCharacter troop)
        {
            if (troop == null)
                return;

            var vanilla = new WCharacter(troop.VanillaStringId);

            foreach (var party in MobileParty.All.Select(mp => new WParty(mp)))
            {
                party.MemberRoster.SwapTroop(troop, vanilla);
                party.PrisonRoster.SwapTroop(troop, vanilla);
            }
        }
    }
}
