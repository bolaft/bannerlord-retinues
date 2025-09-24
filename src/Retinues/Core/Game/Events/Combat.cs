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

        public void LogKill(Kill kill)
        {
            string victimTeam = kill.Victim.IsPlayer ? "Player" : kill.Victim.IsPlayerTroop ? "PlayerTroop" : kill.Victim.IsAllyTroop ? "Ally" : kill.Victim.IsEnemyTroop ? "Enemy" : "Neutral";
            string killerTeam = kill.Killer.IsPlayer ? "Player" : kill.Killer.IsPlayerTroop ? "Player" : kill.Killer.IsAllyTroop ? "Ally" : kill.Killer.IsEnemyTroop ? "Enemy" : "Neutral";
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
