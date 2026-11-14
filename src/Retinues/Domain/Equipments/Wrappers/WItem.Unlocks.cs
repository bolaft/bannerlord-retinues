using System.Collections.Generic;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Framework.Model.Attributes;
using Retinues.Settings;
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

        MAttribute<int> UnlockProgressAttribute =>
            Attribute<int>(initialValue: 0, name: "UnlockProgressAttribute");

        public bool IsUnlocked => UnlockProgress >= UnlockThreshold;

        public int UnlockProgress
        {
            get
            {
                if (!Configuration.EquipmentNeedsUnlocking)
                    return UnlockThreshold; // Always unlocked.

                // Special case: Ancestral Heritage doctrine.
                if (DoctrineCatalog.AncestralHeritage.IsAcquired)
                    if (Player.Clan.Culture == Culture)
                        return UnlockThreshold; // Always unlocked for matching culture.

                return UnlockProgressAttribute.Get();
            }
            set
            {
                value = System.Math.Max(value, 0);
                value = System.Math.Min(value, UnlockThreshold);
                UnlockProgressAttribute.Set(value);
            }
        }

        /// <summary>
        /// Increases the unlock progress by the given amount,
        /// capping it at the unlock threshold.
        /// </summary>
        public bool IncreaseUnlockProgress(int amount)
        {
            if (amount <= 0)
                return IsUnlocked;

            if (IsUnlocked)
                return true;

            UnlockProgress = System.Math.Min(UnlockProgress + amount, UnlockThreshold);
            return IsUnlocked;
        }

        /// <summary>
        /// Immediately unlocks this item for the player.
        /// </summary>
        public void Unlock() => UnlockProgress = UnlockThreshold;

        /// <summary>
        /// Clears earned unlocks and partial progress without changing stocks or access overrides.
        /// </summary>
        internal static int ResetUnlockProgress(IEnumerable<WItem> items)
        {
            int count = 0;
            foreach (var item in items)
            {
                // The public getter includes settings/doctrine overrides; reset the saved value.
                var progress = item.UnlockProgressAttribute;
                if (progress.Get() == 0)
                    continue;

                progress.Set(0); // Mark dirty so the cleared progress survives the next save.
                count++;
            }
            return count;
        }

        /// <summary>
        /// Gets all unlocked items that can be equipped in the given slot.
        /// </summary>
        public static IEnumerable<WItem> GetUnlockedItems(EquipmentIndex slot)
        {
            foreach (var item in GetEquipmentsForSlot(slot))
            {
                if (item.IsUnlocked)
                    yield return item;
            }
        }
    }
}
