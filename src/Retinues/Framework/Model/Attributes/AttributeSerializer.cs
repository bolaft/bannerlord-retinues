using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Retinues.Framework.Model.Attributes
{
    /// <summary>
    /// File-local XML serialization helpers used by MAttribute only.
    /// This intentionally replaces the old Retinues.Utilities.Serialization.cs so the attribute system
    /// does not depend on a separate utility file.
    /// </summary>
    internal static class AttributeSerializer
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static readonly XmlWriterSettings WriterSettings = new()
        {
            Indent = true,
            OmitXmlDeclaration = true,
            NewLineHandling = NewLineHandling.None,
        };

        static readonly XmlReaderSettings ReaderSettings = new()
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
        };

        /// <summary>
        /// Serializes an object to an XML string.
        /// </summary>
        static string SerializeToString(object value, Type declaredType)
        {
            var serializer = CreateSerializer(declaredType);

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb, CultureInfo.InvariantCulture))
            using (var xw = XmlWriter.Create(sw, WriterSettings))
            {
                serializer.WriteObject(xw, value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Deserializes an object from an XML string.
        /// </summary>
        static object DeserializeFromString(string xml, Type declaredType)
        {
            var serializer = CreateSerializer(declaredType);

            using var sr = new StringReader(xml);
            using var xr = XmlReader.Create(sr, ReaderSettings);
            return serializer.ReadObject(xr);
        }

        /// <summary>
        /// Creates a DataContractSerializer for the given type.
        /// </summary>
        static DataContractSerializer CreateSerializer(Type type)
        {
            var settings = new DataContractSerializerSettings
            {
                MaxItemsInObjectGraph = int.MaxValue,
            };

            return new DataContractSerializer(type, settings);
        }
    }

    /// <summary>
    /// Holds serialized XML and provides pretty/compact formatting via ToString.
    /// </summary>
    internal sealed class XmlBlob(string xml)
    {
        readonly string _xml = xml ?? "";

        XDocument _doc;

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
    }
}
