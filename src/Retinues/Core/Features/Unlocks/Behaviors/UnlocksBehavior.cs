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

        public bool HasSyncData => UnlockedItemIds.Count > 0 || ProgressByItemId.Count > 0;

        public override void SyncData(IDataStore ds)
        {
            // Unlocked item IDs
            ds.SyncData("Retinues_Unlocks_Unlocked", ref _unlockedItemIds);
            // Defeats by item ID
            ds.SyncData("Retinues_Unlocks_Progress", ref _progressByItemId);

            Log.Info(
                $"{ProgressByItemId.Count} item defeat counts, {UnlockedItemIds.Count} unlocked."
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

            Log.Debug("Adding UnlocksMissionBehavior.");

            // Cast to concrete type
            Mission m = mission as Mission;

            // Attach per-battle tracker
            m?.AddMissionBehavior(new UnlocksMissionBehavior());
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

        private readonly List<ItemObject> _newlyUnlockedThisBattle = [];

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
                AddBattleCounts(randomItemsByTier, false);
        }

        /// <summary>
        /// Add battle defeat counts and unlock items if threshold reached.
        /// </summary>
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
                            _newlyUnlockedThisBattle.Add(item);
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
