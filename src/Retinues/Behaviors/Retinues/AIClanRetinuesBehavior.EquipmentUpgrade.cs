using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Services.Random;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Interface.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Retinues
{
    public sealed partial class AIClanRetinuesBehavior
    {
        private static readonly EquipmentIndex[] UpgradeSlots =
        [
            EquipmentIndex.Head,
            EquipmentIndex.Cape,
            EquipmentIndex.Body,
            EquipmentIndex.Gloves,
            EquipmentIndex.Leg,
            EquipmentIndex.Weapon0,
            EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2,
            EquipmentIndex.Weapon3,
        ];

        private const double EquipmentUpgradeChance = 0.01;

        /// <summary>
        /// Iterates all AI clan retinues and gives each a 1% daily chance to upgrade one
        /// piece of gear by one tier.
        /// </summary>
        private void TryDailyEquipmentUpgradesForAllAIRetinues()
        {
            if (!Configuration.EnableRetinues || !Configuration.EnableAIClanRetinues)
                return;

            foreach (var clan in WClan.All)
            {
                if (clan?.Base == null || clan.IsEliminated || clan.IsBanditFaction)
                    continue;

                if (clan.Base == Clan.PlayerClan)
                    continue;

                foreach (var retinue in clan.GetRawRetinues())
                {
                    if (retinue?.Base == null)
                        continue;

                    if (_rng.NextDouble() < EquipmentUpgradeChance)
                        TryUpgradeRetinueEquipment(retinue);
                }
            }
        }

        /// <summary>
        /// Attempts to upgrade one item slot in the retinue's battle equipment set.
        /// Shuffles all armor and weapon slots, then for each slot tries to find an item
        /// one tier higher. Prefers culture-matched and neutral-culture items; falls back
        /// to any culture when no match is found. Stops at the first successful upgrade.
        /// </summary>
        private void TryUpgradeRetinueEquipment(WCharacter retinue)
        {
            var battleSet = retinue.FirstBattleEquipment;
            if (battleSet == null)
                return;

            // Shuffle slots for random ordering.
            var slots = new List<EquipmentIndex>(UpgradeSlots);
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            var culture = retinue.Culture;
            WCulture[] cultures = culture != null ? [culture] : null;

            foreach (var slot in slots)
            {
                var currentItem = battleSet.GetBase(slot);
                int currentTier = currentItem?.Tier ?? 0;

                if (currentTier >= 6)
                    continue;

                int targetTier = Math.Max(1, currentTier + 1);

                // Prefer culture + neutral; fall back to any culture.
                var picked =
                    ItemRandomizer.GetRandomItemForSlot(
                        retinue,
                        slot,
                        civilian: false,
                        minTier: targetTier,
                        maxTier: 6,
                        acceptableCultures: cultures,
                        acceptNeutralCulture: true,
                        requireSkillForItem: false
                    )
                    ?? ItemRandomizer.GetRandomItemForSlot(
                        retinue,
                        slot,
                        civilian: false,
                        minTier: targetTier,
                        maxTier: 6,
                        acceptableCultures: null,
                        acceptNeutralCulture: true,
                        requireSkillForItem: false
                    );

                if (picked == null || picked == currentItem)
                    continue;

                battleSet.Set(slot, picked);
                Log.Debug(
                    $"[AIClanRetinue] '{retinue.Name}' upgraded {slot}: {currentItem?.Name ?? "empty"} → {picked.Name} (T{picked.Tier})"
                );
                Notifications.Message(
                    L.T("ai_retinue_equipment_upgrade", "{RETINUE} acquired {ITEM}.")
                        .SetTextVariable("RETINUE", retinue.Name)
                        .SetTextVariable("ITEM", picked.Name)
                );
                return;
            }
        }
    }
}
