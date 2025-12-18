using System;
using System.Collections.Generic;
using Retinues.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Model
{
    /// <summary>
    /// Saves/loads persistent MAttribute values into a single string dictionary.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public sealed class MBehavior : BaseCampaignBehavior
    {
        private const string StoreId = "retinues_persistent_attributes_v1";

        private Dictionary<string, string> _store = new(StringComparer.Ordinal);

        public override void RegisterEvents()
        {
            // Safety: applying again after load is cheap and catches late-registered attributes.
            Hook(BehaviorEvent.GameLoadFinished, () => MPersistence.ApplyLoadedEager());
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                MPersistence.AttachStore(_store);

                // Ensure the in-memory store contains the latest dirty values before saving.
                MPersistence.FlushDirty();

                dataStore.SyncData(StoreId, ref _store);

                foreach (var kvp in _store)
                    Log.Debug($"Persisted Attribute: {kvp.Key} = {kvp.Value}");

                // If this was a load, _store is now populated. Attach and apply.
                MPersistence.AttachStore(_store);
            }
            catch (Exception e)
            {
                Log.Exception(e, "MAttributePersistenceBehavior.SyncData failed.");
            }
        }
    }
}
