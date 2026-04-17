using System.Collections.Generic;
using Retinues.Migration.Legacy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

// Class name MUST stay "FactionBehavior" – BL matches saved partition by class
// name (GetType().Name), so renaming would break loading legacy saves.
// ReSharper disable once CheckNamespace
namespace Retinues.Migration.Shims
{
    /// <summary>
    /// Read-only shim for v1 <c>FactionBehavior</c>.
    /// Deserializes clan/kingdom/culture/minor-clan troop data from old saves.
    /// </summary>
    internal sealed class FactionBehavior : CampaignBehaviorBase
    {
        // populated by SyncData when loading a v1 save
        internal FactionSaveData ClanTroops;
        internal FactionSaveData KingdomTroops;
        internal List<FactionSaveData> CultureTroops;
        internal List<FactionSaveData> MinorClanTroops;

        public override void RegisterEvents() { /* read-only shim */
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_ClanTroops", ref ClanTroops);
            dataStore.SyncData("Retinues_KingdomTroops", ref KingdomTroops);
            dataStore.SyncData("Retinues_CultureTroops", ref CultureTroops);
            dataStore.SyncData("Retinues_MinorClanTroops", ref MinorClanTroops);
        }
    }
}
