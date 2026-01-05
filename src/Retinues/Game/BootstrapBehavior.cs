using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Game.Troops.Retinues;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Game
{
    /// <summary>
    /// Centralizes one-time campaign startup logic for Retinues.
    /// Runs once per campaign and persists a flag to never re-run again.
    /// </summary>
    public sealed class BootstrapBehavior : BaseCampaignBehavior<BootstrapBehavior>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string DataStoreKey_Retinue = "Retinues_Bootstrapped_Retinue";
        private const string DataStoreKey_Unlocks = "Retinues_Bootstrapped_Unlocks";

        // Per-section bootstrapped flags so parts can run independently.
        private bool _retinueBootstrapped;
        private bool _unlocksBootstrapped;

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData(DataStoreKey_Retinue, ref _retinueBootstrapped);
            dataStore.SyncData(DataStoreKey_Unlocks, ref _unlocksBootstrapped);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            // New game: after character creation, first time the player hits the campaign map.
            Hook(BehaviorEvent.CharacterCreationIsOver, TryBootstrap);

            // Loaded save: when the campaign has finished loading.
            Hook(BehaviorEvent.GameLoadFinished, TryBootstrap);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Bootstrap                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void TryBootstrap()
        {
            if (_retinueBootstrapped && _unlocksBootstrapped)
                return;

            var hero = WHero.Get(Hero.MainHero);
            var clan = hero?.Clan;
            var culture = clan?.Culture;

            Log.Info("Bootstrapping campaign (per-section). Checking pending sections...");

            // 1) Ensure default retinue for player clan (TroopBuilder unlocks its assigned items).
            if (!_retinueBootstrapped)
            {
                if (clan?.Base != null && Settings.EnableRetinues)
                {
                    CreateDefaultRetinue(clan);
                    _retinueBootstrapped = true;
                    Log.Info("Retinue bootstrap complete.");
                }
                else
                {
                    Log.Info("Retinue bootstrap skipped (missing clan or retinues disabled).");
                }
            }

            // 2) Ensure starter items after troop creation based on current unlocked pool.
            if (!_unlocksBootstrapped)
            {
                if (culture?.Base != null)
                {
                    EnsureStarterUnlocks(culture);
                    _unlocksBootstrapped = true;
                    Log.Info("Starter unlocks bootstrap complete.");
                }
                else
                {
                    Log.Info("Starter unlocks bootstrap skipped (missing culture).");
                }
            }

            if (_retinueBootstrapped && _unlocksBootstrapped)
            {
                Log.Info("All bootstrap sections complete.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Section: Item Unlocks                  //
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

            int targetPerSlot = Math.Max(0, Settings.DefaultUnlockedAmountPerSlot.Value);
            int maxTier = Math.Max(0, Math.Min(6, Settings.DefaultUnlockedItemMaxTier.Value));

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

            Log.Info(
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
                var item = RandomEquipmentHelper.GetRandomItemForSlot(
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //               Section: Default Retinue                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void CreateDefaultRetinue(WClan clan)
        {
            if (!Settings.EnableRetinues)
                return;

            if (RetinuesBehavior.TryGetInstance(out var retinues))
            {
                var name = L.T("retinue_default_name", "{CLAN} House Guard")
                    .SetTextVariable("CLAN", clan.Name)
                    .ToString();

                retinues.EnsureDefaultRetinue(clan, name);
            }
        }
    }
}
