using System.Collections.Generic;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.Utilities;

namespace Retinues.Behaviors.Doctrines.Feats
{
    /// <summary>
    /// Base class for concrete feat behaviors that listen to campaign events and award feat progress.
    /// Also dispatches custom "battle over" events with kill snapshots.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract class BaseFeatBehavior : BaseCampaignBehavior
    {
        public override bool IsActive => Feat.IsInProgress;

        protected abstract string FeatId { get; }
        protected Feat Feat => Feat.Get(FeatId);

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
                Log.Warning("Skipping OnBattleOver: missing battle snapshots.");
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
    }
}
