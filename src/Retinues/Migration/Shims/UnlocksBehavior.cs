using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

// ReSharper disable once CheckNamespace
namespace Retinues.Migration.Shims
{
    /// <summary>
    /// Read-only shim for v1 <c>UnlocksBehavior</c>.
    /// Deserializes item unlock state and progress from old saves.
    /// </summary>
    internal sealed class UnlocksBehavior : CampaignBehaviorBase
    {
        internal List<string> UnlockedItemIds;
        internal Dictionary<string, int> ProgressByItemId;

        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            // Read-only shim: never write legacy data back, so a migrated save drops these
            // partitions on its next save and migration does not re-fire on later loads.
            if (dataStore.IsSaving)
                return;

            dataStore.SyncData("Retinues_Unlocks_Unlocked", ref UnlockedItemIds);
            dataStore.SyncData("Retinues_Unlocks_Progress", ref ProgressByItemId);
        }
    }
}
