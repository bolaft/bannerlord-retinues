using Retinues.Framework.Runtime;
using TaleWorlds.Localization;
#if BL13
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TooltipBase = TaleWorlds.Core.ViewModelCollection.Information.BasicTooltipViewModel;
#else
using TooltipBase = TaleWorlds.Core.ViewModelCollection.Information.HintViewModel;
#endif

namespace Retinues.Interface.Components
{
    /// <summary>
    /// Small tooltip wrapper used by the UI. In BL13 it is a BasicTooltipViewModel; in BL12 it is a HintViewModel.
    /// </summary>
    [SafeClass]
    public class Tooltip : TooltipBase
    {
        /// <summary>
        /// Creates a tooltip with an optional title and message.
        /// </summary>
        public Tooltip(string title, string message)
#if BL13
            : base(() => BuildProperties(title, message))
#else
            : base(BuildHint(title, message))
#endif
        { }

        /// <summary>
        /// Creates a tooltip with only a message line.
        /// </summary>
        public Tooltip(string message)
            : this(null, message) { }

        /// <summary>
        /// Creates a tooltip from a localized message TextObject.
        /// </summary>
        public Tooltip(TextObject message)
            : this(message?.ToString()) { }

        /// <summary>
        /// Creates a tooltip from localized title and message TextObjects.
        /// </summary>
        public Tooltip(TextObject title, TextObject message)
            : this(title?.ToString(), message?.ToString()) { }

#if BL12
        /// <summary>
        /// BL12: builds a HintViewModel TextObject (title + newline + message).
        /// </summary>
        private static TextObject BuildHint(string title, string message)
        {
            var text = BuildText(title, message);
            return new TextObject("{=!}" + text);
        }
#endif

        /// <summary>
        /// Builds the merged display text (used by BL12 and as a fallback formatting rule).
        /// </summary>
        private static string BuildText(string title, string message)
        {
            if (string.IsNullOrEmpty(title))
                return message ?? string.Empty;

            if (string.IsNullOrEmpty(message))
                return title ?? string.Empty;

            return $"{title}\n{message}";
        }

#if BL13
        /// <summary>
        /// BL13: builds the tooltip property list (title row + message row).
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
#endif
    }
}
