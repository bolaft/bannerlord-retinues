using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Interface.Services;
using Retinues.Utilities;
using SandBox.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.MountAndBlade;

namespace Retinues.Behaviors.Unlocks.Patches
{
    /// <summary>
    /// Adds scoreboard entries for items unlocked via mission kills.
    /// </summary>
    [HarmonyPatch]
    [SafeClass]
    internal static class ScoreboardUnlocksPatch
    {
        // SPScoreboardVM.GetBattleRewards(bool) is private.
        /// <summary>
        /// Postfix that applies unlock progress from mission kills when the player wins.
        /// </summary>
        [HarmonyPatch(typeof(SPScoreboardVM), "GetBattleRewards")]
        [HarmonyPostfix]
        private static void Postfix_GetBattleRewards(SPScoreboardVM __instance, bool playerVictory)
        {
            try
            {
                if (!playerVictory)
                    return;

                if (!Settings.EquipmentNeedsUnlocking || !Settings.UnlockItemsThroughKills)
                    return;

                var mission = Mission.Current;
                if (mission == null)
                    return;

                // IMPORTANT:
                // We still want the normal unlock notification (popup/message), which is delayed
                // to MapState by UnlockNotifierBehavior. This patch only adds scoreboard lines.
                var unlocked = UnlocksByKillsBehavior.ApplyProgressFromMissionKills(mission);

                Log.Debug(
                    $"ScoreboardUnlocksPatch: {unlocked.Count} items unlocked from mission kills."
                );

                if (unlocked == null || unlocked.Count == 0)
                    return;

                InsertUnlockLines(__instance, unlocked);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "SPScoreboardVM.GetBattleRewards unlock-lines patch failed.");
            }
        }

        /// <summary>
        /// Inserts human-readable unlock lines into the scoreboard results view model.
        /// </summary>
        private static void InsertUnlockLines(SPScoreboardVM vm, IReadOnlyList<WItem> unlocked)
        {
            if (vm?.BattleResults == null || unlocked == null || unlocked.Count == 0)
                return;

            // Insert right after the renown/influence/morale/loot lines. Those entries have
            // null portraits, while the dead-lord entries have a portrait.
            var insertIndex = vm.BattleResults.Count;
            for (var i = 0; i < vm.BattleResults.Count; i++)
            {
                var r = vm.BattleResults[i];
                if (r?.DeadLordPortrait != null)
                {
                    insertIndex = i;
                    break;
                }
            }

            // Compact rules:
            // - Up to 3 unlocked items => 1 line per item.
            // - More than 3 => one aggregated line.
            if (unlocked.Count <= 3)
            {
                for (var i = 0; i < unlocked.Count; i++)
                {
                    var name = GetItemName(unlocked[i]);
                    if (string.IsNullOrEmpty(name))
                        continue;

                    var text = L.T("item_unlock_scoreboard_line", "Item unlocked: {ITEM}.")
                        .SetTextVariable("ITEM", name)
                        .ToString();

                    vm.BattleResults.Insert(
                        insertIndex,
                        new BattleResultVM(text, () => BuildTooltip(unlocked))
                    );
                    insertIndex++;
                }

                return;
            }

            var manyText = L.T("item_unlock_scoreboard_many", "You unlocked {COUNT} new items.")
                .SetTextVariable("COUNT", unlocked.Count)
                .ToString();

            vm.BattleResults.Insert(
                insertIndex,
                new BattleResultVM(manyText, () => BuildTooltip(unlocked))
            );
        }

        /// <summary>
        /// Builds a compact tooltip listing unlocked items (capped for responsiveness).
        /// </summary>
        private static List<TooltipProperty> BuildTooltip(IReadOnlyList<WItem> unlocked)
        {
            var props = new List<TooltipProperty>();

            if (unlocked == null || unlocked.Count == 0)
                return props;

            props.Add(
                new TooltipProperty(
                    "",
                    L.T("item_unlock_scoreboard_tooltip_title", "Unlocked items").ToString(),
                    0,
                    false,
                    TooltipProperty.TooltipPropertyFlags.Title
                )
            );

            // Cap to keep tooltips responsive even if a lot of items unlock at once.
            const int maxItems = 8;
            var take = Math.Min(maxItems, unlocked.Count);

            for (var i = 0; i < take; i++)
            {
                var name = GetItemName(unlocked[i]);
                if (string.IsNullOrEmpty(name))
                    continue;

                props.Add(
                    new TooltipProperty(
                        "",
                        name,
                        0,
                        false,
                        TooltipProperty.TooltipPropertyFlags.None
                    )
                );
            }

            if (unlocked.Count > take)
            {
                props.Add(
                    new TooltipProperty(
                        "",
                        "...",
                        0,
                        false,
                        TooltipProperty.TooltipPropertyFlags.None
                    )
                );
            }

            return props;
        }

        /// <summary>
        /// Returns a display name for the unlocked item, falling back to the item id.
        /// </summary>
        private static string GetItemName(WItem item)
        {
            var name = item?.Base?.Name?.ToString();
            if (!string.IsNullOrEmpty(name))
                return name;

            return item?.StringId ?? string.Empty;
        }
    }
}
