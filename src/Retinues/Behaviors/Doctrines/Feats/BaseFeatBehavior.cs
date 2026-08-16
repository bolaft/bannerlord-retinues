using System.Collections.Generic;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.MapEvents;

namespace Retinues.Behaviors.Doctrines.Feats
{
    /// <summary>
    /// Base class for concrete feat behaviors that listen to campaign events and award feat progress.
    /// Also dispatches custom "battle over" events with kill snapshots.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract class BaseFeatBehavior : BaseCampaignBehavior
    {
        public override bool IsActive => Feat?.IsInProgress == true;

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

        protected override void OnPartyAddedToMapEvent(WParty party)
        {
            var mapEvent = party?.Base.MapEvent;
            if (mapEvent?.State != MapEventState.Wait || !mapEvent.IsPlayerMapEvent)
                return;

            // path for joining an existing ai battle does not include MapEventStarted. this will leave feats relying on OnBattleStart in a state from before the player joined.
            // run the start checks again as parties are attached, allowing the feat progress to reflect the current player battle
            OnBattleStart(new MMapEvent(mapEvent));
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

            // CombatBehavior retains the previous player battle information after mission end and this isn't replaced for simulated battles
            // without matching the map event every next autoresolve battle will count the previous battle progress again for feat progress until a new mission is opened.
            // if autoresolve is meant to contribute to feats, add MMapEvent snapshot for a MapEvent and feats progress by kills can use SkillLevelingManager.OnSimulationCombatKill
            if (CombatBehavior.MapEvent != mapEvent)
                return;

            var start = CombatBehavior.Snapshot;
            if (start == null)
            {
                Log.Warning("Skipping OnBattleOver: missing battle snapshots.");
                return;
            }

            var kills = CombatBehavior.GetKills() ?? [];
            OnBattleOver(kills, start, mapEvent);
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
