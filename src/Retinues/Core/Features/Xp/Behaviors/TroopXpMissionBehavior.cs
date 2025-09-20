using System.Collections.Generic;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Game.Wrappers.Cache;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Features.Xp.Behaviors
{
    public sealed class TroopXpMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void OnAgentRemoved(
            Agent victim,
            Agent killer,
            AgentState state,
            KillingBlow blow
        )
        {
            var kill = new Combat.Kill(victim, killer, state, blow);

            if (!kill.IsValid)
                return;

            if (!kill.Killer.IsPlayerTroop)
                return; // player-side only

            if (!kill.Killer.Character.IsCustom)
                return;

            int xp = ComputeKillXp(kill.Victim);
            if (xp <= 0)
                return;

            _xpByTroop.TryGetValue(kill.Killer.Character, out var current);
            _xpByTroop[kill.Killer.Character] = current + xp;
        }

        protected override void OnEndMission()
        {
            if (_xpByTroop.Count == 0)
                return;
            foreach (var kv in _xpByTroop)
                Log.Info($"  {kv.Key.Name}: {kv.Value} XP");
            TroopXpService.AccumulateFromMission(_xpByTroop);
            _xpByTroop.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const int XpPerTier = 5;

        private readonly Dictionary<WCharacter, int> _xpByTroop = [];

        private static int ComputeKillXp(WAgent victim)
        {
            int tier = victim.Character.Tier;
            return (tier + 1) * XpPerTier;
        }
    }
}
