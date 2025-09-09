using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Persistence.Troop
{
    public class TroopSaveBehavior : CampaignBehaviorBase
    {
        private List<TroopSaveData> _TroopData = [];

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist the troops inside the native save.
            dataStore.SyncData("Retinues", ref _TroopData);
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            Log.Debug("OnGameLoaded.");

            // Restore troops from save
            if (_TroopData != null && _TroopData.Count > 0)
            {
                RestoreTroopsFromSave(_TroopData);
                Log.Debug($"Restored {_TroopData.Count} root troops from save.");
            }
            else
            {
                Log.Debug("No saved troops found in save.");
            }
        }

        private void OnBeforeSave()
        {
            Log.Debug("Collecting troops.");

            _TroopData = CollectAllCurrentTroops();

            Log.Debug($"{_TroopData.Count} root troops serialized.");
        }

        // -------------------------
        // Helpers
        // -------------------------

        private static List<TroopSaveData> CollectAllCurrentTroops()
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

            if (faction.RetinueElite != null && faction.RetinueBasic != null)
            {
                Log.Debug("Collecting retinue troops.");
                list.Add(TroopSave.Save(faction.RetinueElite));
                list.Add(TroopSave.Save(faction.RetinueBasic));
            }
            else
            {
                Log.Debug("No retinue troops found.");
            }

            if (faction.RootElite != null && faction.RootBasic != null)
            {
                Log.Debug("Collecting root troops.");
                list.Add(TroopSave.Save(faction.RootElite));
                list.Add(TroopSave.Save(faction.RootBasic));
            }
            else
            {
                Log.Debug("No root troops found.");
            }
        }

        private static void RestoreTroopsFromSave(List<TroopSaveData> saved)
        {
            // Clear existing troops, if any
            Player.Clan?.ClearTroops();
            Player.Kingdom?.ClearTroops();

            // Rebuild recursively so upgrade targets are also recreated
            foreach (var root in saved)
                TroopSave.Load(root);
        }
    }
}
