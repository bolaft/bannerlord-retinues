using System.Collections.Generic;
using Retinues.Migration.Legacy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

// ReSharper disable once CheckNamespace
namespace Retinues.Migration.Shims
{
    /// <summary>
    /// Read-only shim for v1 <c>TroopStatisticsBehavior</c>.
    /// Deserializes per-troop combat statistics from old saves.
    /// </summary>
    internal sealed class TroopStatisticsBehavior : CampaignBehaviorBase
    {
        internal Dictionary<string, TroopCombatStats> Stats;

        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            // Read-only shim: never write legacy data back, so a migrated save drops these
            // partitions on its next save and migration does not re-fire on later loads.
            if (dataStore.IsSaving)
                return;

            dataStore.SyncData("_retinuesTroopStatistics", ref Stats);
        }
    }
}
