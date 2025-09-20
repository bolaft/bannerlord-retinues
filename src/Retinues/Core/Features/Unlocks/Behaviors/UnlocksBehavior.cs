using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Features.Unlocks.Behaviors
{
    public sealed class UnlocksBehavior : CampaignBehaviorBase
    {
        // Persistent total defeats per item id
        private Dictionary<string, int> _defeatsByItemId = [];

        // Transient: items newly unlocked this battle (to show in post-battle UI)
        private readonly List<ItemObject> _newlyUnlockedThisBattle = [];

        public override void RegisterEvents()
        {
            // Install our mission collector each time a mission starts
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);

            // Good time to show a result summary (after map event concludes)
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore ds)
        {
            ds.SyncData(nameof(_defeatsByItemId), ref _defeatsByItemId);

            Log.Debug($"SyncData: {_defeatsByItemId.Count} item defeat counts.");
        }

        private void OnMissionStarted(IMission mission)
        {
            // Return if the feature is disabled
            if (!Config.GetOption<bool>("UnlockFromKills"))
                return;

            // Cast to concrete type
            Mission m = mission as Mission;

            // Add our per-battle tracker
            m?.AddMissionBehavior(new UnlocksMissionBehavior(this));
            _newlyUnlockedThisBattle.Clear();
        }

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
            Dictionary<ItemObject, int> randomItemsByTier = [];

            foreach (var tier in bonuses.Keys)
                randomItemsByTier[GetRandomItem(Player.Clan?.Culture?.StringId, tier)] = bonuses[
                    tier
                ];

            AddBattleCounts(randomItemsByTier, false);
        }

        // Called by the mission behavior when a battle ends to flush its per-battle counts
        internal void AddBattleCounts(
            Dictionary<ItemObject, int> battleCounts,
            bool addCultureBonuses = true
        )
        {
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
                    && Config.GetOption<bool>("OwnCultureUnlockBonuses")
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

                // crossed the threshold? unlock once
                if (prev < threshold && now >= threshold)
                {
                    var w = new WItem(item);
                    if (!w.IsUnlocked) // if you expose it; else check your UnlockedItems set
                    {
                        w.Unlock();
                        _newlyUnlockedThisBattle.Add(item);
                    }
                }
            }

            // Apply accumulated own culture bonuses
            AddOwnCultureBonuses(ownCultureBonuses);
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

        private static void ShowUnlockInquiry(List<ItemObject> items)
        {
            var body = string.Join("\n", items.Select(i => i.Name.ToString()));

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
