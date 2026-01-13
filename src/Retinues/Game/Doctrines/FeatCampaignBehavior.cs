using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using TaleWorlds.Core;
using TaleWorlds.Localization;
// Alias so child classes can write IReadOnlyList<Kill> naturally if they also add the same alias.
// (The actual type is MMission.Kill.)
using Kill = Retinues.Domain.Events.Models.MMission.Kill;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Base class for concrete feat behaviors that listen to campaign events and award feat progress.
    /// Also dispatches custom "battle over" events with kill snapshots.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract class FeatCampaignBehavior : BaseCampaignBehavior
    {
        public override bool IsEnabled =>
            Settings.EnableDoctrines && Settings.EnableFeatRequirements;

        protected abstract string FeatId { get; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Custom Events                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Custom hook fired when a player-involved map battle starts and both MMission.Current and
        /// MMapEvent.Current are available.
        /// </summary>
        protected virtual void OnBattleStart(MMapEvent battle) { }

        protected override void OnMapEventStarted(
            MMapEvent mapEvent,
            WParty attackerParty,
            WParty defenderParty
        )
        {
            if (!IsEnabled)
                return;

            var mission = MMission.Current;
            var currentBattle = MMapEvent.Current;

            if (mission == null || currentBattle == null)
                return;

            if (mapEvent?.IsPlayerInvolved != true)
                return;

            // Optional strict check: only fire when the started event matches Current.
            if (mapEvent.Base != null && currentBattle.Base != null)
            {
                if (!ReferenceEquals(mapEvent.Base, currentBattle.Base))
                    return;
            }

            OnBattleStart(mapEvent);
        }

        /// <summary>
        /// Custom hook fired when a player-involved map battle ends and both MMission.Current and
        /// MMapEvent.Current are available.
        /// </summary>
        protected virtual void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle) { }

        protected override void OnMapEventEnded(MMapEvent mapEvent)
        {
            if (!IsEnabled)
                return;

            // Require both "Current" contexts, as requested.
            var mission = MMission.Current;
            var currentBattle = MMapEvent.Current;

            if (mission == null || currentBattle == null)
                return;

            // Use the actual ended event as "battle" for the check.
            // This also matches your requirement: only if battle.IsPlayerInvolved == true.
            if (mapEvent?.IsPlayerInvolved != true)
                return;

            // If you want to be extra strict: only fire when the ended event matches Current.
            // (Safe guard against stale Current pointers.)
            if (mapEvent.Base != null && currentBattle.Base != null)
            {
                if (!ReferenceEquals(mapEvent.Base, currentBattle.Base))
                    return;
            }

            var kills = mission.Kills ?? [];

            OnBattleOver(kills, mapEvent);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Kill Filtering                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Dummy model used for kill filtering. Extend it freely.
        /// </summary>
        protected sealed class AgentModel
        {
            public string CharacterId { get; }
            public WCharacter Character { get; }

            public bool IsPlayerTroop { get; }
            public bool IsAllyTroop { get; }
            public bool IsEnemyTroop { get; }

            public bool IsPlayerCharacter { get; }

            public bool IsCustom { get; }
            public bool IsDead { get; }

            internal AgentModel(
                string characterId,
                WCharacter character,
                bool isPlayerTroop,
                bool isAllyTroop,
                bool isEnemyTroop,
                bool isPlayerCharacter,
                bool isCustom,
                bool isDead
            )
            {
                CharacterId = characterId;
                Character = character;

                IsPlayerTroop = isPlayerTroop;
                IsAllyTroop = isAllyTroop;
                IsEnemyTroop = isEnemyTroop;

                IsPlayerCharacter = isPlayerCharacter;
                IsCustom = isCustom;
                IsDead = isDead;
            }
        }

        /// <summary>
        /// Helper for filtering kill snapshots using derived "agent-ish" models
        /// for killer and victim (player troop, enemy troop, etc.).
        /// </summary>
        protected static KillFilter Filter(
            System.Func<AgentModel, bool> killers = null,
            System.Func<AgentModel, bool> victims = null,
            System.Func<Kill, bool> kills = null
        )
        {
            return new KillFilter(killers, victims, kills);
        }

        /// <summary>
        /// Filters MMission.Kill snapshots by killer/victim predicates.
        /// </summary>
        protected sealed class KillFilter
        {
            readonly System.Func<AgentModel, bool> _killer;
            readonly System.Func<AgentModel, bool> _victim;
            readonly System.Func<Kill, bool> _kill;

            internal KillFilter(
                System.Func<AgentModel, bool> killer,
                System.Func<AgentModel, bool> victim,
                System.Func<Kill, bool> kill
            )
            {
                _killer = killer;
                _victim = victim;
                _kill = kill;
            }

            public IEnumerable<Kill> Filter(IEnumerable<Kill> kills)
            {
                if (kills == null)
                    yield break;

                foreach (var k in kills)
                {
                    if (_kill != null && !_kill(k))
                        continue;

                    if (_killer != null && !_killer(BuildKiller(in k)))
                        continue;

                    if (_victim != null && !_victim(BuildVictim(in k)))
                        continue;

                    yield return k;
                }
            }

            static AgentModel BuildKiller(in Kill k)
            {
                return new AgentModel(
                    k.KillerCharacterId,
                    k.Killer,
                    isPlayerTroop: k.KillerIsPlayerTroop,
                    isAllyTroop: k.KillerIsAllyTroop,
                    isEnemyTroop: k.KillerIsEnemyTroop,
                    isPlayerCharacter: k.KillerIsPlayer,
                    isCustom: k.Killer.InCustomTree,
                    isDead: k.State == AgentState.Killed
                );
            }

            static AgentModel BuildVictim(in Kill k)
            {
                return new AgentModel(
                    k.VictimCharacterId,
                    k.Victim,
                    isPlayerTroop: k.VictimIsPlayerTroop,
                    isAllyTroop: k.VictimIsAllyTroop,
                    isEnemyTroop: k.VictimIsEnemyTroop,
                    isPlayerCharacter: k.VictimIsPlayer,
                    isCustom: k.Victim.InCustomTree,
                    isDead: k.State == AgentState.Killed
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Battle Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected static bool IsValidForBattle(MEquipment eq, MMapEvent battle)
        {
            if (eq.IsCivilian)
                return false;

            if (battle.IsFieldBattle && !eq.FieldBattleSet)
                return false;

            if (battle.IsSiege && !eq.SiegeBattleSet)
                return false;

            if (battle.IsNavalBattle && !eq.NavalBattleSet)
                return false;

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Progress Helpers                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds feat progress, completing the feat if it reaches the target.
        /// </summary>
        protected void Progress(int amount)
        {
            FeatsAPI.TryAddProgress(FeatId, amount);
        }

        /// <summary>
        /// Resets the feat progress and completion flag.
        /// </summary>
        protected void Reset()
        {
            FeatsAPI.TryReset(FeatId);
        }

        /// <summary>
        /// Sets the feat progress to the specified value, clamped to [0, target].
        /// </summary>
        protected void SetProgress(int amount)
        {
            FeatsAPI.TrySet(FeatId, amount);
        }
    }
}
