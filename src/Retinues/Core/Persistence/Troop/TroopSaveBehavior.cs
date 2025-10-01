using System;
using System.Collections.Generic;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Persistence.Troop
{
    [SafeClass(SwallowByDefault = false)]
    public class TroopSaveBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private List<TroopSaveData> _troopData = [];

        public bool HasTroopData => _troopData != null && _troopData.Count > 0;

        public override void SyncData(IDataStore dataStore)
        {
            // Persist the troops inside the native save.
            dataStore.SyncData("Retinues_Troops", ref _troopData);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnBeforeSave()
        {
            try
            {
                Log.Debug("Collecting root troops.");
                _troopData = CollectAllDefinedCustomTroops();
                Log.Debug($"{_troopData.Count} root troops serialized.");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private void OnGameLoaded(CampaignGameStarter _)
        {
            try
            {
                // Rebuild all custom trees first
                if (_troopData != null && _troopData.Count > 0)
                {
                    foreach (var root in _troopData)
                        TroopSave.Load(root);

                    Log.Debug($"Rebuilt {_troopData.Count} root troops.");
                }
                else
                {
                    Log.Debug("No root troops in save.");
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Troop Collection                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static List<TroopSaveData> CollectAllDefinedCustomTroops()
        {
            var list = new List<TroopSaveData>();

            Log.Debug("Collecting from clan.");
            CollectFromFaction(Player.Clan, list);

            Log.Debug("Collecting from kingdom.");
            CollectFromFaction(Player.Kingdom, list);

            return list;
        }

        private static void CollectFromFaction(WFaction faction, List<TroopSaveData> list)
        {
            if (faction is null)
            {
                Log.Debug("No faction, skipping.");
                return;
            }

            if (faction.RetinueElite.IsActive && faction.RetinueBasic.IsActive)
            {
                Log.Debug("Collecting retinue troops.");
                list.Add(TroopSave.Save(faction.RetinueElite));
                list.Add(TroopSave.Save(faction.RetinueBasic));
            }
            else
            {
                Log.Debug("No retinue troops found.");
            }

            if (faction.RootElite.IsActive && faction.RootBasic.IsActive)
            {
                Log.Debug("Collecting root troops.");
                list.Add(TroopSave.Save(faction.RootElite));
                list.Add(TroopSave.Save(faction.RootBasic));
            }
            else
            {
                Log.Debug("No root troops found.");
            }

            if (
                faction.MilitiaMelee.IsActive
                && faction.MilitiaMeleeElite.IsActive
                && faction.MilitiaRanged.IsActive
                && faction.MilitiaRangedElite.IsActive
            )
            {
                Log.Debug("Collecting militia troops.");
                list.Add(TroopSave.Save(faction.MilitiaMelee));
                list.Add(TroopSave.Save(faction.MilitiaMeleeElite));
                list.Add(TroopSave.Save(faction.MilitiaRanged));
                list.Add(TroopSave.Save(faction.MilitiaRangedElite));
            }
            else
            {
                Log.Debug("No militia troops found.");
            }
        }
    }
}
