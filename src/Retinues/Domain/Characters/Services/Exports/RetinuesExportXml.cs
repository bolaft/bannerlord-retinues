// File: src/Retinues/Domain/Characters/Services/Exports/RetinuesExportXml.cs

using System;
using System.Xml.Linq;
using Retinues.Utilities;

namespace Retinues.Domain.Characters.Services.Exports
{
    /// <summary>
    /// Shared XML helpers for the Retinues export format.
    /// </summary>
    public static class RetinuesExportXml
    {
        internal const string RootName = "Retinues";
        internal const string RootVersion = "1";

        public static XElement BuildRoot(string kind, string sourceId)
        {
            var root = new XElement(RootName);
            root.SetAttributeValue("v", RootVersion);
            root.SetAttributeValue("kind", kind ?? string.Empty);
            root.SetAttributeValue("source", sourceId ?? string.Empty);
            root.SetAttributeValue("createdUtc", DateTime.UtcNow.ToString("o"));
            return root;
        }

        public static XDocument ToDocument(XElement root)
        {
            return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        }

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
