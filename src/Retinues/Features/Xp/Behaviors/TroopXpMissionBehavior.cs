using Retinues.Game.Events;
using Retinues.Utils;

namespace Retinues.Features.Xp.Behaviors
{
    /// <summary>
    /// Mission behavior for awarding XP to custom player-side troops for kills at mission end.
    /// </summary>
    [SafeClass]
    public class TroopXpMissionBehavior : Combat
    {
        private const int XpPerTier = 10;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Awards XP to custom player-side troops for each valid kill at mission end.
        /// </summary>
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
