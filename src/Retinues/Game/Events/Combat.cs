using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Events
{
    /// <summary>
    /// Combat event wrapper, tracks kills and provides helpers for logging kill details and combat reports.
    /// </summary>
    [SafeClass]
    public class Combat : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Info                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsVictory => Mission.Current?.MissionResult?.PlayerVictory == true;
        public bool IsDefeat => !IsVictory;

        public readonly List<Kill> Kills = [];

        /// <summary>
        /// Kill event details, including victim, killer, state, and blow.
        /// </summary>
        public class Kill(Agent victim, Agent killer, AgentState state, KillingBlow blow)
        {
            public WAgent Victim { get; } = victim != null ? new WAgent(victim) : null;
            public WAgent Killer { get; } = killer != null ? new WAgent(killer) : null;
            public AgentState State { get; } = state;
            public KillingBlow Blow { get; } = blow;

            public bool IsValid =>
                Killer?.Agent != null
                && Victim?.Agent != null
                && Killer.Agent.IsHuman
                && Victim.Agent.IsHuman
                && (State == AgentState.Killed || State == AgentState.Unconscious)
                && Killer.Agent.Character is CharacterObject
                && Victim.Agent.Character is CharacterObject;
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
            var logKills = false; // set to true for verbose logging of all kills

            if (kill.IsValid)
            {
                Kills.Add(kill);

                if (logKills)
                    LogKill(kill);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Logging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Log details of a single kill event.
        /// </summary>
        public void LogKill(Kill kill)
        {
            string victimTeam =
                kill.Victim.IsPlayer ? "Player"
                : kill.Victim.IsPlayerTroop ? "PlayerTroop"
                : kill.Victim.IsAllyTroop ? "Ally"
                : kill.Victim.IsEnemyTroop ? "Enemy"
                : "Neutral";
            string killerTeam =
                kill.Killer.IsPlayer ? "Player"
                : kill.Killer.IsPlayerTroop ? "PlayerTroop"
                : kill.Killer.IsAllyTroop ? "Ally"
                : kill.Killer.IsEnemyTroop ? "Enemy"
                : "Neutral";
            string action =
                kill.State == AgentState.Killed ? "killed"
                : kill.State == AgentState.Unconscious ? "downed"
                : "removed";

            Log.Info(
                $"{kill.Victim?.Character?.Name} ({victimTeam}) {action} by {kill.Killer?.Character?.Name} ({killerTeam})"
            );
        }

        /// <summary>
        /// Log a summary report of all kills and casualties in the combat.
        /// </summary>
        public void LogCombatReport()
        {
            int playerKills = 0;
            int playerTroopKills = 0;
            int allyKills = 0;
            int enemyKills = 0;
            int playerCasualties = 0;
            int playerTroopCasualties = 0;
            int allyCasualties = 0;
            int enemyCasualties = 0;
            int customKills = 0;
            int retinueKills = 0;

            foreach (var kill in Kills)
            {
                if (kill.Killer.IsPlayer)
                    playerKills++;
                if (kill.Killer.IsPlayerTroop)
                    playerTroopKills++;
                if (kill.Killer.IsAllyTroop)
                    allyKills++;
                if (kill.Killer.IsEnemyTroop)
                    enemyKills++;

                if (kill.Victim.IsPlayer)
                    playerCasualties++;
                if (kill.Victim.IsPlayerTroop)
                    playerTroopCasualties++;
                if (kill.Victim.IsAllyTroop)
                    allyCasualties++;
                if (kill.Victim.IsEnemyTroop)
                    enemyCasualties++;

                if (kill.Killer.Character?.IsCustom == true)
                    customKills++;
                if (kill.Killer.Character?.IsRetinue == true)
                    retinueKills++;
            }

            Log.Debug($"--- Combat Report ---");
            Log.Debug($"Kills: {Kills.Count} total");
            Log.Debug($"PlayerKills = {playerKills}");
            Log.Debug($"PlayerTroopKills = {playerTroopKills}");
            Log.Debug($"AllyKills = {allyKills}");
            Log.Debug($"EnemyKills = {enemyKills}");
            Log.Debug($"---------------------");
            Log.Debug($"Casualties: {Kills.Count} total");
            Log.Debug($"PlayerCasualties = {playerCasualties}");
            Log.Debug($"PlayerTroopCasualties = {playerTroopCasualties}");
            Log.Debug($"AllyCasualties = {allyCasualties}");
            Log.Debug($"EnemyCasualties = {enemyCasualties}");
            Log.Debug($"---------------------");
            Log.Debug($"CustomKills = {customKills}");
            Log.Debug($"RetinueKills = {retinueKills}");
            Log.Debug($"---------------------");
        }
    }
}
