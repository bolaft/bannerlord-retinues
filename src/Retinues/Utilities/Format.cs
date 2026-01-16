using System;
using System.Globalization;
using System.Text;

namespace Retinues.Utilities
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                    String Formatting                   //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Utility for formatting strings for display, e.g. converting code-style names to readable titles.
    /// </summary>
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
            text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);

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

        /// <summary>
        /// Format an integer using a space as the thousands separator (e.g. "1 234 567").
        /// </summary>
        public static string ToNumber(int value)
        {
            var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberGroupSeparator = " ";
            return value.ToString("N0", nfi);
        }

        /// <summary>
        /// Convert an integer to a Roman numeral (1..3999).
        /// </summary>
        public static string ToRoman(int value)
        {
            if (value <= 0 || value > 3999)
                return value.ToString();

            // Ordered from largest to smallest, including subtractive forms.
            int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };

            string[] symbols =
            {
                "M",
                "CM",
                "D",
                "CD",
                "C",
                "XC",
                "L",
                "XL",
                "X",
                "IX",
                "V",
                "IV",
                "I",
            };

            var sb = new StringBuilder(16);

            for (int i = 0; i < values.Length; i++)
            {
                int v = values[i];
                string sym = symbols[i];

                while (value >= v)
                {
                    sb.Append(sym);
                    value -= v;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Pad a string on the left to a fixed width (for aligned numeric columns).
        /// </summary>
        public static string PadLeft(string text, int width)
        {
            text ??= "";
            if (width <= 0 || text.Length >= width)
                return text;

            return new string(' ', width - text.Length) + text;
        }

        /// <summary>
        /// Pad a string on the right to a fixed width (for aligned columns).
        /// </summary>
        public static string PadRight(string text, int width)
        {
            text ??= "";
            if (width <= 0 || text.Length >= width)
                return text;

            return text + new string(' ', width - text.Length);
        }

        /// <summary>
        /// Format an integer with a leading '+' when non-negative.
        /// Uses ToNumber() for grouping.
        /// </summary>
        public static string Signed(int value)
        {
            var n = ToNumber(value);
            return value >= 0 ? "+" + n : n;
        }

        /// <summary>
        /// Format "+X label" (e.g. "+10 386 gold").
        /// </summary>
        public static string PlusLabel(int value, string label)
        {
            return $"{Signed(value)} {label}";
        }
    }
}
