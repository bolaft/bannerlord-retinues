using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Framework.Behaviors;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Game.Staging
{
    public sealed class StagedItemProgressBehavior : BaseCampaignBehavior
    {
        public override bool IsActive => Settings.EquippingTakesTime;

        protected override void OnHourlyTick()
        {
            if (!Settings.EquippingProgressesWhileTravelling && Player.CurrentSettlement == null)
                return;

            float timeMult = Settings.EquipTimeMultiplier;
            if (timeMult <= 0.01f)
                timeMult = 0.01f;

            // Base: 1 "work hour" per game hour.
            float progressDelta = 1f;

            bool changed = false;

            foreach (var wc in WCharacter.All)
            {
                if (wc == null || wc.IsHero)
                    continue;

                if (!MEquipment.IsItemStagingActive(wc))
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

                    me.ItemStagingProgress = MathF.Max(0f, me.ItemStagingProgress + progressDelta);

                    int safety = 0;

                    while (me.HasAnyStagedItems() && safety < 128)
                    {
                        float requiredHours = me.GetNextStagedHours(timeMult);

                        if (
                            requiredHours > 0.001f
                            && me.ItemStagingProgress + 0.0001f < requiredHours
                        )
                            break;

                        if (!me.TryApplyNextStagedItem(out var slot, out var item, out var unequip))
                            break;

                        if (requiredHours > 0.001f)
                            me.ItemStagingProgress = MathF.Max(
                                0f,
                                me.ItemStagingProgress - requiredHours
                            );

                        changed = true;
                        safety++;

                        var troopName = wc.Name?.ToString() ?? wc.StringId;
                        var slotName = slot.ToString();

                        if (unequip || item == null)
                        {
                            // Should not happen.
                        }
                        else
                        {
                            var itemName = item.Name?.ToString() ?? item.StringId;

                            Notifications.Message(
                                L.T("staged_item_equipped", "{TROOP} finished equipping {ITEM}.")
                                    .SetTextVariable("TROOP", troopName)
                                    .SetTextVariable("ITEM", itemName)
                            );
                        }
                    }

                    if (!me.HasAnyStagedItems())
                        me.ItemStagingProgress = 0f;
                }
            }

            if (changed)
                Log.Debug("Applied staged equipment changes.");
        }
    }
}
