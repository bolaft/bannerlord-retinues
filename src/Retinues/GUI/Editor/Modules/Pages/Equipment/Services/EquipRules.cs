using System;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.GUI.Services;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Modules.Pages.Equipment.Services
{
    /// <summary>
    /// Service for equipment rules and validations.
    /// </summary>
    public static class EquipRules
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlock                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines if unlocking is allowed for the given item in the current context.
        /// </summary>
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

        /// <summary>
        /// Provides a tooltip explaining the unlock status of the given item.
        /// </summary>
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

        /// <summary>
        /// Checks if the given item can be set in the specified slot within the provided context.
        /// </summary>
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

                var current = EquipmentLimitsHelper.GetTotals(getPlanned);
                var next = EquipmentLimitsHelper.GetTotals(getPlanned, slot, item);

                bool weightFits =
                    !ctx.WeightLimitActive
                    || EquipmentLimitsHelper.FitsWeight(
                        current,
                        next,
                        weightLimit,
                        allowNonIncreasingWhenOver: true
                    );

                bool valueFits =
                    !ctx.ValueLimitActive
                    || EquipmentLimitsHelper.FitsValue(
                        current,
                        next,
                        valueLimit,
                        allowNonIncreasingWhenOver: true
                    );

                if (!weightFits)
                {
                    return EquipDecision.Skip(
                        EquipSkipReason.Limits,
                        L.T("cant_equip_reason_too_heavy", "Too heavy")
                    );
                }

                if (!valueFits)
                {
                    return EquipDecision.Skip(
                        EquipSkipReason.Limits,
                        L.T("cant_equip_reason_too_valuable", "Too valuable")
                    );
                }
            }

            return EquipDecision.Ok();
        }
    }
}
