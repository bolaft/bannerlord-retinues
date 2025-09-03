namespace CustomClanTroops.Utils
{
    public static class Format
    {
        public static string CamelCaseToTitle(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Insert spaces before capital letters and convert to title case
            var result = System.Text.RegularExpressions.Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result);
        }
    }
}
