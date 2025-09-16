using System;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines
{
    public abstract class Feat
    {
        // Minimal metadata you define in each feat class
        public abstract string Description { get; }
        public virtual int Target => 0; // 0 => trivially complete

        // Type-based key used internally/persisted
        public string Key => GetType().FullName;

        // Owning doctrine type (assumes nested class)
        public Type DoctrineType => GetType().DeclaringType;

        // ---- Progress tracking ----

        public int Progress => DoctrineAPI.GetFeatProgress(GetType());

        protected void SetProgress(int progress)
        {
            Log.Info($"Setting progress of feat {GetType().FullName} to {progress}");
            DoctrineAPI.SetFeatProgress(GetType(), progress);
        }

        protected int AdvanceProgress(int amount = 1)
        {
            Log.Info($"Advancing progress of feat {GetType().FullName} by {amount}");
            return DoctrineAPI.AdvanceFeat(GetType(), amount);
        }

        // ---- Hooks ----

        public virtual void OnRegister() { }

        public virtual void OnUnregister() { }

        // ---- Event hooks ----

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
        )
        { }

        public virtual void OnArenaStart(Combat combat) { }
        public virtual void OnArenaEnd(Combat combat) { }
    }
}
