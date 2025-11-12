using System;
using System.Collections.Generic;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Safety
{
    /// <summary>
    /// Safety helpers for repairing common issues in the game state.
    /// </summary>
    public class Helpers
    {
        /// <summary>
        /// Ensures that the main party has the correct leader assigned.
        /// </summary>
        public static void EnsureMainPartyLeader()
        {
            var p = MobileParty.MainParty;
            var h = Hero.MainHero;

            if (p == null || h == null)
                return;

            if (p.LeaderHero == h)
                return; // All good

            Log.Info(
                $"[Repair] Before: leader={p.LeaderHero?.Name} owner={p.LeaderHero?.Clan?.Leader?.Name} clanTier={p.LeaderHero?.Clan?.Tier}"
            );

            p.PartyComponent.ChangePartyLeader(h);

            Log.Info(
                $"[Repair] After:  leader={p.LeaderHero?.Name} owner={p.LeaderHero?.Clan?.Leader?.Name} clanTier={p.LeaderHero?.Clan?.Tier}"
            );
        }
    }
}
