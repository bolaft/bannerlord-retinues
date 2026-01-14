using System.Collections.Generic;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.Game.Missions;
using Retinues.Utilities;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Base class for concrete feat behaviors that listen to campaign events and award feat progress.
    /// Also dispatches custom "battle over" events with kill snapshots.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract class FeatCampaignBehavior : BaseCampaignBehavior
    {
        protected abstract string FeatId { get; }

        public override bool IsActive => FeatsAPI.CanProgress(FeatId);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Custom Events                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Custom hook fired when a player-involved map battle starts and both MMission.Current and
        /// MMapEvent.Current are available.
        /// </summary>
        protected virtual void OnBattleStart(MMapEvent battle) { }

        protected override void OnMapEventStarted(MMapEvent mapEvent)
        {
            if (mapEvent == null)
                return;

            if (!mapEvent.IsPlayerInvolved)
                return;

            OnBattleStart(mapEvent);
        }

        /// <summary>
        /// Custom hook fired when a player-involved map battle ends and both MMission.Current and
        /// MMapEvent.Current are available.
        /// </summary>
        protected virtual void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        ) { }

        protected override void OnMapEventEnded(MMapEvent mapEvent)
        {
            if (mapEvent?.IsPlayerInvolved != true)
                return;

            var start = CombatBehavior.Snapshot;
            var end = mapEvent;

            if (start == null || end == null)
            {
                Log.Warn("Skipping OnBattleOver: missing battle snapshots.");
                return;
            }

            var kills = CombatBehavior.GetKills();
            OnBattleOver(kills, start, end);
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

            if (battle.IsSiegeBattle && !eq.SiegeBattleSet)
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
        protected void Progress(int amount = 1)
        {
            if (FeatsAPI.TryAddProgress(FeatId, amount))
                Log.Debug($"Adding {amount} progress to feat '{FeatId}'");
        }

        /// <summary>
        /// Resets the feat progress and completion flag.
        /// </summary>
        protected void Reset()
        {
            if (FeatsAPI.TryReset(FeatId))
                Log.Debug($"Resetting feat '{FeatId}'");
        }

        /// <summary>
        /// Sets the feat progress to the specified value, clamped to [0, target].
        /// </summary>
        protected void SetProgress(int amount)
        {
            if (amount < FeatsAPI.GetProgress(FeatId))
                return; // Do not decrease progress.

            if (FeatsAPI.TrySet(FeatId, amount))
                Log.Debug($"Set progress of feat '{FeatId}' to {amount}");
        }
    }
}
