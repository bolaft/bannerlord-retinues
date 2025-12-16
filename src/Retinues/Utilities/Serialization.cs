using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Retinues.Utilities
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                 Generic XML Serializer                 //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Generic XML serializer for common CLR types (dicts, lists, primitives, and POCOs).
    /// </summary>
    [SafeClass]
    public static class Serialization
    {
        /* ━━━━━━━ Public API ━━━━━━ */

        /// <summary>
        /// Serializes a value into an XML blob. XmlBlob.ToString() returns a pretty form.
        /// XmlBlob.Compact returns a compact form (recommended for persistence).
        /// </summary>
        public static XmlBlob Serialize<T>(T value)
        {
            try
            {
                var xml = SerializeToString(value, typeof(T));
                return new XmlBlob(xml);
            }
            catch
            {
                return new XmlBlob("");
            }
        }

        /// <summary>
        /// Deserializes an XML string into a value.
        /// Returns default(T) on failure or empty input.
        /// </summary>
        public static T Deserialize<T>(string xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                    return default;

                return (T)DeserializeFromString(xml, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Alias for Deserialize.
        /// </summary>
        public static T Unserialize<T>(string xml)
        {
            return Deserialize<T>(xml);
        }

        /* ━━━━━━━ Compatibility Shims ━━━━━━ */

        /// <summary>
        /// Legacy helper: serialize a string dictionary to an XML string.
        /// </summary>
        public static string SerializeDictionary(Dictionary<string, string> data)
        {
            // Keep it compact for storage.
            return Serialize(data).Compact;
        }

        /// <summary>
        /// Legacy helper: serialize a list of strings to an XML string.
        /// </summary>
        public static string SerializeList(List<string> data)
        {
            return Serialize(data).Compact;
        }

        /// <summary>
        /// Legacy helper: deserialize an XML string back into a string dictionary.
        /// Returns an empty dictionary on failure.
        /// </summary>
        public static Dictionary<string, string> DeserializeDictionary(string data)
        {
            var result = Deserialize<Dictionary<string, string>>(data);
            return result ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Legacy helper: deserialize an XML string back into a list of strings.
        /// Returns an empty list on failure.
        /// </summary>
        public static List<string> DeserializeList(string data)
        {
            var result = Deserialize<List<string>>(data);
            return result ?? new List<string>();
        }

        /* ━━━━━━━ Internals ━━━━━━ */

        private static string SerializeToString(object value, Type declaredType)
        {
            // Note: OmitXmlDeclaration avoids "utf-16" declarations from StringWriter.
            // For persistence, Compact is usually best, but we keep output readable by default.
            var serializer = CreateSerializer(declaredType);

            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
                NewLineHandling = NewLineHandling.None,
            };

            using (var sw = new StringWriter(sb, CultureInfo.InvariantCulture))
            using (var xw = XmlWriter.Create(sw, settings))
            {
                serializer.WriteObject(xw, value);
            }

            return sb.ToString();
        }

        private static object DeserializeFromString(string xml, Type declaredType)
        {
            var serializer = CreateSerializer(declaredType);

            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
            };

            using (var sr = new StringReader(xml))
            using (var xr = XmlReader.Create(sr, settings))
            {
                return serializer.ReadObject(xr);
            }
        }

        private static DataContractSerializer CreateSerializer(Type type)
        {
            // MaxItemsInObjectGraph: avoid unexpected truncation on larger graphs.
            var settings = new DataContractSerializerSettings
            {
                MaxItemsInObjectGraph = int.MaxValue,
            };

            return new DataContractSerializer(type, settings);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                        XML Blob                        //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Holds serialized XML and provides pretty/compact formatting via ToString.
    /// </summary>
    public sealed class XmlBlob(string xml)
    {
        private readonly string _xml = xml ?? "";
        private XDocument _doc;

        /// <summary>
        /// The raw XML string as produced by the serializer (usually indented).
        /// </summary>
        public string Xml => _xml;

        /// <summary>
        /// Compact XML for persistence (no whitespace formatting).
        /// </summary>
        public string Compact
        {
            get
            {
                try
                {
                    var doc = Document;
                    return doc == null ? "" : doc.ToString(SaveOptions.DisableFormatting);
                }
                catch
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Parsed XDocument (lazy). Useful if you want to inspect or edit the XML.
        /// </summary>
        public XDocument Document
        {
            get
            {
                try
                {
                    if (_doc != null)
                        return _doc;

                    if (string.IsNullOrWhiteSpace(_xml))
                        return null;

                    _doc = XDocument.Parse(_xml, LoadOptions.None);
                    return _doc;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Pretty printed XML string.
        /// </summary>
        public override string ToString()
        {
            try
            {
                var doc = Document;
                return doc == null ? "" : doc.ToString();
            }
            catch
            {
                return "";
            }
        }
    }
}
