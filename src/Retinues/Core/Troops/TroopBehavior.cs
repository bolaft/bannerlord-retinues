using System;
using System.Collections.Generic;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Troops.Save;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Troops
{
    [SafeClass(SwallowByDefault = false)]
    public sealed class TroopBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private List<TroopSaveData> _troopData;
        public List<TroopSaveData> TroopData => _troopData ??= [];

        public bool HasSyncData => TroopData.Count > 0;

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

        private void OnGameLoaded(CampaignGameStarter _)
        {
            try
            {
                if (TroopData.Count > 0)
                {
                    foreach (var root in TroopData)
                        TroopLoader.Load(root); // rebuild every tree from the save payload

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
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static List<TroopSaveData> CollectAllDefinedCustomTroops()
        {
            var list = new List<TroopSaveData>();

            CollectFromFaction(Player.Clan, list);
            CollectFromFaction(Player.Kingdom, list);

            return list;
        }

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
