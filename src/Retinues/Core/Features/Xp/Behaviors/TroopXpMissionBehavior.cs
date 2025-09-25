using System;
using System.Collections.Generic;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Features.Xp.Behaviors
{
    public sealed class TroopXpMissionBehavior : Combat
    {
        private const int XpPerTier = 5;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnEndMission()
        {
            try
            {
                Dictionary<WCharacter, int> xpPerTroop = [];

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

                    xpPerTroop.TryGetValue(kill.Killer.Character, out var current);
                    xpPerTroop[kill.Killer.Character] = current + xp;
                }

                Log.Info("XP earned this mission:");
                foreach (var kv in xpPerTroop)
                    Log.Info($"  {kv.Key.Name}: {kv.Value} XP");

                TroopXpService.AccumulateFromMission(xpPerTroop);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
