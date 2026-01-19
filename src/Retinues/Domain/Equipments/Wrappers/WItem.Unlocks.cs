using System.Collections.Generic;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Model.Attributes;
using TaleWorlds.Core;

namespace Retinues.Domain.Equipments.Wrappers
{
    public partial class WItem
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlocks                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The unlock progress required to unlock this item.
        /// </summary>
        public const int UnlockThreshold = 1000;

        MAttribute<Dictionary<string, int>> UnlockProgressByHeroAttribute =>
            Attribute<Dictionary<string, int>>(initialValue: []);

        public bool IsUnlocked => IsUnlockedFor(Player.Hero);

        /// <summary>
        /// Indicates whether this item is unlocked for the given hero.
        /// </summary>
        public bool IsUnlockedFor(WHero hero) => GetUnlockProgress(hero) >= UnlockThreshold;

        public int UnlockProgress
        {
            get => GetUnlockProgress(Player.Hero);
            set => SetUnlockProgress(Player.Hero, value);
        }

        /// <summary>
        /// Gets the unlock progress for the given hero.
        /// </summary>
        public int GetUnlockProgress(WHero hero)
        {
            if (!Settings.EquipmentNeedsUnlocking)
                return UnlockThreshold; // Always unlocked.

            // Special case: Ancestral Heritage doctrine.
            if (DoctrineCatalog.AncestralHeritage.IsAcquired)
                if (Player.Clan.Culture == Culture)
                    return UnlockThreshold; // Always unlocked for matching culture.

            var map = UnlockProgressByHeroAttribute.Get() ?? [];

            if (map.TryGetValue(hero.StringId, out var value))
                return value;

            return 0;
        }

        /// <summary>
        /// Sets the unlock progress to the given value for the given hero.
        /// </summary>
        public void SetUnlockProgress(WHero hero, int value)
        {
            value = System.Math.Max(value, 0);
            value = System.Math.Min(value, UnlockThreshold);

            var current = UnlockProgressByHeroAttribute.Get() ?? [];
            var map = new Dictionary<string, int>(current) { [hero.StringId] = value };
            UnlockProgressByHeroAttribute.Set(map);
        }

        /// <summary>
        /// Increases the unlock progress by the given amount for the player,
        /// capping it at the unlock threshold.
        /// </summary>
        public bool IncreaseUnlockProgress(int amount) =>
            IncreaseUnlockProgress(Player.Hero, amount);

        /// <summary>
        /// Increases the unlock progress by the given amount for the given hero,
        /// capping it at the unlock threshold.
        public bool IncreaseUnlockProgress(WHero hero, int amount)
        {
            if (amount <= 0)
                return IsUnlockedFor(hero);

            if (IsUnlockedFor(hero))
                return true;

            SetUnlockProgress(
                hero,
                System.Math.Min(GetUnlockProgress(hero) + amount, UnlockThreshold)
            );

            return IsUnlockedFor(hero);
        }

        /// <summary>
        /// Immediately unlocks this item for the player.
        /// </summary>
        public void Unlock() => Unlock(Player.Hero);

        /// <summary>
        /// Immediately unlocks this item for the given hero.
        /// </summary>
        public void Unlock(WHero hero)
        {
            SetUnlockProgress(hero, UnlockThreshold);
        }

        /// <summary>
        /// Gets all unlocked items that can be equipped in the given slot.
        /// </summary>
        public static IEnumerable<WItem> GetUnlockedItems(EquipmentIndex slot) =>
            GetUnlockedItems(slot, Player.Hero);

        public static IEnumerable<WItem> GetUnlockedItems(EquipmentIndex slot, WHero hero)
        {
            foreach (var item in GetEquipmentsForSlot(slot))
            {
                if (item.IsUnlockedFor(hero))
                    yield return item;
            }
        }
    }
}
