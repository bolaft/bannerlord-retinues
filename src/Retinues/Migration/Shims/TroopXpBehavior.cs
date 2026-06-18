using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

// ReSharper disable once CheckNamespace
namespace Retinues.Migration.Shims
{
    /// <summary>
    /// Read-only shim for v1 <c>TroopXpBehavior</c>.
    /// Deserializes per-troop XP pool data from old saves.
    /// </summary>
    internal sealed class TroopXpBehavior : CampaignBehaviorBase
    {
        internal Dictionary<string, int> XpPools;

        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            // Read-only shim: never write legacy data back, so a migrated save drops these
            // partitions on its next save and migration does not re-fire on later loads.
            if (dataStore.IsSaving)
                return;

            dataStore.SyncData("Retinues_Xp_Pools", ref XpPools);
        }
    }
}
