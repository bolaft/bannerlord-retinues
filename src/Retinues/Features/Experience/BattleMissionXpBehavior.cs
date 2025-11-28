using Retinues.Game.Events;
using Retinues.Utils;

namespace Retinues.Features.Experience
{
    /// <summary>
    /// Mission behavior for awarding XP to custom player-side troops for kills at mission end.
    /// </summary>
    [SafeClass]
    public class BattleMissionXpBehavior : Combat
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
            Log.Info("BattleMissionXpBehavior: OnEndMission - awarding XP for kills.");
            foreach (var kill in Kills)
            {
                if (!kill.KillerIsPlayerTroop)
                    continue; // player-side only

                if (!kill.Killer.IsCustom)
                    continue;

                int tier = kill.Victim.Tier;
                int xp = (tier + 1) * XpPerTier;

                if (xp <= 0)
                    continue;

                TroopXpBehavior.Add(kill.Killer, xp);
            }
            Log.Info("BattleMissionXpBehavior: OnEndMission complete.");
        }
    }
}
