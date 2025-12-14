using System.Collections.Generic;
using Retinues.Game;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace OldRetinues.Safety.Legacy
{
    /// <summary>
    /// Handles migration of legacy troop save data to the current format.
    /// </summary>
    public class TroopBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private List<LegacyTroopSaveData> _troopData;
        public List<LegacyTroopSaveData> TroopData => _troopData ??= [];

        public override void SyncData(IDataStore ds)
        {
            if (ds.IsSaving)
                return; // No migration needed when saving

            // Load legacy troop save data
            ds.SyncData("Retinues_Troops_Data", ref _troopData);

            // Migrate each legacy troop to the current format
            if (TroopData.Count == 0)
            {
                Log.Debug("No legacy custom roots found in save.");
                return;
            }

            var (clanSaveData, kingdomSaveData) = LegacyTroopSaveConverter.ConvertLegacyFactionData(
                TroopData
            );

            Log.Info("Applying migrated troop data to factions...");

            // Apply migrated data back to factions
            clanSaveData.Apply(Player.Clan);
            kingdomSaveData.Apply(Player.Kingdom);

            Log.Debug($"Migrated {TroopData.Count} legacy root troops.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents() { }
    }
}
