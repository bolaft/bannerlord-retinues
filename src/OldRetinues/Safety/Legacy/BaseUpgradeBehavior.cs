using System.Collections.Generic;
using Retinues.Features.Staging;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
# endif

namespace OldRetinues.Safety.Legacy
{
    /// <summary>
    /// Base class for legacy upgrade behaviors (equip/train) migration.
    /// </summary>
    [SafeClass]
    public abstract class BaseUpgradeBehavior<T> : CampaignBehaviorBase
        where T : IPendingData
    {
        public static BaseUpgradeBehavior<T> Instance { get; private set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>troopId → (objectKey → data)</summary>
        public Dictionary<string, Dictionary<string, T>> _pending = [];
        public Dictionary<string, Dictionary<string, T>> Pending => _pending;

        protected abstract string SaveFieldName { get; set; }

        public override void SyncData(IDataStore data)
        {
            if (!data.IsLoading)
                return; // only care about loading

            // Ensure Instance is set while the legacy data is being loaded
            Instance = this;

            _pending ??= []; // Guard against null reference
            _pending?.Clear(); // to avoid cross-save contamination

            data.SyncData(SaveFieldName, ref _pending);

            _pending ??= []; // in case it was null in the save

            Log.Info($"Legacy migration: {_pending.Count} troops with staged jobs.");
            Log.Dump(_pending, LogLevel.Debug);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this,
                OnGameLoadFinished
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected abstract void OnGameLoadFinished();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Replace all pending entries for a troop from oldTroopId to newTroopId
        /// in the legacy migration dictionary.
        /// </summary>
        public static void ReplacePendingKey(string oldTroopId, string newTroopId)
        {
            var instance = Instance;
            if (instance == null)
                return;

            if (string.IsNullOrEmpty(oldTroopId) || string.IsNullOrEmpty(newTroopId))
                return;

            if (!instance._pending.TryGetValue(oldTroopId, out var dict) || dict == null)
                return;

            instance._pending.Remove(oldTroopId);
            instance._pending[newTroopId] = dict;

            // Keep payloads consistent
            foreach (var value in dict.Values)
                ((IPendingData)value).TroopId = newTroopId;
        }
    }

    [SafeClass]
    public sealed class TroopEquipBehavior : BaseUpgradeBehavior<PendingEquipData>
    {
        protected override string SaveFieldName { get; set; } = "Retinues_Equip_Pending";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnGameLoadFinished()
        {
            Log.Info("Legacy migration: TroopEquipBehavior.OnGameLoadFinished called.");

            if (_pending == null || _pending.Count == 0)
            {
                Log.Info("Legacy migration: no legacy equip jobs to migrate.");
                return;
            }

            int troops = 0;
            int imported = 0;
            int skipped = 0;

            foreach (var troopEntry in _pending)
            {
                var troopId = troopEntry.Key;
                var byKey = troopEntry.Value;

                if (string.IsNullOrEmpty(troopId) || byKey == null || byKey.Count == 0)
                    continue;

                foreach (var kv in byKey)
                {
                    var objectKey = kv.Key;
                    var data = kv.Value;

                    if (string.IsNullOrEmpty(objectKey) || data == null)
                        continue;

                    Log.Info($"Migrating equip job for troop {troopId}, object {objectKey}");
                    // Migrate to EquipStagingBehavior
                    EquipStagingBehavior.Instance.SetPending(troopId, objectKey, data);

                    imported++;
                }

                troops++;
            }

            _pending.Clear();

            Log.Info(
                $"Legacy migration: migrated {imported} equip job(s) for {troops} troop(s) "
                    + $"into EquipStagingBehavior (skipped {skipped} duplicate key(s))."
            );
        }
    }

    [SafeClass]
    public sealed class TroopTrainBehavior : BaseUpgradeBehavior<PendingTrainData>
    {
        protected override string SaveFieldName { get; set; } = "Retinues_Train_Pending";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnGameLoadFinished()
        {
            Log.Info("Legacy migration: TroopTrainBehavior.OnGameLoadFinished called.");

            if (_pending == null || _pending.Count == 0)
            {
                Log.Info("Legacy migration: no legacy train jobs to migrate.");
                return;
            }

            int troops = 0;
            int imported = 0;
            int skipped = 0;

            foreach (var troopEntry in _pending)
            {
                var troopId = troopEntry.Key;
                var byKey = troopEntry.Value;

                foreach (var kv in byKey)
                {
                    var objectKey = kv.Key; // skillId
                    var data = kv.Value;

                    if (string.IsNullOrEmpty(objectKey) || data == null)
                        continue;

                    Log.Info($"Migrating train job for troop {troopId}, skill {objectKey}");
                    // Migrate to TrainStagingBehavior
                    TrainStagingBehavior.Instance.SetPending(troopId, objectKey, data);

                    imported++;
                }

                troops++;
            }

            _pending.Clear();

            Log.Info(
                $"Legacy migration: migrated {imported} train job(s) for {troops} troop(s) "
                    + $"into TrainStagingBehavior (skipped {skipped} duplicate key(s))."
            );
        }
    }
}
