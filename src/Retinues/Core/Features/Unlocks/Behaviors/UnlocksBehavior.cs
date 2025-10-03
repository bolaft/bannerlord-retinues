using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Features.Unlocks.Behaviors
{
    [SafeClass]
    public sealed class UnlocksBehavior : CampaignBehaviorBase
    {
        public static UnlocksBehavior Instance { get; private set; }

        public UnlocksBehavior() => Instance = this;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, int> _defeatsByItemId;

        private HashSet<string> _unlockedItemIds = [];

        public bool HasSyncData =>
            (_defeatsByItemId != null && _defeatsByItemId.Count > 0)
            || (_unlockedItemIds != null && _unlockedItemIds.Count > 0);

        public override void SyncData(IDataStore ds)
        {
            if (ds.IsSaving)
            {
                // write as List<string>
                var list = _unlockedItemIds?.ToList() ?? [];
                ds.SyncData(nameof(_unlockedItemIds), ref list);
            }
            else
            {
                // read as List<string>
                List<string> list = null;
                ds.SyncData(nameof(_unlockedItemIds), ref list);

                // convert to HashSet<string>
                _unlockedItemIds = list != null
                    ? new HashSet<string>(list, StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal);
            }

            // Defeats by item ID
            ds.SyncData(nameof(_defeatsByItemId), ref _defeatsByItemId);

            Log.Info(
                $"{_defeatsByItemId.Count} item defeat counts, {_unlockedItemIds.Count} unlocked."
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnMissionStarted(IMission mission)
        {
            // Return if the feature is disabled
            if (!Config.GetOption<bool>("UnlockFromKills"))
                return;

            Log.Info("OnMissionStarted: Installing UnlocksMissionBehavior.");

            // Cast to concrete type
            Mission m = mission as Mission;

            // Add our per-battle tracker
            m?.AddMissionBehavior(new UnlocksMissionBehavior(this));
            _newlyUnlockedThisBattle.Clear();
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (_newlyUnlockedThisBattle.Count == 0)
                return;
            if (!mapEvent?.IsPlayerMapEvent ?? true)
                return;

            // Modal summary
            ShowUnlockInquiry(_newlyUnlockedThisBattle);

            _newlyUnlockedThisBattle.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static Dictionary<string, int> IdsToProgress => Instance?._defeatsByItemId ?? [];

        public static bool IsUnlocked(string itemId) =>
            Instance != null && itemId != null && Instance._unlockedItemIds.Contains(itemId);

        public static void Unlock(ItemObject item)
        {
            if (Instance == null || item == null)
                return;

            Instance._unlockedItemIds.Add(item.StringId);
        }

        public static void Lock(ItemObject item)
        {
            if (Instance == null || item == null)
                return;
            Instance._unlockedItemIds.Remove(item.StringId);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly List<ItemObject> _newlyUnlockedThisBattle = [];

        internal ItemObject GetRandomItem(string cultureId, int tier)
        {
            // Look up the culture once (optional: compare by StringId instead)
            var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
            if (culture == null)
                return null;

            // Pull the global item list and filter
            IEnumerable<ItemObject> allItems =
                MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
            var pool = allItems.Where(i =>
                i != null
                && (int)i.Tier == tier
                && i.Culture == culture
                && i.ItemType != ItemObject.ItemTypeEnum.Invalid
            );

            // Pick random
            var list = pool.ToList();
            if (list.Count == 0)
                return null;

            int idx = MBRandom.RandomInt(list.Count);
            return list[idx];
        }

        internal void AddOwnCultureBonuses(Dictionary<int, int> bonuses)
        {
            Log.Info(
                $"AddOwnCultureBonuses: {bonuses.Count} tiers to process for culture {Player.Clan?.Culture?.Name}."
            );

            Dictionary<ItemObject, int> randomItemsByTier = [];

            foreach (var tier in bonuses.Keys)
            {
                var it = GetRandomItem(Player.Clan?.Culture?.StringId, tier);
                if (it != null)
                    randomItemsByTier[it] = bonuses[tier];
            }

            if (randomItemsByTier.Count > 0)
                AddBattleCounts(randomItemsByTier, false);
        }

        // Called by the mission behavior when a battle ends to flush its per-battle counts
        internal void AddBattleCounts(
            Dictionary<ItemObject, int> battleCounts,
            bool addCultureBonuses = true
        )
        {
            // Only add culture bonuses if enabled
            addCultureBonuses =
                addCultureBonuses && Config.GetOption<bool>("OwnCultureUnlockBonuses");

            Log.Info($"AddBattleCounts: {battleCounts.Count} items to process.");

            if (battleCounts == null || battleCounts.Count == 0)
                return;

            int threshold = Math.Max(1, Config.GetOption<int>("KillsForUnlock"));

            Dictionary<int, int> ownCultureBonuses = [];

            foreach (var kvp in battleCounts)
            {
                var item = kvp.Key;
                var inc = kvp.Value;

                if (item == null)
                    continue;

                // accumulate in own culture as well
                if (
                    addCultureBonuses
                    && item.Culture != null
                    && item.Culture != Clan.PlayerClan?.Culture
                )
                {
                    if (!ownCultureBonuses.ContainsKey((int)item.Tier))
                        ownCultureBonuses[(int)item.Tier] = 0;
                    ownCultureBonuses[(int)item.Tier] += inc;
                }

                var id = item.StringId;
                _defeatsByItemId.TryGetValue(id, out int prev);
                int now = prev + inc;
                _defeatsByItemId[id] = now;

                if (prev < threshold && now >= threshold)
                {
                    if (_unlockedItemIds.Add(item.StringId))
                        _newlyUnlockedThisBattle.Add(item);
                }
            }

            // Apply accumulated own culture bonuses
            if (addCultureBonuses)
                AddOwnCultureBonuses(ownCultureBonuses);
        }

        private static void ShowUnlockInquiry(List<ItemObject> items)
        {
            var body = string.Join(
                "\n",
                items.Where(i => i != null).Select(i => i.Name?.ToString() ?? i.StringId)
            );

            InformationManager.ShowInquiry(
                new InquiryData(
                    L.S("items_unlocked", "New Gear Unlocked"),
                    body,
                    true,
                    false,
                    GameTexts.FindText("str_ok").ToString(),
                    "",
                    null,
                    null
                )
            );
        }
    }
}
