using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace Retinues.GUI.Helpers
{
    /// <summary>
    /// Static helpers for building tooltips for UI elements and items.
    /// </summary>
    [SafeClass]
    public static class Tooltip
    {
        /// <summary>
        /// Builds a tooltip with a title and description.
        /// </summary>
        public static BasicTooltipViewModel MakeTooltip(string title, string description)
        {
            return new BasicTooltipViewModel(() =>
            {
                var props = new List<TooltipProperty>();
                if (!string.IsNullOrEmpty(title))
                    props.Add(
                        new TooltipProperty(
                            "",
                            title,
                            0,
                            false,
                            TooltipProperty.TooltipPropertyFlags.Title
                        )
                    );
                if (!string.IsNullOrEmpty(description))
                    props.Add(
                        new TooltipProperty(
                            "",
                            description,
                            0,
                            false,
                            TooltipProperty.TooltipPropertyFlags.None
                        )
                    );
                return props;
            });
        }

        /// <summary>
        /// Builds a tooltip for an item, showing tier, class, culture, and statistics.
        /// </summary>
        public static BasicTooltipViewModel MakeItemTooltip(WItem item)
        {
            if (item == null)
                return null;

            return new BasicTooltipViewModel(() =>
            {
                string titleText;

                if (item.Culture?.Name != null)
                    titleText = $"T{item.Tier} {item.Class} ({item.Culture.Name})";
                else
                    titleText = $"T{item.Tier} {item.Class}";

                var props = new List<TooltipProperty>
                {
                    // Title
                    new(
                        string.Empty,
                        titleText,
                        0,
                        false,
                        TooltipProperty.TooltipPropertyFlags.Title
                    ),
                };

                // Description
                foreach (var stat in item.Statistics)
                {
                    props.Add(
                        new TooltipProperty(
                            string.Empty,
                            $"{stat.Key}: {stat.Value}",
                            0,
                            false,
                            TooltipProperty.TooltipPropertyFlags.None
                        )
                    );
                }

                return props;
            });
        }
    }
}
