using Retinues.Migration.Shims;
using TaleWorlds.CampaignSystem;

namespace Retinues.Migration
{
    /// <summary>
    /// Registers all migration shims and the coordinator with a
    /// <see cref="CampaignGameStarter"/>.  Call this <strong>before</strong>
    /// <c>BehaviorManager.RegisterCampaignBehaviors</c> so that shim
    /// <c>SyncData</c> is called before the coordinator's
    /// <c>OnGameLoadFinished</c> listener fires.
    /// </summary>
    internal static class LegacyMigrationRegistrar
    {
        internal static void Register(CampaignGameStarter starter)
        {
            var faction = new FactionBehavior();
            var xp = new TroopXpBehavior();
            var stats = new TroopStatisticsBehavior();
            var stocks = new StocksBehavior();
            var unlocks = new UnlocksBehavior();
            var doctrines = new DoctrineServiceBehavior();
            var autoJoin = new AutoJoinBehavior();
            var version = new VersionBehavior();

            var coordinator = new LegacyMigrationCoordinator(
                faction,
                xp,
                stats,
                stocks,
                unlocks,
                doctrines,
                autoJoin,
                version
            );

            // Shims first so they receive SyncData before the coordinator acts.
            starter.AddBehavior(faction);
            starter.AddBehavior(xp);
            starter.AddBehavior(stats);
            starter.AddBehavior(stocks);
            starter.AddBehavior(unlocks);
            starter.AddBehavior(doctrines);
            starter.AddBehavior(autoJoin);
            starter.AddBehavior(version);
            starter.AddBehavior(coordinator);
        }
    }
}
