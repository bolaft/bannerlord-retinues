using System;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.GUI.Services;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Modules.Pages.Equipment.Services
{
    public static class EquipRules
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlock                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool UnlockAllowed(EquipContext ctx, WItem item)
        {
            if (item == null)
                return true;

            if (ctx.PreviewEnabled)
                return true;

            if (ctx.Mode != EditorMode.Player)
                return true;

            if (item.IsCrafted)
                return true;

            return item.IsUnlocked;
        }

        public static TextObject UnlockTooltip(WItem item)
        {
            if (item == null)
                return L.T("cant_equip_reason_null", "Invalid item");

            double progress = Convert.ToDouble(item.UnlockProgress);
            double percentDouble = progress / WItem.UnlockThreshold * 100.0;
            int percent = (int)Math.Round(percentDouble, MidpointRounding.AwayFromZero);

            if (percent < 0)
                percent = 0;
            if (percent > 100)
                percent = 100;

            if (percent > 0)
            {
                return L.T("cant_equip_reason_unlocking", "Unlocking {PERCENT}%")
                    .SetTextVariable("PERCENT", percent);
            }

            return L.T("cant_equip_reason_locked", "Locked");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Validation                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EquipDecision CanSetItem(
            EquipContext ctx,
            Func<EquipmentIndex, WItem> getPlanned,
            EquipmentIndex slot,
            WItem item
        )
        {
            // Unequip is always allowed.
            if (item == null)
                return EquipDecision.Ok();

            if (!UnlockAllowed(ctx, item))
                return EquipDecision.Skip(EquipSkipReason.Locked, UnlockTooltip(item));

            int tier = ctx.Character?.Tier ?? 0;

            if (ctx.LimitsEnabled && !item.IsEquippableByCharacterOfTier(tier))
                return EquipDecision.Skip(
                    EquipSkipReason.Tier,
                    L.T("cant_equip_reason_mount_disallowed", "Too low tier")
                );

            if (!item.IsEquippableByCharacter(ctx.Character))
            {
                return EquipDecision.Skip(
                    EquipSkipReason.Skill,
                    L.T("cant_equip_reason_skill", "{SKILL} {VALUE}")
                        .SetTextVariable("VALUE", item.Difficulty)
                        .SetTextVariable("SKILL", item.RelevantSkill?.Name)
                );
            }

            if (ctx.Equipment?.IsCivilian == true && !item.IsCivilian)
                return EquipDecision.Skip(
                    EquipSkipReason.CivilianMismatch,
                    L.T("cant_equip_reason_civilian", "Not civilian")
                );

            if (slot == EquipmentIndex.HorseHarness)
            {
                var horse = getPlanned(EquipmentIndex.Horse);
                if (horse != null && !horse.IsCompatibleWith(item))
                    return EquipDecision.Skip(
                        EquipSkipReason.Incompatible,
                        L.T("cant_equip_reason_mount_compat", "Incompatible")
                    );
            }

            // Limits (weight/value)
            if (ctx.WeightLimitActive || ctx.ValueLimitActive)
            {
                int t = ctx.Character?.Tier ?? 0;

                float weightLimit = EquipmentLimitsHelper.GetWeightLimit(
                    t,
                    Configuration.Settings.EquipmentWeightLimitMultiplier
                );

                int valueLimit = EquipmentLimitsHelper.GetValueLimit(
                    t,
                    Configuration.Settings.EquipmentValueLimitMultiplier
                );

                bool fits = EquipmentLimitsHelper.FitsLimitsAfterSet(
                    getPlanned,
                    slot,
                    item,
                    weightLimitActive: ctx.WeightLimitActive,
                    weightLimit: weightLimit,
                    valueLimitActive: ctx.ValueLimitActive,
                    valueLimit: valueLimit,
                    allowNonIncreasingWhenOver: true
                );

                if (!fits)
                {
                    return EquipDecision.Skip(
                        EquipSkipReason.Limits,
                        L.T("cant_equip_reason_limits", "Limits exceeded")
                    );
                }
            }

            return EquipDecision.Ok();
        }
    }
}
