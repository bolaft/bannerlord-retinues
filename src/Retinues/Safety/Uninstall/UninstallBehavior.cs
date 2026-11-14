using TaleWorlds.CampaignSystem;

namespace Retinues.Safety.Uninstall
{
    // Same behavior name/key on both branches. Must be registered before other Retinues behaviors.
    public sealed class UninstallBehavior : CampaignBehaviorBase
    {
        private bool _prepared;
        // Transient: failed cleanup must be abandoned by reloading the original save.
        internal bool CleanupFailed;
        internal static bool HasFailed => Campaign.Current?.CampaignBehaviorManager?.GetBehavior<UninstallBehavior>()?.CleanupFailed == true;
        public static bool IsPrepared => Campaign.Current?.CampaignBehaviorManager?.GetBehavior<UninstallBehavior>()?._prepared == true;

        public override void SyncData(IDataStore dataStore) => dataStore.SyncData("Retinues_UninstallPrepared", ref _prepared);

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, _ => ResumePreparedSave());
        }

        internal void Prepare()
        {
            _prepared = true;
            SuspendRetinues();
        }

        private void ResumePreparedSave()
        {
            if (!_prepared) return;
            SuspendRetinues();
            UninstallHooks.Log("[Uninstall] This save is prepared for removal; Retinues troop generation remains disabled.");
        }

        private void SuspendRetinues()
        {
            foreach (var behavior in Campaign.Current.CampaignBehaviorManager.GetBehaviors<CampaignBehaviorBase>())
                if (behavior != this && behavior.GetType().Assembly == GetType().Assembly)
                    CampaignEventDispatcher.Instance.RemoveListeners(behavior);
            UninstallHooks.DisableCustomTroops();
        }
    }
}
