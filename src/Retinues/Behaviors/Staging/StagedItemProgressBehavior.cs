using System;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Interface.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Staging
{
    /// <summary>
    /// Advances staged equipping progress for NPC troops and applies items when ready.
    /// </summary>
    public sealed class StagedItemProgressBehavior : BaseCampaignBehavior
    {
        public override bool IsActive => Configuration.EquippingTakesTime;

        /// <summary>
        /// Hourly tick handler that progresses staged equipment and finalizes equips when thresholds are met.
        /// </summary>
        protected override void OnHourlyTick()
        {
            if (
                !Configuration.EquippingProgressesWhileTravelling
                && Player.CurrentSettlement == null
            )
                return;

            float timeMult = Configuration.EquipTimeMultiplier;
            if (timeMult <= 0.01f)
                timeMult = 0.01f;

            // Base: 1 "work hour" per game hour.
            float progressDelta = 1f;

            bool changed = false;

            foreach (var wc in WCharacter.All)
            {
                if (wc == null || wc.IsHero)
                    continue;

                var list = wc.Equipments;
                if (list == null || list.Count == 0)
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var me = list[i];
                    if (me == null)
                        continue;

                    if (!me.HasAnyStagedItems())
                        continue;

                    changed |= AdvanceEquipment(me, progressDelta, timeMult, item =>
                    {
                        if (item != null)
                        {
                            Notifications.Message(
                                L.T("staged_item_equipped", "{TROOP} finished equipping {ITEM}.")
                                    .SetTextVariable("TROOP", wc.Name?.ToString() ?? wc.StringId)
                                    .SetTextVariable("ITEM", item.Name ?? item.StringId)
                            );
                        }
                    });
                }
            }

            if (changed)
                Log.Debug("Applied staged equipment changes.");
        }

        internal static bool AdvanceEquipment(
            MEquipment equipment, float hours, float timeMultiplier, Action<WItem> onEquipped = null
        )
        {
            if (equipment == null || !equipment.HasAnyStagedItems())
                return false;

            // The editor mode controls new edits, never work already queued in the campaign.
            bool changed = equipment.ScrubInvalidItems() > 0;
            if (float.IsPositiveInfinity(equipment.GetNextStagedHours(timeMultiplier)))
                return changed; // Keep unresolved saved work and its progress until it is available.
            equipment.ItemStagingProgress = MathF.Max(0f, equipment.ItemStagingProgress + hours);
            int safety = 0;
            while (equipment.HasAnyStagedItems() && safety++ < 128)
            {
                float required = equipment.GetNextStagedHours(timeMultiplier);
                if (required > 0.001f && equipment.ItemStagingProgress + 0.0001f < required)
                    break;

                if (!equipment.TryApplyNextStagedItem(out _, out var item, out _))
                    continue;

                if (required > 0.001f)
                    equipment.ItemStagingProgress = MathF.Max(0f, equipment.ItemStagingProgress - required);
                changed = true;
                onEquipped?.Invoke(item);
            }

            if (!equipment.HasAnyStagedItems())
                equipment.ItemStagingProgress = 0f;
            return changed;
        }
    }
}
