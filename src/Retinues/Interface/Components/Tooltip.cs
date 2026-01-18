using System.Collections.Generic;
using Retinues.Framework.Runtime;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;

namespace Retinues.Interface.Components
{
    /// <summary>
    /// Tooltip with an optional title and a single message line.
    /// </summary>
    [SafeClass]
    public class Tooltip(string title, string message)
        : BasicTooltipViewModel(() => BuildProperties(title, message))
    {
        /// <summary>
        /// Creates a Tooltip with only a message line.
        /// </summary>
        public Tooltip(string message)
            : this(null, message) { }

        /// <summary>
        /// Creates a Tooltip from a localized message TextObject.
        /// </summary>
        public Tooltip(TextObject message)
            : this(message?.ToString()) { }

        /// <summary>
        /// Creates a Tooltip from localized title and message TextObjects.
        /// </summary>
        public Tooltip(TextObject title, TextObject message)
            : this(title?.ToString(), message?.ToString()) { }

        /// <summary>
        /// Builds the tooltip property list from title and message strings.
        /// </summary>
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
