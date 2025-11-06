using System.Collections.Generic;
using Retinues.Game;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

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

            var clanSaveData = new FactionSaveData(Player.Clan);
            var kingdomSaveData = new FactionSaveData(Player.Kingdom);

            Log.Info($"{TroopData.Count} legacy root troops found, migrating.");

            foreach (var root in TroopData)
            {
                // Load legacy troop data
                var troop = LegacyTroopSaveLoader.Load(root);

                // Create new troop save data
                var data = new TroopSaveData(troop);

                // Determine faction
                var factionData = IsKingdom(root.StringId) ? kingdomSaveData : clanSaveData;

                // Determine troop type
                var token = ExtractToken(root.StringId);

                // Determine if elite
                var elite = IsElite(root.StringId);

                // Assign to appropriate slot
                switch (token)
                {
                    case "retinue": // Retinues
                        if (elite)
                            factionData.RetinueElite = data;
                        else
                            factionData.RetinueBasic = data;
                        break;

                    case "mmilitia": // Melee Militia
                        if (elite)
                            factionData.MilitiaMeleeElite = data;
                        else
                            factionData.MilitiaMelee = data;
                        break;

                    case "rmilitia": // Ranged Militia
                        if (elite)
                            factionData.MilitiaRangedElite = data;
                        else
                            factionData.MilitiaRanged = data;
                        break;

                    default: // Regular Roots
                        if (elite)
                            factionData.RootElite = data;
                        else
                            factionData.RootBasic = data;
                        break;
                }
            }

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Extracts the token from a custom troop ID.
        /// </summary>
        private static string ExtractToken(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            var underscore = id.LastIndexOf('_');
            return underscore >= 0 && underscore + 1 < id.Length
                ? id.Substring(underscore + 1)
                : null;
        }

        /// <summary>
        /// Returns true if the ID is an elite troop.
        /// </summary>
        public bool IsElite(string id) => id != null && id.Contains("_elite_");

        /// <summary>
        /// Returns true if the ID is a kingdom troop.
        /// </summary>
        public bool IsKingdom(string id) => id != null && id.Contains("_kingdom_");
    }
}
