using System;
using System.Collections.Generic;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Xp.Behaviors
{
    /// <summary>
    /// Campaign behavior for tracking and managing XP pools for custom troops.
    /// Provides static API for getting, setting, spending, and refunding troop XP.
    /// </summary>
    [SafeClass]
    public sealed class TroopXpBehavior : CampaignBehaviorBase
    {
        public const float TrainingXpMultiplier = 0.2f; // 20% of the original XP

        public static TroopXpBehavior Instance { get; private set; }

        public TroopXpBehavior()
        {
            Instance = this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, int> _xpPools = [];

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_Xp_Pools", ref _xpPools);

            Log.Info($"{_xpPools.Count} troop XP pools.");
            Log.Dump(_xpPools, LogLevel.Debug);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnMissionStarted(IMission mission)
        {
            Log.Debug("Adding TroopXpMissionBehavior.");

            // Cast to concrete type
            Mission m = mission as Mission;

            // Attach per-battle tracker
            m?.AddMissionBehavior(new TroopXpMissionBehavior());
        }

        private void OnDailyTickParty(MobileParty mobileParty)
        {
            if (mobileParty == null || mobileParty.IsMainParty == false)
                return; // only care about main party

            var party = new WParty(mobileParty);
            
            foreach (var e in party.MemberRoster.Elements)
            {
                if (!e.Troop.IsCustom)
                    continue; // only care about custom troops

                // Vanilla uses PartyTrainingModel.GetEffectiveDailyExperience(...) per element.
                ExplainedNumber en =
                    Campaign.Current.Models.PartyTrainingModel.GetEffectiveDailyExperience(
                        mobileParty,
                        e.Base
                    );

                float each = en.ResultNumber;
                int total = (int)(each * e.Number * TrainingXpMultiplier);

                if (total <= 0)
                    continue; // no XP to give

                Log.Debug($"Granting training XP for {e.Troop.Name}: {total} XP");
                Add(e.Troop, total);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Get the XP pool for a troop.
        /// </summary>
        public static int Get(WCharacter troop)
        {
            if (troop == null || Instance == null)
                return 0;
            return Instance.GetPool(PoolKey(troop));
        }

        /// <summary>
        /// Set the XP pool for a troop.
        /// </summary>
        public static void Set(WCharacter troop, int value)
        {
            if (troop == null || Instance == null || value < 0)
                return;
            Instance._xpPools[PoolKey(troop)] = value;
        }

        /// <summary>
        /// Add delta to the XP pool for a troop.
        /// </summary>
        public static void Add(WCharacter troop, int delta)
        {
            if (troop == null || Instance == null || delta == 0)
                return;
            var cur = Instance.GetPool(PoolKey(troop));
            Instance._xpPools[PoolKey(troop)] = Math.Max(0, cur + delta);
        }

        /// <summary>
        /// Try to spend XP from a troop's pool. Returns true if successful.
        /// </summary>
        public static bool TrySpend(WCharacter troop, int amount)
        {
            if (amount <= 0)
                return true;
            if (troop == null)
                return false;
            var have = Get(troop);
            if (have < amount)
                return false;
            Add(troop, -amount);
            return true;
        }

        /// <summary>
        /// Refund XP to a troop's pool.
        /// </summary>
        public static void Refund(WCharacter troop, int amount, bool force = false)
        {
            if (troop == null || amount <= 0)
                return;

            if (!DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>())
                if (!force) // If force is true, skip this check
                    return;

            Add(troop, amount);
        }

        /// <summary>
        /// Refund one skill point.
        /// </summary>
        public static void RefundOnePoint(WCharacter troop, int skillValue, bool force = false)
        {
            if (troop == null)
                return;

            int amount = TroopRules.SkillPointXpCost(skillValue - 1);

            Refund(troop, amount, force);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal static bool SharedPool => Config.GetOption<bool>("SharedXpPool");

        internal static string PoolKey(WCharacter troop) =>
            SharedPool ? "_shared" : troop?.StringId;

        internal int GetPool(string key) =>
            (key != null && _xpPools.TryGetValue(key, out var v)) ? v : 0;
    }
}
