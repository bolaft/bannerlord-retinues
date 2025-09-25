using System;
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

        public bool IsVictory => Mission.Current?.MissionResult?.PlayerVictory == true;
        public bool IsDefeat => !IsVictory;

        public readonly List<Kill> Kills = [];

        public class Kill(Agent victim, Agent killer, AgentState state, KillingBlow blow)
        {
            public WAgent Victim { get; } = victim != null ? new WAgent(victim) : null;
            public WAgent Killer { get; } = killer != null ? new WAgent(killer) : null;
            public AgentState State { get; } = state;
            public KillingBlow Blow { get; } = blow;

            public bool IsValid =>
                Killer?.Agent != null &&
                Victim?.Agent != null &&
                Killer.Agent.IsHuman && Victim.Agent.IsHuman &&
                (State == AgentState.Killed || State == AgentState.Unconscious) &&
                Killer.Agent.Character is CharacterObject &&
                Victim.Agent.Character is CharacterObject;
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
            try
            {
                var kill = new Kill(victim, killer, state, blow);
                var logKills = false; // set to true for verbose logging of all kills

                if (kill.IsValid)
                {
                    Kills.Add(kill);

                    if (logKills)
                        LogKill(kill);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Logging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void LogKill(Kill kill)
        {
            string victimTeam = kill.Victim.IsPlayer ? "Player" : kill.Victim.IsPlayerTroop ? "PlayerTroop" : kill.Victim.IsAllyTroop ? "Ally" : kill.Victim.IsEnemyTroop ? "Enemy" : "Neutral";
            string killerTeam = kill.Killer.IsPlayer ? "Player" : kill.Killer.IsPlayerTroop ? "PlayerTroop" : kill.Killer.IsAllyTroop ? "Ally" : kill.Killer.IsEnemyTroop ? "Enemy" : "Neutral";
            string action = kill.State == AgentState.Killed ? "killed" : kill.State == AgentState.Unconscious ? "downed" : "removed";

            Log.Info(
                $"{kill.Victim?.Character?.Name} ({victimTeam}) {action} by {kill.Killer?.Character?.Name} ({killerTeam})"
            );
        }

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
