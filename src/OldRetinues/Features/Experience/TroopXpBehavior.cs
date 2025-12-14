using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace OldRetinues.Features.Experience
{
    /// <summary>
    /// Campaign behavior for tracking and managing XP pools for custom troops.
    /// Provides static API for getting, setting, spending, and refunding troop XP.
    /// </summary>
    [SafeClass]
    public class TroopXpBehavior : CampaignBehaviorBase
    {
        public const float MarinerPenaltyMultiplier = 0.8f; // 80% XP for mariner troops
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
            if (mission is not Mission m)
                return;

            // Log mission mode
            Log.Info($"Mission mode: {m.Mode}.");

            // Attach per-battle tracker
            m.AddMissionBehavior(new BattleMissionXpBehavior());
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

            // Apply mariner penalty
            if (troop.IsMariner && delta > 0)
                delta = (int)(delta * MarinerPenaltyMultiplier);

            var cur = Instance.GetPool(PoolKey(troop));
            Instance._xpPools[PoolKey(troop)] = Math.Max(0, cur + delta);
        }

        /// <summary>
        /// Replace an old pool key with a new one (e.g., on troop ID change).
        /// </summary>
        public static void ReplacePoolKey(string oldKey, string newKey)
        {
            if (Instance == null || string.IsNullOrEmpty(oldKey) || string.IsNullOrEmpty(newKey))
                return;
            if (Instance._xpPools.TryGetValue(oldKey, out var v))
            {
                Instance._xpPools[newKey] = v;
                Instance._xpPools.Remove(oldKey);
            }
        }

        /// <summary>
        /// Try to spend XP from a troop's pool. Returns true if successful.
        /// </summary>
        public static bool TrySpend(WCharacter troop, int amount)
        {
            if (Config.SkillXpCostPerPoint == 0 && Config.BaseSkillXpCost == 0)
                return true;
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

            if (!DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>() && !Config.ForceXpRefunds)
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

            int amount = SkillManager.SkillPointXpCost(skillValue - 1);

            Refund(troop, amount, force);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal static bool SharedPool => Config.SharedXpPool;

        internal static string PoolKey(WCharacter troop)
        {
            if (troop?.IsCaptain == true && troop.BaseTroop != null)
                troop = troop.BaseTroop; // Use base troop for captains

            return SharedPool ? "_shared" : troop?.StringId;
        }

        internal int GetPool(string key) =>
            (key != null && _xpPools.TryGetValue(key, out var v)) ? v : 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds XP to a troop by ID. Usage: retinues.troop_xp_add [id] [amount]
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("troop_xp_add", "retinues")]
        public static string TroopXpAdd(List<string> args)
        {
            if (args.Count == 0 || args.Count > 2)
                return "Usage: retinues.troop_xp_add [id] [amount]";

            // Find the troop
            WCharacter troop;

            try
            {
                troop = new(args[0]);
            }
            catch
            {
                return "Invalid troop ID.";
            }

            // Parse the amount
            int amount;

            if (args.Count == 1)
            {
                amount = 1000;
            }
            else
            {
                try
                {
                    amount = int.Parse(args[1]);
                }
                catch
                {
                    return "Amount must be an integer.";
                }
            }

            // Add the XP
            try
            {
                Add(troop, amount);
            }
            catch (Exception e)
            {
                return $"Failed to add XP: {e.Message}";
            }

            return $"Added {amount} XP to {troop.Name} ({troop}).";
        }
    }
}
