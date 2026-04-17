using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

// ReSharper disable once CheckNamespace
namespace Retinues.Migration.Shims
{
    /// <summary>
    /// Read-only shim for v1 <c>DoctrineServiceBehavior</c>.
    /// Deserializes unlocked doctrines and feat progress from old saves.
    /// </summary>
    internal sealed class DoctrineServiceBehavior : CampaignBehaviorBase
    {
        /// <summary>List of v1 doctrine keys (Type.FullName strings).</summary>
        internal List<string> UnlockedDoctrines;

        /// <summary>
        /// Per-feat progress keyed by Type.FullName.
        /// Migrated to v2 via <see cref="FeatKeyMap"/> on a best-effort basis.
        /// </summary>
        internal Dictionary<string, int> FeatProgress;

        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_Doctrines_Unlocked", ref UnlockedDoctrines);
            dataStore.SyncData("Retinues_Doctrines_FeatProgress", ref FeatProgress);
        }
    }
}
