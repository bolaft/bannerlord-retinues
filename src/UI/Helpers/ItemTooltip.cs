using System.Collections.Generic;
using System.Reflection;
using CustomClanTroops.Wrappers.Objects;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace CustomClanTroops.UI.Helpers
{
    public static class ItemTooltip
    {
        private static readonly TooltipProperty.TooltipPropertyFlags Title = TooltipProperty.TooltipPropertyFlags.Title;
        private static readonly TooltipProperty.TooltipPropertyFlags Description = TooltipProperty.TooltipPropertyFlags.None;

        private static WItem _itemForTooltip;

        public static BasicTooltipViewModel Make(WItem item)
        {
            if (item == null) return null;

            _itemForTooltip = item;
            return new BasicTooltipViewModel(MakeProperties);
        }

        public static List<TooltipProperty> MakeProperties()
        {
            var properties = new List<TooltipProperty>
            {
                // Title line
                new(
                    "", $"{_itemForTooltip.Class} (Tier {_itemForTooltip.Tier})",
                    0, false, Title
                )
            };

            foreach (var statistic in _itemForTooltip.Statistics)
            {
                // One property per statistic
                properties.Add(new TooltipProperty(
                    $"{statistic.Key}: {statistic.Value}", "",
                    0, false, Description
                ));
            }

            return properties;
        }
    }
}