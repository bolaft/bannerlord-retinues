using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        /// Kill event details. Implemented as a readonly struct to avoid heap allocations
        /// and speed up creation when many kills are recorded.
        /// </summary>
        public readonly struct Kill
        {
            /* ━━━━━━ Fields (readonly for fast init) ━━━━━━ */

            public readonly bool KillerIsPlayer;
            public readonly bool KillerIsPlayerTroop;
            public readonly bool KillerIsAllyTroop;
            public readonly bool KillerIsEnemyTroop;
            public readonly bool VictimIsPlayer;
            public readonly bool VictimIsPlayerTroop;
            public readonly bool VictimIsAllyTroop;
            public readonly bool VictimIsEnemyTroop;
            public readonly string KillerCharacterId;
            public readonly string VictimCharacterId;
            public readonly string LootCode;
            public readonly AgentState State;
            public readonly bool IsMissile;
            public readonly bool IsHeadShot;
            public readonly int BlowWeaponClass;

            /* ━━━━━━ Convenience (create wrappers on-demand) ━━━━━ */

            public WCharacter Killer => new(KillerCharacterId);
            public WCharacter Victim => new(VictimCharacterId);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Kill(Agent victim, Agent killer, AgentState state, KillingBlow blow)
            {
                // Inline simple checks to avoid delegate/virtual overhead
                KillerIsPlayer = killer?.IsPlayerControlled == true;
                KillerIsPlayerTroop =
                    killer?.IsAIControlled == true && killer?.Team?.IsPlayerTeam == true;
                KillerIsAllyTroop =
                    killer?.Team?.IsPlayerAlly == true && !KillerIsPlayer && !KillerIsPlayerTroop;
                KillerIsEnemyTroop = killer?.Team?.IsEnemyOf(Mission.Current?.PlayerTeam) == true;

                VictimIsPlayer = victim?.IsPlayerControlled == true;
                VictimIsPlayerTroop =
                    victim?.IsAIControlled == true && victim?.Team?.IsPlayerTeam == true;
                VictimIsAllyTroop =
                    victim?.Team?.IsPlayerAlly == true && !VictimIsPlayer && !VictimIsPlayerTroop;
                VictimIsEnemyTroop = victim?.Team?.IsEnemyOf(Mission.Current?.PlayerTeam) == true;

                KillerCharacterId = killer?.Character?.StringId;
                VictimCharacterId = victim?.Character?.StringId;
                LootCode = victim?.SpawnEquipment?.CalculateEquipmentCode();

                State = state;
                IsMissile = blow.IsMissile;
                IsHeadShot = blow.IsHeadShot();
                BlowWeaponClass = blow.WeaponClass;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsValid(Agent victim, Agent killer, AgentState state)
            {
                return killer is not null
                    && victim is not null
                    && killer.IsHuman
                    && victim.IsHuman
                    && (state == AgentState.Killed || state == AgentState.Unconscious)
                    && killer.Character is CharacterObject
                    && victim.Character is CharacterObject;
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
            if (Kill.IsValid(victim, killer, state))
                Kills.Add(new Kill(victim, killer, state, blow));
        }

        public sealed override void OnEndMissionInternal()
        {
            // Derived classes must override OnEndMission instead.
            base.OnEndMissionInternal();

            if (Kills.Count > 0)
                Kills.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Logging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

            foreach (var kill in Kills)
            {
                if (kill.KillerIsPlayer)
                    playerKills++;
                if (kill.KillerIsPlayerTroop)
                    playerTroopKills++;
                if (kill.KillerIsAllyTroop)
                    allyKills++;
                if (kill.KillerIsEnemyTroop)
                    enemyKills++;

                if (kill.VictimIsPlayer)
                    playerCasualties++;
                if (kill.VictimIsPlayerTroop)
                    playerTroopCasualties++;
                if (kill.VictimIsAllyTroop)
                    allyCasualties++;
                if (kill.VictimIsEnemyTroop)
                    enemyCasualties++;
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
        }
    }
}
