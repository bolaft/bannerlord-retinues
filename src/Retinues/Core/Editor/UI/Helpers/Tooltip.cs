using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Editor.UI.Helpers
{
    public static class Tooltip
    {
        public static BasicTooltipViewModel MakeTooltip(string title, string description)
        {
            return new BasicTooltipViewModel(() =>
            {
                var props = new List<TooltipProperty>();
                if (!string.IsNullOrEmpty(title))
                    props.Add(new TooltipProperty(
                        "", title, 0, false,
                        TooltipProperty.TooltipPropertyFlags.Title
                    ));
                if (!string.IsNullOrEmpty(description))
                    props.Add(new TooltipProperty(
                        "", description, 0, false,
                        TooltipProperty.TooltipPropertyFlags.None
                    ));
                return props;
            });
        }

        public static BasicTooltipViewModel MakeItemTooltip(WItem item)
        {
            if (item == null)
                return null;

            return new BasicTooltipViewModel(() =>
            {
                var props = new List<TooltipProperty>
                {
                    // Title
                    new TooltipProperty(
                    string.Empty, item.Class,
                    0, false, TooltipProperty.TooltipPropertyFlags.Title
                )
                };

                // Description
                foreach (var stat in item.Statistics)
                {
                    props.Add(new TooltipProperty(
                        string.Empty, $"{stat.Key}: {stat.Value}", 0, false,
                        TooltipProperty.TooltipPropertyFlags.None
                    ));
                }

                return props;
            });
        }
    }
}