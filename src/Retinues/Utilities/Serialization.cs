using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Retinues.Utilities
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                  String KeyValue Serializer            //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Versioned, forward-compatible serializer for string dictionaries.
    /// </summary>
    [SafeClass]
    public static class Serialization
    {
        private const string HeaderV1 = "retinues:kv1";
        private const char PairSep = '=';
        private const string NullToken = "~";

        /// <summary>
        /// Serialize a dictionary into a compact, line-based, versioned string.
        /// </summary>
        public static string SerializeDictionary(Dictionary<string, string> data)
        {
            try
            {
                if (data == null || data.Count == 0)
                    return HeaderV1;

                var sb = new StringBuilder();
                sb.Append(HeaderV1);

                foreach (var kv in data.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    if (kv.Key == null)
                        throw new ArgumentException("Dictionary contains a null key.");

                    var k = EncodeBase64Url(kv.Key);
                    var v = kv.Value == null ? NullToken : EncodeBase64Url(kv.Value);

                    sb.Append('\n');
                    sb.Append(k);
                    sb.Append(PairSep);
                    sb.Append(v);
                }

                return sb.ToString();
            }
            catch
            {
                return HeaderV1;
            }
        }

        /// <summary>
        /// Serialize a list into a compact, line-based, versioned string.
        /// </summary>
        public static string SerializeList(List<string> data)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < data.Count; i++)
            {
                dict[$"item_{i}"] = data[i];
            }
            return SerializeDictionary(dict);
        }

        /// <summary>
        /// Deserialize a string produced by Serialize back into a dictionary.
        /// </summary>
        public static Dictionary<string, string> DeserializeDictionary(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                    return [];

                var dict = new Dictionary<string, string>(StringComparer.Ordinal);

                // Accept either Windows or Unix line breaks, keep deterministic behavior.
                var lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                if (lines.Length == 0)
                    return dict;

                var start = 0;
                if (string.Equals(lines[0].Trim(), HeaderV1, StringComparison.Ordinal))
                    start = 1;

                for (var i = start; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    var sep = line.IndexOf(PairSep);
                    if (sep < 0)
                        continue;

                    var kEnc = line.Substring(0, sep);
                    var vEnc = line.Substring(sep + 1);

                    var key = DecodeBase64Url(kEnc);
                    var value = vEnc == NullToken ? null : DecodeBase64Url(vEnc);

                    // Last write wins if duplicates exist.
                    dict[key] = value;
                }

                return dict;
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Deserialize a string produced by Serialize back into a list.
        /// </summary>
        public static List<string> DeserializeList(string data)
        {
            var dict = DeserializeDictionary(data);
            var list = new List<string>();

            int index = 0;
            while (true)
            {
                var key = $"item_{index}";
                if (!dict.TryGetValue(key, out string value))
                    break;

                list.Add(value);
                index++;
            }

            return list;
        }

        /* ━━━━━━━ Encoding ━━━━━━ */

        private static string EncodeBase64Url(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var bytes = Encoding.UTF8.GetBytes(text);
            var b64 = Convert.ToBase64String(bytes);

            // Base64Url: no padding, URL-safe alphabet.
            return b64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string DecodeBase64Url(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var b64 = text.Replace('-', '+').Replace('_', '/');

            // Restore padding.
            var mod = b64.Length % 4;
            if (mod != 0)
                b64 = b64 + new string('=', 4 - mod);

            var bytes = Convert.FromBase64String(b64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
