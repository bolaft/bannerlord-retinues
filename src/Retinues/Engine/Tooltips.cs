using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace Retinues.Engine
{
    /// <summary>
    /// Tooltip with an optional title and a single message line.
    /// </summary>
    public class Tooltip(string title, string message)
        : BasicTooltipViewModel(() => BuildProperties(title, message))
    {
        public Tooltip(string message)
            : this(null, message) { }

        private static List<TooltipProperty> BuildProperties(string title, string message)
        {
            var props = new List<TooltipProperty>();

            if (!string.IsNullOrEmpty(title))
            {
                props.Add(
                    new TooltipProperty(
                        "",
                        title,
                        0,
                        false,
                        TooltipProperty.TooltipPropertyFlags.Title
                    )
                );
            }

            if (!string.IsNullOrEmpty(message))
            {
                props.Add(
                    new TooltipProperty(
                        "",
                        message,
                        0,
                        false,
                        TooltipProperty.TooltipPropertyFlags.None
                    )
                );
            }

            return props;
        }
    }
}
