namespace Retinues.Core.Utils
{
    /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */
    /*                              String Formatting                             */
    /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */

    public static class Format
    {
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
    }
}
