using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Retinues.Features.Unlocks.Behaviors
{
    /// <summary>
    /// Campaign behavior for tracking item unlocks and defeat progress.
    /// Handles unlocking items from kills, culture bonuses, and showing unlock popups.
    /// </summary>
    [SafeClass]
    public sealed class UnlocksBehavior : CampaignBehaviorBase
    {
        public static UnlocksBehavior Instance { get; private set; }

        public UnlocksBehavior() => Instance = this;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, int> _progressByItemId;
        public Dictionary<string, int> ProgressByItemId => _progressByItemId ??= [];

        private List<string> _unlockedItemIds;
        public List<string> UnlockedItemIds => _unlockedItemIds ??= [];

        public override void SyncData(IDataStore ds)
        {
            // Unlocked item IDs
            ds.SyncData("Retinues_Unlocks_Unlocked", ref _unlockedItemIds);
            // Defeats by item ID
            ds.SyncData("Retinues_Unlocks_Progress", ref _progressByItemId);

            Log.Info(
                $"{ProgressByItemId.Count} item defeat counts, {UnlockedItemIds.Count} unlocked."
            );
            Log.Dump(_unlockedItemIds, LogLevel.Debug);
            Log.Dump(_progressByItemId, LogLevel.Debug);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.OnItemsDiscardedByPlayerEvent.AddNonSerializedListener(
                this,
                OnItemsDiscardedByPlayer
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnMissionStarted(IMission mission)
        {
            // Return if the feature is disabled
            if (!Config.UnlockFromKills)
                return;

            Log.Debug("Adding UnlocksMissionBehavior.");

            // Cast to concrete type
            Mission m = mission as Mission;

            // Attach per-battle tracker
            m?.AddMissionBehavior(new UnlocksMissionBehavior());
            _newlyUnlocked.Clear();
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (_newlyUnlocked.Count == 0)
                return;
            if (!mapEvent?.IsPlayerMapEvent ?? true)
                return;

            // Modal summary
            ShowUnlockInquiry(_newlyUnlocked);

            _newlyUnlocked.Clear();
        }

        private void OnItemsDiscardedByPlayer(ItemRoster roster)
        {
            // Return if the feature is disabled
            if (!Config.UnlockFromDiscards)
                return;

            if (roster == null || roster.Count == 0)
                return;

            // Build counts per ItemObject (you already accept a Dictionary<ItemObject,int>)
            var counts = new Dictionary<ItemObject, int>(roster.Count);

            for (int i = 0; i < roster.Count; i++)
            {
                var elem = roster.GetElementCopyAtIndex(i);
                var item = elem.EquipmentElement.Item;
                int amount = elem.Amount;

                if (item == null || amount <= 0)
                    continue;

                // Discard progress ratio
                float discardProgressRatio =
                    (float)Config.KillsForUnlock / Config.DiscardsForUnlock;

                // Progress per physical item
                int progress = amount * (int)Math.Ceiling(discardProgressRatio);

                // Accumulate
                if (counts.TryGetValue(item, out int prev))
                    counts[item] = prev + progress;
                else
                    counts[item] = progress;
            }

            if (counts.Count == 0)
                return;

            Log.Debug(
                $"OnItemsDiscardedByPlayer: {counts.Count} stacks will contribute to unlock progress."
            );
            AddUnlockCounts(counts, addCultureBonuses: false);

            if (_newlyUnlocked.Count > 0)
                ShowUnlockInquiry(_newlyUnlocked);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the item is currently being unlocked.
        /// </summary>
        public static bool InProgress(string itemId)
        {
            if (Instance == null || itemId == null)
                return false;

            return Instance.ProgressByItemId.TryGetValue(itemId, out int count)
                && count > 0
                && count < Math.Max(1, Config.KillsForUnlock);
        }

        /// <summary>
        /// Returns the current progress towards unlocking the item.
        /// </summary>
        public static int GetProgress(string itemId)
        {
            if (Instance == null || itemId == null)
                return 0;

            Instance.ProgressByItemId.TryGetValue(itemId, out int count);
            return count;
        }

        /// <summary>
        /// Returns true if the item is unlocked.
        /// </summary>
        public static bool IsUnlocked(string itemId) =>
            Instance != null && itemId != null && Instance.UnlockedItemIds.Contains(itemId);

        /// <summary>
        /// Unlocks the item if not already unlocked.
        /// </summary>
        public static void Unlock(ItemObject item)
        {
            if (Instance == null || item == null)
                return;

            if (Instance.UnlockedItemIds.Contains(item.StringId))
                return;

            Instance.UnlockedItemIds.Add(item.StringId);
        }

        /// <summary>
        /// Locks the item if currently unlocked.
        /// </summary>
        public static void Lock(ItemObject item)
        {
            if (Instance == null || item == null)
                return;

            if (!Instance.UnlockedItemIds.Contains(item.StringId))
                return;

            Instance.UnlockedItemIds.Remove(item.StringId);
        }

        /// <summary>
        /// Resets all unlocks and progress.
        /// </summary>
        public void Reset()
        {
            UnlockedItemIds.Clear();
            ProgressByItemId.Clear();
            Log.Info("All unlocks and progress have been reset.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly List<ItemObject> _newlyUnlocked = [];

        /// <summary>
        /// Get a random item of a given culture and tier.
        /// </summary>
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

        /// <summary>
        /// Add unlock bonuses for player's own culture.
        /// </summary>
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
                AddUnlockCounts(randomItemsByTier, false);
        }

        /// <summary>
        /// Add battle defeat counts and unlock items if threshold reached.
        /// </summary>
        internal void AddUnlockCounts(
            Dictionary<ItemObject, int> battleCounts,
            bool addCultureBonuses = true
        )
        {
            // Only add culture bonuses if enabled
            addCultureBonuses = addCultureBonuses && Config.OwnCultureUnlockBonuses;

            Log.Info($"AddBattleCounts: {battleCounts.Count} items to process.");

            if (battleCounts == null || battleCounts.Count == 0)
                return;

            int threshold = Math.Max(1, Config.KillsForUnlock);

            Dictionary<int, int> ownCultureBonuses = [];

            int loopIndex = 0;
            foreach (var kvp in battleCounts)
            {
                loopIndex++;
                try
                {
                    var item = kvp.Key;
                    var inc = kvp.Value;

                    if (item == null)
                        continue;

                    // accumulate in own culture as well
                    if (
                        addCultureBonuses
                        && item.Culture != null
                        && Player.Clan != null
                        && item.Culture?.StringId != Player.Clan.StringId
                    )
                    {
                        if (!ownCultureBonuses.ContainsKey((int)item.Tier))
                            ownCultureBonuses[(int)item.Tier] = 0;
                        ownCultureBonuses[(int)item.Tier] += inc;
                    }

                    var id = item.StringId;
                    ProgressByItemId.TryGetValue(id, out int prev);
                    int now = prev + inc;
                    ProgressByItemId[id] = now;

                    if (prev < threshold && now >= threshold)
                    {
                        if (!UnlockedItemIds.Contains(id))
                        {
                            UnlockedItemIds.Add(id);
                            _newlyUnlocked.Add(item);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                    continue;
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
