using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Services.Random;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
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

        // A persisted retinue list is not sufficient proof that its entries are editable.
        // Stale/external links must never authorize changes to ordinary troops or heroes.
        internal static bool IsEditableRetinue(WCharacter troop) =>
            troop?.Base != null
            && !string.IsNullOrEmpty(troop.StringId)
            && troop.IsCustom
            && !troop.Base.IsHero;

        /// <summary>
        /// Iterates all AI clan retinues and gives each a 1% daily chance to upgrade one
        /// piece of gear to a higher tier.
        /// </summary>
        private void TryDailyEquipmentUpgradesForAllAIRetinues()
        {
            if (!Configuration.EnableRetinues || !Configuration.EnableAIClanRetinues)
                return;

            if (!Configuration.AIClanRetinueEquipmentUpgrades)
                return; // Player opted out of AI retinues improving their own gear.

            foreach (var clan in WClan.All)
            {
                if (clan?.Base == null || clan.IsEliminated || clan.IsBanditFaction)
                    continue;

                if (clan.Base == Clan.PlayerClan)
                    continue;

                foreach (var retinue in clan.GetRawRetinues())
                {
                    if (!IsEditableRetinue(retinue))
                        continue;

                    if (_rng.NextDouble() < EquipmentUpgradeChance)
                        TryUpgradeRetinueEquipment(retinue);
                }
            }
        }

        /// <summary>
        /// Attempts to upgrade one item slot in the retinue's battle equipment set.
        /// Shuffles all armor and weapon slots, then for each slot tries to find an item
        /// at a higher tier without changing its item type. Prefers culture-matched and neutral-culture items; falls back
        /// to any culture when no match is found. Stops at the first successful upgrade.
        /// </summary>
        private void TryUpgradeRetinueEquipment(WCharacter retinue)
        {
            if (!IsEditableRetinue(retinue))
                return;

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

                // Avoid duplicate weapons; multiple quivers and throwing stacks are valid.
                var carried = new HashSet<string>(StringComparer.Ordinal);
                foreach (var other in UpgradeSlots)
                {
                    if (other == slot)
                        continue;

                    var carriedId = battleSet.GetBase(other)?.StringId;
                    if (!string.IsNullOrEmpty(carriedId))
                        carried.Add(carriedId);
                }

                var picked = PickEquipmentUpgrade(retinue, slot, currentItem, carried, cultures);

                if (picked == null || picked == currentItem)
                    continue;

                battleSet.Set(slot, picked);
                // No player notification: this is an AI clan's retinue improving its own gear, which
                // the player should not be told about. (Enemy retinues looting the player's own
                // casualties are announced separately in AIClanRetinuesBehavior.Looting.)
                Log.Debug(
                    $"[AIClanRetinue] '{retinue.Name}' upgraded {slot}: {currentItem?.Name ?? "empty"} → {picked.Name} (T{picked.Tier})"
                );
                return;
            }
        }

        internal static WItem PickEquipmentUpgrade(
            WCharacter retinue,
            EquipmentIndex slot,
            WItem currentItem,
            ISet<string> carried,
            WCulture[] cultures
        )
        {
            // Empty weapon slots are part of the loadout, not invitations to add a
            // random bow, shield or ammunition. Empty armor slots may still improve.
            if (IsWeaponSlot(slot) && currentItem == null)
                return null;

            int currentTier = currentItem?.Tier ?? 0;
            if (currentTier >= 6)
                return null;

            int targetTier = Math.Max(1, currentTier + 1);
            bool Accept(WItem item) => IsCompatibleEquipmentUpgrade(slot, currentItem, item, carried);

            // The culture fallback must enforce the same loadout constraints.
            return ItemRandomizer.GetRandomItemForSlot(
                retinue, slot, civilian: false, minTier: targetTier, maxTier: 6,
                acceptableCultures: cultures, acceptNeutralCulture: true,
                requireSkillForItem: false, itemFilter: Accept
            ) ?? ItemRandomizer.GetRandomItemForSlot(
                retinue, slot, civilian: false, minTier: targetTier, maxTier: 6,
                acceptableCultures: null, acceptNeutralCulture: true,
                requireSkillForItem: false, itemFilter: Accept
            );
        }

        internal static bool IsCompatibleEquipmentUpgrade(
            EquipmentIndex slot, WItem currentItem, WItem candidate, ISet<string> carried
        )
        {
            if (candidate?.Base == null || !candidate.IsValidEquipment || candidate.IsCrafted
                || !candidate.IsEquippableInSlot(slot))
                return false;

            if (currentItem == null)
                return !IsWeaponSlot(slot);

            // Weapon slots accept every weapon type. An "upgrade" must not turn
            // arrows into a shield, a bow into a crossbow, or a sword into ammunition.
            if (candidate.Type != currentItem.Type)
                return false;

            return candidate.IsAmmo || candidate.IsThrownWeapon || !carried.Contains(candidate.StringId);
        }

        private static bool IsWeaponSlot(EquipmentIndex slot) =>
            slot >= EquipmentIndex.Weapon0 && slot <= EquipmentIndex.Weapon3;
    }
}
