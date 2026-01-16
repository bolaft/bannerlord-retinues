using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Retinues.Modules.Submods
{
    /// <summary>
    /// A submod "project": manifest + a set of files to write relative to the module root.
    /// </summary>
    public sealed class SubModProject(SubModManifest manifest)
    {
        public SubModManifest Manifest { get; } =
            manifest ?? throw new ArgumentNullException(nameof(manifest));
        public List<SubModFile> Files { get; } = [];

        public SubModProject AddText(string relativePath, string content)
        {
            Files.Add(SubModFile.Text(relativePath, content ?? string.Empty));
            return this;
        }

        public SubModProject AddXml(string relativePath, XDocument doc)
        {
            Files.Add(SubModFile.Xml(relativePath, doc));
            return this;
        }

        public SubModProject AddBytes(string relativePath, byte[] bytes)
        {
            Files.Add(SubModFile.Bytes(relativePath, bytes));
            return this;
        }
    }

    /// <summary>
    /// One output file in a submod project.
    /// </summary>
    public sealed class SubModFile
    {
        public string RelativePath { get; }
        public SubModFileKind Kind { get; }

        public string TextContent { get; }
        public XDocument XmlContent { get; }
        public byte[] ByteContent { get; }

        private SubModFile(
            string relativePath,
            SubModFileKind kind,
            string text,
            XDocument xml,
            byte[] bytes
        )
        {
            RelativePath = relativePath ?? string.Empty;
            Kind = kind;

            TextContent = text;
            XmlContent = xml;
            ByteContent = bytes;
        }

        public static SubModFile Text(string relativePath, string content) =>
            new(relativePath, SubModFileKind.Text, content ?? string.Empty, null, null);

        public static SubModFile Xml(string relativePath, XDocument doc) =>
            new(relativePath, SubModFileKind.Xml, null, doc, null);

        public static SubModFile Bytes(string relativePath, byte[] bytes) =>
            new(relativePath, SubModFileKind.Bytes, null, null, bytes ?? []);

        public string GetAbsolutePath(string moduleRoot) =>
            Path.Combine(moduleRoot ?? string.Empty, RelativePath ?? string.Empty);
    }

    public enum SubModFileKind
    {
        Text,
        Xml,
        Bytes,
    }

    internal static class Utf8
    {
        public static readonly Encoding NoBom = new UTF8Encoding(false);
    }
}
