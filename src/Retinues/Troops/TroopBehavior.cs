using System;
using System.Collections.Generic;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Persistence;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using System.Linq;

namespace Retinues.Troops
{
    /// <summary>
    /// Campaign behavior for saving and loading custom troop definitions.
    /// Handles serialization, event registration, and tree rebuilding for custom troops.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
    public sealed class TroopBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private List<TroopSaveData> _troopData;
        public List<TroopSaveData> TroopData => _troopData ??= [];

        /// <summary>
        /// Returns true if there is any troop sync data present.
        /// </summary>
        public bool HasSyncData => TroopData.Count > 0;

        /// <summary>
        /// Syncs custom troop data to and from the campaign save file.
        /// </summary>
        public override void SyncData(IDataStore ds)
        {
            // Persist custom troop roots in the save file.
            ds.SyncData("Retinues_Troops_Data", ref _troopData);

            Log.Info($"{TroopData.Count} root troops.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            // Save roots before saving
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);

            // Rebuild all trees from serialized roots after a save is loaded
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Saving ━━━━━━━━ */

        /// <summary>
        /// Collects all defined custom troops before saving.
        /// </summary>
        private void OnBeforeSave()
        {
            try
            {
                _troopData = CollectAllDefinedCustomTroops();
                Log.Debug($"Serialized {TroopData.Count} root troops.");
            }
            catch (Exception e)
            {
                Log.Exception(e, "OnBeforeSave failed");
            }
        }

        /* ━━━━━━━━ Loading ━━━━━━━ */

        /// <summary>
        /// Loads and rebuilds all custom troop trees after a save is loaded.
        /// </summary>
        private void OnGameLoaded(CampaignGameStarter _)
        {
            try
            {
                if (TroopData.Count > 0)
                {
                    foreach (var root in TroopData)
                    {
                        var troop = TroopLoader.Load(root);
                        // sanitize old saves: enforce [0]=battle, [1]=civilian, [2+]=battle
                        var eqs = troop.Loadout.Equipments; // WEquipment list
                        troop.Loadout.Equipments = [.. eqs.Select((we, i) => WEquipment.FromCode(we.Code, i == 1))];
                    }
                    Log.Debug($"Rebuilt {TroopData.Count} root troops from save.");
                }
                else
                {
                    Log.Debug("No custom roots found in save.");
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "OnGameLoaded failed");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Collects all custom troop roots from player clan and kingdom.
        /// </summary>
        public static List<TroopSaveData> CollectAllDefinedCustomTroops()
        {
            var list = new List<TroopSaveData>();

            CollectFromFaction(Player.Clan, list);
            CollectFromFaction(Player.Kingdom, list);

            return list;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds all active custom troop types from a faction to the list.
        /// </summary>
        private static void CollectFromFaction(WFaction faction, List<TroopSaveData> list)
        {
            if (faction is null)
            {
                Log.Debug("Collect: no faction, skipping.");
                return;
            }

            // Retinues
            if (faction.RetinueElite.IsActive && faction.RetinueBasic.IsActive)
            {
                list.Add(TroopLoader.Save(faction.RetinueElite));
                list.Add(TroopLoader.Save(faction.RetinueBasic));
            }

            // Roots
            if (faction.RootElite.IsActive && faction.RootBasic.IsActive)
            {
                list.Add(TroopLoader.Save(faction.RootElite));
                list.Add(TroopLoader.Save(faction.RootBasic));
            }

            // Militias
            if (
                faction.MilitiaMelee.IsActive
                && faction.MilitiaMeleeElite.IsActive
                && faction.MilitiaRanged.IsActive
                && faction.MilitiaRangedElite.IsActive
            )
            {
                list.Add(TroopLoader.Save(faction.MilitiaMelee));
                list.Add(TroopLoader.Save(faction.MilitiaMeleeElite));
                list.Add(TroopLoader.Save(faction.MilitiaRanged));
                list.Add(TroopLoader.Save(faction.MilitiaRangedElite));
            }
        }
    }
}
