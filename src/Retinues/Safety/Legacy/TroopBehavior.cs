using System.Collections.Generic;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Save;
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

            var (clanSaveData, kingdomSaveData) = ConvertLegacyDataLegacyData(TroopData);

            Log.Info("Applying migrated troop data to factions...");

            // Apply migrated data back to factions
            clanSaveData.Apply(Player.Clan);
            kingdomSaveData.Apply(Player.Kingdom);

            Log.Debug($"Migrated {TroopData.Count} legacy root troops.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static (FactionSaveData clan, FactionSaveData kingdom) ConvertLegacyDataLegacyData(
            List<LegacyTroopSaveData> roots
        )
        {
            var clanSaveData = new FactionSaveData(Player.Clan);
            var kingdomSaveData = new FactionSaveData(Player.Kingdom);

            Log.Info($"{roots.Count} legacy root troops found, migrating.");

            foreach (var root in roots)
            {
                // Determine if kingdom or clan
                bool isKingdom = IsKingdom(root.StringId);

                // Load legacy troop data
                var troop = LegacyTroopSaveLoader.Load(root, faction: isKingdom ? Player.Kingdom : Player.Clan, parent: null, type: GetTroopType(root.StringId));

                // Determine faction
                var factionData = isKingdom ? kingdomSaveData : clanSaveData;

                // Assign to the faction
                factionData.Assign(troop);
            }

            return (clanSaveData, kingdomSaveData);
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

            if (LegacyTroopSaveLoader.TroopIdMap.Count == 0)
                return; // No troops to swap

            Log.Info("Swapping out legacy troops in parties and settlements...");

            foreach (var kvp in LegacyTroopSaveLoader.TroopIdMap)
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

            LegacyTroopSaveLoader.TroopIdMap.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the ID is an elite troop.
        /// </summary>
        private static bool IsElite(string id) => id != null && id.Contains("_elite_");

        /// <summary>
        /// Returns true if the ID is a kingdom troop.
        /// </summary>
        private static bool IsKingdom(string id) => id != null && id.Contains("_kingdom_");

        /// <summary>
        /// Determines the troop type from the ID.
        /// </summary>
        private static WCharacter.TroopType GetTroopType(string id)
        {
            if (id == null)
                return WCharacter.TroopType.Other;

            // Special troops
            if (id.Contains("_retinue_"))
                return IsElite(id) ? WCharacter.TroopType.RetinueElite : WCharacter.TroopType.RetinueBasic;
            if (id.Contains("_mmilitia_"))
                return IsElite(id) ? WCharacter.TroopType.MilitiaMeleeElite : WCharacter.TroopType.MilitiaMelee;
            if (id.Contains("_rmilitia_"))
                return IsElite(id) ? WCharacter.TroopType.MilitiaRangedElite : WCharacter.TroopType.MilitiaRanged;

            // Regular troops
            return IsElite(id) ? WCharacter.TroopType.Elite : WCharacter.TroopType.Basic;
        }
    }
}
