using System.Collections.Generic;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Game.Wrappers.Cache;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Features.Xp.Behaviors
{
    /// Tracks kills done by custom troops during a mission,
    public sealed class TroopXpMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private readonly Dictionary<WCharacter, int> _xpByTroop = [];

        public override void OnAgentRemoved(
            Agent victim,
            Agent killer,
            AgentState state,
            KillingBlow blow
        )
        {
            if (killer == null || victim == null)
                return;
            if (!killer.IsHuman || !victim.IsHuman)
                return;
            if (state is not AgentState.Killed and not AgentState.Unconscious)
                return; // count both if you prefer
            if (
                killer.Character is not CharacterObject kChar
                || victim.Character is not CharacterObject vChar
            )
                return;
            if (!killer.Team?.IsPlayerTeam == true)
                return; // player-side only

            var w = WCharacterCache.Wrap(kChar);
            if (!w.IsCustom)
                return;

            int xp = ComputeKillXp(vChar);
            if (xp <= 0)
                return;

            _xpByTroop.TryGetValue(w, out var cur);
            _xpByTroop[w] = cur + xp;
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

        private const int XpPerTier = 5;

        private static int ComputeKillXp(CharacterObject victim)
        {
            int tier = victim.Tier;
            return (tier + 1) * XpPerTier;
        }
    }
}
