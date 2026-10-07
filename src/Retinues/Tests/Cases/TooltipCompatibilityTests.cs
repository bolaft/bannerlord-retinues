using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace Retinues.Tests.Cases
{
    public static class TooltipCompatibilityTests
    {
        [GameTest("EditorTooltipRowsSurviveGameConstructorChanges", "compatibility", RequiresCampaign = false)]
        public static void EditorTooltipRowsSurviveGameConstructorChanges()
        {
            var tooltip = Retinues.GUI.Helpers.Tooltip.MakeTooltip("Saved troop", "Élite <&> 日本");
            var callback = (Func<List<TooltipProperty>>)AccessTools.Field(typeof(BasicTooltipViewModel), "_tooltipProperties").GetValue(tooltip);
            var rows = callback();
            Tests.AssertEqual(2, rows.Count);
            Tests.AssertEqual("Saved troop", rows[0].ValueLabel);
            Tests.AssertEqual((int)TooltipProperty.TooltipPropertyFlags.Title, rows[0].PropertyModifier);
            Tests.AssertEqual("Élite <&> 日本", rows[1].ValueLabel);
            Tests.AssertEqual((int)TooltipProperty.TooltipPropertyFlags.None, rows[1].PropertyModifier);
            Tests.AssertFalse(rows[1].OnlyShowWhenExtended);
        }
    }
}
