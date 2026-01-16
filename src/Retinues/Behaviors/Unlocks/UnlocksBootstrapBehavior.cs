using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Game.Unlocks
{
    /// <summary>
    /// Centralizes one-time campaign startup logic for Retinues.
    /// Runs once per campaign and persists a flag to never re-run again.
    /// </summary>
    public sealed class UnlocksBootstrapBehavior : BaseCampaignBehavior<UnlocksBootstrapBehavior>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string DataStoreKey_Unlocks = "Retinues_Bootstrapped_Unlocks";

        private bool _unlocksBootstrapped;

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData(DataStoreKey_Unlocks, ref _unlocksBootstrapped);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Auto handlers                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnCharacterCreationIsOver() => TryBootstrap();

        protected override void OnGameLoadFinished() => TryBootstrap();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Bootstrap                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void TryBootstrap()
        {
            if (_unlocksBootstrapped)
                return;

            var hero = WHero.Get(Hero.MainHero);
            var clan = hero?.Clan;
            var culture = clan?.Culture;

            Log.Debug("Bootstrapping campaign (per-section). Checking pending sections...");

            // 1) Ensure starter items after troop creation based on current unlocked pool.
            if (!_unlocksBootstrapped)
            {
                if (culture?.Base != null)
                {
                    EnsureStarterUnlocks(culture);
                    _unlocksBootstrapped = true;
                    Log.Debug("Starter unlocks bootstrap complete.");
                }
                else
                {
                    Log.Debug("Starter unlocks bootstrap skipped (missing culture).");
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Item Unlocks                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly EquipmentIndex[] StarterSlots =
        [
            EquipmentIndex.Head,
            EquipmentIndex.Cape,
            EquipmentIndex.Body,
            EquipmentIndex.Gloves,
            EquipmentIndex.Leg,
            EquipmentIndex.Horse,
            EquipmentIndex.HorseHarness,
            EquipmentIndex.Weapon0,
            EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2,
            EquipmentIndex.Weapon3,
        ];

        private static void EnsureStarterUnlocks(WCulture culture)
        {
            if (culture?.Base == null)
                return;

            int targetPerSlot = Math.Max(0, Settings.DefaultUnlockedAmountPerSlot);
            int maxTier = Math.Max(0, Math.Min(6, Settings.DefaultUnlockedItemMaxTier));

            if (targetPerSlot <= 0)
                return;

            // Use a real troop as picker for skill checks and culture context.
            var picker = culture.RootElite ?? culture.RootBasic;
            if (picker?.Base == null)
                return;

            int totalUnlocked = 0;

            for (int i = 0; i < StarterSlots.Length; i++)
            {
                var slot = StarterSlots[i];

                int alreadyUnlocked = WItem.GetUnlockedItems(slot).Count();
                int missing = Math.Max(0, targetPerSlot - alreadyUnlocked);

                if (missing <= 0)
                    continue;

                totalUnlocked += UnlockRandomMissingForSlot(
                    picker: picker,
                    culture: culture,
                    slot: slot,
                    missing: missing,
                    maxTier: maxTier
                );
            }

            Log.Debug(
                $"Unlocked {totalUnlocked} starter items (max tier {maxTier}, target {targetPerSlot} per slot)."
            );
        }

        private static int UnlockRandomMissingForSlot(
            WCharacter picker,
            WCulture culture,
            EquipmentIndex slot,
            int missing,
            int maxTier
        )
        {
            if (picker?.Base == null)
                return 0;

            if (missing <= 0)
                return 0;

            int unlocked = 0;

            // Avoid spinning forever if the pool is tiny.
            int maxAttempts = Math.Max(50, missing * 50);

            HashSet<string> seen = [];

            for (int attempt = 0; attempt < maxAttempts && unlocked < missing; attempt++)
            {
                var item = RandomItemHelper.GetRandomItemForSlot(
                    owner: picker,
                    slot: slot,
                    civilian: false,
                    minTier: 0,
                    maxTier: maxTier,
                    acceptableCultures: culture != null ? [culture] : null,
                    acceptNeutralCulture: true,
                    requireSkillForItem: true,
                    itemFilter: it => it != null && !it.IsUnlocked && !seen.Contains(it.StringId)
                );

                if (item?.Base == null)
                    break;

                seen.Add(item.StringId);

                item.Unlock();
                unlocked++;
            }

            return unlocked;
        }
    }
}
