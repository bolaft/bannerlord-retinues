using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

// ReSharper disable once CheckNamespace
namespace Retinues.Migration.Shims
{
    /// <summary>
    /// Read-only shim for v1 <c>AutoJoinBehavior</c>.
    /// Deserializes retinue hire caps (which cultures' retinues are unlocked)
    /// and renown reserve bookkeeping from old saves.
    /// </summary>
    internal sealed class AutoJoinBehavior : CampaignBehaviorBase
    {
        /// <summary>
        /// cultureId → hire cap.  Any entry with value &gt; 0 means the player
        /// had unlocked that culture's retinue in v1.
        /// </summary>
        internal Dictionary<string, int> HireCaps;

        // Not migrated; read to avoid orphaning the partition.
        internal float RenownReserve;
        internal float LastRenown;

        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_RetinueHire_Caps", ref HireCaps);
            dataStore.SyncData("Retinues_RetinueHire_RenownReserve", ref RenownReserve);
            dataStore.SyncData("Retinues_RetinueHire_LastRenown", ref LastRenown);
        }
    }
}
