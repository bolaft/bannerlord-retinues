using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

// ReSharper disable once CheckNamespace
namespace Retinues.Migration.Shims
{
    /// <summary>
    /// Read-only shim for v1 <c>VersionBehavior</c>.
    /// Reads the Retinues mod version that was stored in the legacy save.
    /// </summary>
    internal sealed class VersionBehavior : CampaignBehaviorBase
    {
        /// <summary>Version string stored by v1, e.g. "v1.2.12.9".</summary>
        internal string SavedVersion;

        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_Version", ref SavedVersion);
        }
    }
}
