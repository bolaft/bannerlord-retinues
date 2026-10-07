using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Behaviors.Unlocks.Patches;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace Retinues.Tests.Cases
{
    public static class TooltipCompatibilityTests
    {
#if BL13 || BL14
        [GameTest("EditorTooltipRowsSurviveGameConstructorChanges", "compatibility", RequiresCampaign = false)]
        public static void EditorTooltipRowsSurviveGameConstructorChanges()
        {
            var tooltip = new Retinues.Interface.Components.Tooltip("Saved troop", "Élite <&> 日本");
            var callback = (Func<List<TooltipProperty>>)AccessTools.Field(typeof(BasicTooltipViewModel), "_tooltipProperties").GetValue(tooltip);
            var rows = callback();
            Tests.AssertEqual(2, rows.Count);
            Tests.AssertEqual("Saved troop", rows[0].ValueLabel);
            Tests.AssertEqual((int)TooltipProperty.TooltipPropertyFlags.Title, rows[0].PropertyModifier);
            Tests.AssertEqual("Élite <&> 日本", rows[1].ValueLabel);
            Tests.AssertEqual((int)TooltipProperty.TooltipPropertyFlags.None, rows[1].PropertyModifier);
            Tests.AssertFalse(rows[1].OnlyShowWhenExtended);
        }
#endif

        [GameTest("UnlockTooltipRowsSurviveGameConstructorChanges", "compatibility", RequiresCampaign = false)]
        public static void UnlockTooltipRowsSurviveGameConstructorChanges()
        {
            var items = Enumerable.Range(0, 10).Select(i =>
            {
                var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
                item.StringId = "compatibility_item_" + i;
                return new WItem(item);
            }).ToList();
            var rows = (List<TooltipProperty>)typeof(ScoreboardUnlocksPatch)
                .GetMethod("BuildTooltip", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { items });
            Tests.AssertEqual(10, rows.Count, "Title, eight items and overflow row survive.");
            Tests.AssertEqual((int)TooltipProperty.TooltipPropertyFlags.Title, rows[0].PropertyModifier);
            Tests.AssertEqual("compatibility_item_0", rows[1].ValueLabel);
            Tests.AssertEqual("compatibility_item_7", rows[8].ValueLabel);
            Tests.AssertEqual("...", rows[9].ValueLabel);
        }
    }
}
