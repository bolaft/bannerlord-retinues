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

            WFaction clan = Player.Clan;
            WFaction kingdom = Player.Kingdom;

            // Collect from clan
            if (clan.RootElite is null || clan.RootBasic is null)
            {
                Log.Debug("Clan has no troops.");
                return list;
            }

            list.Add(TroopSave.Save(clan.RootElite));
            list.Add(TroopSave.Save(clan.RootBasic));

            if (kingdom is null) return list;

            // Collect from kingdom
            if (kingdom.RootElite is null || kingdom.RootBasic is null)
            {
                Log.Debug("Kingdom has no troops.");
                return list;
            }

            list.Add(TroopSave.Save(kingdom.RootElite));
            list.Add(TroopSave.Save(kingdom.RootBasic));

            return list;
        }

        private static void RestoreTroopsFromSave(List<TroopSaveData> saved)
        {
            // Clear existing troops, if any
            Player.Clan?.EliteTroops?.Clear();
            Player.Clan?.BasicTroops?.Clear();
            Player.Kingdom?.EliteTroops?.Clear();
            Player.Kingdom?.BasicTroops?.Clear();

            // Rebuild recursively so upgrade targets are also recreated
            foreach (var root in saved)
                TroopSave.Load(root);
        }
    }
}
