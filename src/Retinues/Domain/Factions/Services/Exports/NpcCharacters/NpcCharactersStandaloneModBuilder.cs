// File: src/Retinues/Domain/Factions/Services/Exports/NpcCharacters/NpcCharactersStandaloneModBuilder.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Retinues.Framework.Modules.Mods;

namespace Retinues.Domain.Factions.Services.Exports.NpcCharacters
{
    /// <summary>
    /// Builds a standalone submod project from NPCCharacter elements.
    /// </summary>
    public static class NpcCharactersStandaloneModBuilder
    {
        public static ModProject BuildNpcCharactersModProject(
            string moduleId,
            List<string> npcElements,
            List<string> npcIds
        )
        {
            moduleId ??= string.Empty;
            npcElements ??= [];
            npcIds ??= [];

            var manifest = new ModManifest(moduleId, moduleId, "v1.0.0");

            manifest.XmlNodes.Add(
                new ModeXmlNode(
                    xmlNameId: "NPCCharacters",
                    path: "ModuleData/spnpccharacters.xml",
                    includedGameTypes: ["Campaign", "CampaignStoryMode", "CustomGame"]
                )
            );

            var project = new ModProject(manifest);

            project.AddXml(
                "ModuleData/spnpccharacters.xml",
                BuildNpcCharactersDocument(npcElements)
            );
            project.AddText("ModuleData/spnpccharacters.xslt", BuildSpNpcCharactersXslt(npcIds));

            return project;
        }

        private static XDocument BuildNpcCharactersDocument(List<string> npcElements)
        {
            npcElements ??= [];

            var root = new XElement("NPCCharacters");

            foreach (var s in npcElements)
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;

                root.Add(XElement.Parse(s));
            }

            return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        }

        private static string BuildSpNpcCharactersXslt(List<string> npcIds)
        {
            npcIds ??= [];

            var ids = npcIds
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine(
                "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">"
            );
            sb.AppendLine();
            sb.AppendLine("  <xsl:output method=\"xml\" indent=\"yes\"/>");
            sb.AppendLine();
            sb.AppendLine("  <!-- Identity transform -->");
            sb.AppendLine("  <xsl:template match=\"@*|node()\">");
            sb.AppendLine("    <xsl:copy>");
            sb.AppendLine("      <xsl:apply-templates select=\"@*|node()\"/>");
            sb.AppendLine("    </xsl:copy>");
            sb.AppendLine("  </xsl:template>");
            sb.AppendLine();

            foreach (var id in ids)
            {
                var safeId = id.Replace("\"", "");
                sb.AppendLine($"  <xsl:template match='NPCCharacter[@id=\"{safeId}\"]'/>");
            }

            sb.AppendLine("</xsl:stylesheet>");

            return sb.ToString();
        }
    }
}
