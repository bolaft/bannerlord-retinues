using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Retinues.Modules.Submods
{
    /// <summary>
    /// Represents the contents of SubModule.xml for a generated submod.
    /// Keeps generation logic out of controllers and feature code.
    /// </summary>
    public sealed class SubmodManifest(string id, string name, string version)
    {
        public string Id { get; } = id ?? string.Empty;
        public string Name { get; } = name ?? string.Empty;
        public string Version { get; } = version ?? "v1.0.0";

        public bool IsDefaultModule { get; set; } = false;
        public bool IsSingleplayerModule { get; set; } = true;
        public bool IsMultiplayerModule { get; set; } = false;

        public List<SubmodXmlNode> XmlNodes { get; } = [];

        public XDocument ToXDocument()
        {
            // Matches the structure you already write today in LibraryController.
            var root = new XElement(
                "Module",
                new XElement("Name", new XAttribute("value", Name)),
                new XElement("Id", new XAttribute("value", Id)),
                new XElement("Version", new XAttribute("value", Version)),
                new XElement(
                    "DefaultModule",
                    new XAttribute("value", IsDefaultModule ? "true" : "false")
                ),
                new XElement(
                    "SingleplayerModule",
                    new XAttribute("value", IsSingleplayerModule ? "true" : "false")
                ),
                new XElement(
                    "MultiplayerModule",
                    new XAttribute("value", IsMultiplayerModule ? "true" : "false")
                )
            );

            if (XmlNodes.Count > 0)
            {
                var xmls = new XElement("Xmls");

                foreach (var node in XmlNodes)
                    xmls.Add(node.ToXElement());

                root.Add(xmls);
            }

            return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        }
    }

    /// <summary>
    /// One entry under &lt;Xmls&gt; in SubModule.xml.
    /// </summary>
    public sealed class SubmodXmlNode
    {
        public string XmlNameId { get; }
        public string Path { get; }
        public List<string> IncludedGameTypes { get; } = [];

        public SubmodXmlNode(string xmlNameId, string path, IEnumerable<string> includedGameTypes)
        {
            XmlNameId = xmlNameId ?? string.Empty;
            Path = path ?? string.Empty;

            if (includedGameTypes != null)
                IncludedGameTypes.AddRange(
                    includedGameTypes.Where(s => !string.IsNullOrWhiteSpace(s))
                );
        }

        public XElement ToXElement()
        {
            var node = new XElement(
                "XmlNode",
                new XElement(
                    "XmlName",
                    new XAttribute("id", XmlNameId),
                    new XAttribute("path", Path)
                )
            );

            if (IncludedGameTypes.Count > 0)
            {
                var types = new XElement("IncludedGameTypes");
                foreach (var t in IncludedGameTypes)
                    types.Add(new XElement("GameType", new XAttribute("value", t)));

                node.Add(types);
            }

            return node;
        }
    }
}
