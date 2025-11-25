using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Agents
{
    public static class CaptainHelper
    {
        private const int CaptainFrequency = 15;

        private static Mission _lastMission;
        private static readonly Dictionary<string, int> _spawnCounts = new();

        public static IAgentOriginBase AdjustOrigin(IAgentOriginBase origin)
        {
            try
            {
                if (origin == null)
                    return null;

                var mission = Mission.Current;
                if (mission == null)
                    return origin;

                // reset per mission
                if (!ReferenceEquals(mission, _lastMission))
                {
                    _lastMission = mission;
                    _spawnCounts.Clear();
                }

                var co = origin.Troop as CharacterObject;
                if (co == null)
                    return origin;

                var troop = new WCharacter(co);
                if (!troop.IsCustom || troop.IsHero)
                    return origin;

                var baseTroop =
                    troop.IsCaptain && troop.BaseTroop != null ? troop.BaseTroop : troop;

                var captain = baseTroop.Captain;
                if (captain == null)
                    return origin;

                var key = baseTroop.StringId;
                if (!_spawnCounts.TryGetValue(key, out var count))
                    count = 0;
                count++;
                _spawnCounts[key] = count;

                if (CaptainFrequency <= 0 || count % CaptainFrequency != 0)
                    return origin;

                Log.Info(
                    $"[Captains] AdjustOrigin: swapping {baseTroop.StringId} -> captain {captain.StringId}"
                );

                // Wrap the origin so Troop returns captain.Base
                return new CaptainAgentOrigin(origin, captain.Base);
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return origin;
            }
        }
    }
}
