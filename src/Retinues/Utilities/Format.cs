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

        /// <summary>
        /// Wrap a text to a maximum line length by replacing whitespace with '\n' when needed.
        /// Preserves existing line breaks. Multiple spaces/tabs are treated as a single separator.
        /// If a single word is longer than maxLineLength, it is hard-broken.
        /// </summary>
        public static string WrapWhitespace(string text, int maxLineLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (maxLineLength <= 0)
                return text;

            // Normalize line breaks.
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');

            var sb = new StringBuilder(text.Length + 16);

            int lineLen = 0;
            int i = 0;

            while (i < text.Length)
            {
                char c = text[i];

                // Preserve existing newlines (paragraph boundaries).
                if (c == '\n')
                {
                    sb.Append('\n');
                    lineLen = 0;
                    i++;
                    continue;
                }

                // Skip leading whitespace (space/tabs/etc) but remember we saw it as a separator.
                bool hadWhitespace = false;
                while (i < text.Length)
                {
                    c = text[i];
                    if (c == '\n' || !char.IsWhiteSpace(c))
                        break;

                    hadWhitespace = true;
                    i++;
                }

                // If whitespace ended on a newline, let the newline handler run next loop.
                if (i >= text.Length)
                    break;

                if (text[i] == '\n')
                    continue;

                // Read next word token.
                int start = i;
                while (i < text.Length)
                {
                    c = text[i];
                    if (c == '\n' || char.IsWhiteSpace(c))
                        break;
                    i++;
                }

                int wordLen = i - start;
                if (wordLen <= 0)
                    continue;

                // If we are not at line start, we may need a separator before the word.
                if (lineLen > 0)
                {
                    // Prefer newline over space if it would overflow.
                    if (lineLen + 1 + wordLen > maxLineLength)
                    {
                        sb.Append('\n');
                        lineLen = 0;
                    }
                    else
                    {
                        // Collapse any whitespace to a single space.
                        if (hadWhitespace)
                        {
                            sb.Append(' ');
                            lineLen += 1;
                        }
                        else
                        {
                            // No whitespace in source (rare for normal prose). Keep no separator.
                        }
                    }
                }

                // If the word itself is too long, hard-break it.
                if (wordLen > maxLineLength && maxLineLength > 0)
                {
                    int remaining = wordLen;
                    int offset = start;

                    while (remaining > 0)
                    {
                        int take = remaining > maxLineLength ? maxLineLength : remaining;

                        // If we are mid-line and can't fit "take", start a new line.
                        if (lineLen > 0 && lineLen + take > maxLineLength)
                        {
                            sb.Append('\n');
                            lineLen = 0;
                        }

                        sb.Append(text, offset, take);
                        lineLen += take;

                        remaining -= take;
                        offset += take;

                        if (remaining > 0)
                        {
                            sb.Append('\n');
                            lineLen = 0;
                        }
                    }
                }
                else
                {
                    sb.Append(text, start, wordLen);
                    lineLen += wordLen;
                }
            }

            return sb.ToString();
        }
    }
}
