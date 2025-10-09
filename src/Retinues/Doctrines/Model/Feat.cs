using System;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Model
{
    /// <summary>
    /// Base class for doctrine feats. Provides metadata, progress tracking, registration, and event hooks.
    /// </summary>
    public abstract class Feat
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Metadata                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public abstract TextObject Description { get; }
        public virtual int Target => 0;

        // Type-based key used internally/persisted
        public string Key => GetType().FullName;

        // Owning doctrine type (assumes nested class)
        public Type DoctrineType => GetType().DeclaringType;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Progress Tracking                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int Progress => DoctrineAPI.GetFeatProgress(GetType());

        /// <summary>
        /// Set progress for this feat.
        /// </summary>
        protected void SetProgress(int progress)
        {
            Log.Info($"Setting progress of feat {GetType().FullName} to {progress}");
            DoctrineAPI.SetFeatProgress(GetType(), progress);
        }

        /// <summary>
        /// Advance progress for this feat by amount.
        /// </summary>
        protected int AdvanceProgress(int amount = 1)
        {
            Log.Info($"Advancing progress of feat {GetType().FullName} by {amount}");
            return DoctrineAPI.AdvanceFeat(GetType(), amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Registration                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public virtual void OnRegister() { }

        public virtual void OnUnregister() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Event Hooks                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public virtual void OnDailyTick() { }

        public virtual void OnBattleEnd(Battle battle) { }

        public virtual void OnBattleStart(Battle battle) { }

        public virtual void OnTournamentStart(Tournament tournament) { }

        public virtual void OnTournamentFinished(Tournament tournament) { }

        public virtual void OnSettlementOwnerChanged(SettlementOwnerChange change) { }

        public virtual void OnQuestCompleted(Quest quest) { }

        public virtual void OnTroopRecruited(WCharacter troop, int amount) { }

        public virtual void PlayerUpgradedTroops(
            WCharacter upgradeFromTroop,
            WCharacter upgradeToTroop,
            int number
        ) { }

        public virtual void OnArenaStart(Combat combat) { }

        public virtual void OnArenaEnd(Combat combat) { }
    }
}
