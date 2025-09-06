using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Logic;
using CustomClanTroops.Persistence;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Behaviors
{
    public class CampaignBehavior : CampaignBehaviorBase
    {
        private List<TroopSaveData> _savedTroops = [];
        private ItemSaveData _itemData = null;

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist the troops inside the native save.
            dataStore.SyncData("CCT_Troops", ref _savedTroops);
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            Log.Debug("OnGameLoaded.");

            // Restore stocks and unlocked items

            if (_itemData != null)
            {
                // Clear existing items, if any
                WItem.UnlockedItems.Clear();
                WItem.Stocks.Clear();

                // Restore from save
                ItemSave.Load(_itemData);

                Log.Debug($"Restored {_itemData.UnlockedItemIds.Count} unlocked items and {_itemData.StockedItems.Count} stocked items.");
            }
            else
            {
                Log.Debug("No item data found in save.");
            }

            // Restore troops from save
            if (_savedTroops != null && _savedTroops.Count > 0)
            {
                RestoreTroopsFromSave(_savedTroops);
                Log.Debug($"Restored {_savedTroops.Count} root troops from save.");
            }
            else
            {
                Log.Debug("No saved troops found in save.");
            }
        }

        private void OnBeforeSave()
        {
            Log.Debug("Collecting item data.");

            _itemData = ItemSave.Save();

            Log.Debug($"Collected {_itemData.UnlockedItemIds.Count} unlocked items and {_itemData.StockedItems.Count} stocked items.");

            Log.Debug("Collecting troops.");

            _savedTroops = CollectAllCurrentTroops();

            Log.Debug($"{_savedTroops.Count} root troops serialized.");
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
