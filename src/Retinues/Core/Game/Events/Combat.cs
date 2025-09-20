using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Events
{
    public class Combat : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Info                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly List<Kill> Kills = [];

        public class Kill(Agent victim, Agent killer, AgentState state, KillingBlow blow)
        {
            public WAgent Victim = new(victim);
            public WAgent Killer = new(killer);
            public AgentState State = state;
            public KillingBlow Blow = blow;

            public bool IsValid
            {
                get
                {
                    if (killer == null || victim == null)
                        return false;
                    if (!killer.IsHuman || !victim.IsHuman)
                        return false;
                    if (State is not AgentState.Killed and not AgentState.Unconscious)
                        return false;
                    if (
                        killer.Character is not CharacterObject
                        || victim.Character is not CharacterObject
                    )
                        return false;

                    return true;
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Mission Events                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void OnAgentRemoved(
            Agent victim,
            Agent killer,
            AgentState state,
            KillingBlow blow
        )
        {
            var kill = new Kill(victim, killer, state, blow);

            if (kill.IsValid)
                Kills.Add(kill);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Logging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void LogCombatReport()
        {
            Log.Debug($"--- Combat Report ---");
            Log.Debug($"Kills: {Kills.Count} total");
            Log.Debug($"PlayerKills = {Kills.Where(k => k.Killer.IsPlayer).Count()}");
            Log.Debug($"CustomKills = {Kills.Where(k => k.Killer.Character.IsCustom).Count()}");
            Log.Debug($"RetinueKills = {Kills.Where(k => k.Killer.Character.IsRetinue).Count()}");
            Log.Debug($"---------------------");
        }
    }
}
