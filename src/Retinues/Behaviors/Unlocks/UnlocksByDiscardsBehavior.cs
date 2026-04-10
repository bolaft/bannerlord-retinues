using System;
using System.Collections.Generic;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Behaviors.Unlocks
{
    /// <summary>
    /// Applies equipment unlock progress from items discarded by the player.
    /// </summary>
    public sealed class UnlocksByDiscardsBehavior : BaseCampaignBehavior
    {
        public override bool IsActive => Configuration.UnlockItemsThroughDiscards;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called when the player discards items.
        /// </summary>
        protected override void OnItemsDiscardedByPlayer(ItemRoster roster)
        {
            if (roster == null || roster.Count == 0)
                return;

            var required = (int)Configuration.RequiredDiscardsToUnlock;
            if (required <= 0)
                return;

            var perDiscard = Math.Max(1, WItem.UnlockThreshold / required);

            var itemsTouched = 0;
            var unlocked = new List<WItem>();
            long totalAdded = 0;

            for (var i = 0; i < roster.Count; i++)
            {
                var e = roster[i];
                if (e.Amount <= 0)
                    continue;

                var item = e.EquipmentElement.Item;
                if (item == null)
                    continue;

                var wItem = WItem.Get(item);
                if (wItem == null || !wItem.IsValidEquipment)
                    continue;

                var add = perDiscard * e.Amount;
                if (add <= 0)
                    continue;

                var wasUnlocked = wItem.IsUnlocked;
                var isUnlocked = wItem.IncreaseUnlockProgress(add);

                itemsTouched++;
                totalAdded += add;

                if (!wasUnlocked && isUnlocked)
                    unlocked.Add(wItem);
            }

            if (unlocked.Count > 0)
                ItemUnlockNotifier.ItemsUnlocked(
                    ItemUnlockNotifier.UnlockMethod.Discards,
                    unlocked
                );

            if (itemsTouched > 0)
            {
                Log.Debug(
                    $"[Unlocks] Discard progress applied: items={itemsTouched}, newlyUnlocked={unlocked.Count}, totalAdded={totalAdded}."
                );
            }
        }
    }
}
