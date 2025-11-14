using System.Collections.Generic;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Safety.Legacy
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

        private void OnGameLoadFinished()
        {
            Helpers.EnsureMainPartyLeader();

            if (LegacyTroopSaveConverter.TroopIdMap.Count == 0)
                return; // No troops to swap

            Log.Info("Swapping out legacy troops in parties and settlements...");

            foreach (var kvp in LegacyTroopSaveConverter.TroopIdMap)
            {
                var oldId = kvp.Key;
                var newId = kvp.Value;

                var oldTroop = new WCharacter(oldId);
                var newTroop = new WCharacter(newId);

                // Swap out legacy troops in all parties
                foreach (var mp in MobileParty.All)
                {
                    var party = new WParty(mp);
                    party?.MemberRoster.SwapTroop(oldTroop, newTroop);
                    party?.PrisonRoster.SwapTroop(oldTroop, newTroop);
                }

                // Swap out legacy troops in all settlements
                foreach (var s in Campaign.Current.Settlements)
                {
                    var settlement = new WSettlement(s);

                    foreach (var notable in settlement.Notables)
                        notable.SwapVolunteer(oldTroop, newTroop);
                }
            }

            LegacyTroopSaveConverter.TroopIdMap.Clear();
        }
    }
}
