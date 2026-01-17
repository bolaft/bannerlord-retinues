using System;
using System.Xml.Linq;

namespace Retinues.Exports
{
    /// <summary>
    /// Shared XML helpers for the Retinues export format.
    /// </summary>
    public static class ExportXML
    {
        /// <summary>
        /// Root element name for export files.
        /// </summary>
        internal const string RootName = "Retinues";

        /// <summary>
        /// Supported export format version.
        /// </summary>
        internal const string RootVersion = "1";

        /// <summary>
        /// Builds a root XElement for an export file with metadata attributes.
        /// </summary>
        public static XElement BuildRoot(string kind, string sourceId)
        {
            var root = new XElement(RootName);
            root.SetAttributeValue("v", RootVersion);
            root.SetAttributeValue("kind", kind ?? string.Empty);
            root.SetAttributeValue("source", sourceId ?? string.Empty);
            root.SetAttributeValue("createdUtc", DateTime.UtcNow.ToString("o"));
            return root;
        }

        /// <summary>
        /// Wraps the provided root XElement in an XDocument with declaration.
        /// </summary>
        public static XDocument ToDocument(XElement root)
        {
            return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        }

        /// <summary>
        /// Adds a serialized payload under the root, using an element or CDATA Entry as needed.
        /// </summary>
        public static XElement AddSerialized(XElement root, string uid, string serialized)
        {
            if (root == null || string.IsNullOrWhiteSpace(serialized))
                return null;

            var trimmed = serialized.TrimStart();

            if (trimmed.StartsWith("<"))
            {
                try
                {
                    var el = XElement.Parse(serialized, LoadOptions.None);

                    if (!string.IsNullOrWhiteSpace(uid))
                        el.SetAttributeValue("uid", uid);

                    root.Add(el);
                    return el;
                }
                catch
                {
                    // Fall through to <Entry>.
                }
            }

            var entry = new XElement(
                "Entry",
                new XAttribute("uid", uid ?? string.Empty),
                new XCData(serialized)
            );

            root.Add(entry);
            return entry;
        }
    }
}
