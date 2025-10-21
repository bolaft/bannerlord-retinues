using System.Collections.Generic;
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
    }
}
