using System.Collections.Generic;
using Retinues.Core.Game.Events;
using Retinues.Core.Utils;

namespace Retinues.Core.Features.Xp.Behaviors
{
    [SafeClass]
    public sealed class TroopXpMissionBehavior : Combat
    {
        private const int XpPerTier = 5;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnEndMission()
        {
            foreach (var kill in Kills)
            {
                if (!kill.Killer.IsPlayerTroop)
                    continue; // player-side only

                if (!kill.Killer.Character.IsCustom)
                    continue;

                int tier = kill.Victim.Character.Tier;
                int xp = (tier + 1) * XpPerTier;

                if (xp <= 0)
                    continue;

                TroopXpBehavior.Add(kill.Killer.Character, xp);
            }
        }
    }
}
