namespace Retinues.Utils
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                    String Formatting                   //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Utility for formatting strings for display, e.g. converting code-style names to readable titles.
    /// </summary>
    [SafeClass]
    public static class Format
    {
        /// <summary>
        /// Converts a camelCase or PascalCase string (optionally with underscores) to a human-readable title.
        /// Example: "maxEliteRetinueRatio" → "Max Elite Retinue Ratio"
        /// </summary>
        public static string CamelCaseToTitle(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Replace underscores with spaces
            text = text.Replace('_', ' ');
            // Insert spaces before capital letters
            text = System.Text.RegularExpressions.Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
            // Convert to title case
            text = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);

            return text;
        }

        /// <summary>
        /// Crops a string to a maximum length, appending "(...)" if it was cropped.
        /// </summary>
        public static string Crop(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (text.Length > maxLength)
                return text.Substring(0, maxLength) + "(...)";
            return text;
        }
    }
}
