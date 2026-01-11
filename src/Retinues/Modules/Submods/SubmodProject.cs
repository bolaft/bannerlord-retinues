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
    public sealed class SubmodProject(SubmodManifest manifest)
    {
        public SubmodManifest Manifest { get; } =
            manifest ?? throw new ArgumentNullException(nameof(manifest));
        public List<SubmodFile> Files { get; } = [];

        public SubmodProject AddText(string relativePath, string content)
        {
            Files.Add(SubmodFile.Text(relativePath, content ?? string.Empty));
            return this;
        }

        public SubmodProject AddXml(string relativePath, XDocument doc)
        {
            Files.Add(SubmodFile.Xml(relativePath, doc));
            return this;
        }

        public SubmodProject AddBytes(string relativePath, byte[] bytes)
        {
            Files.Add(SubmodFile.Bytes(relativePath, bytes));
            return this;
        }
    }

    /// <summary>
    /// One output file in a submod project.
    /// </summary>
    public sealed class SubmodFile
    {
        public string RelativePath { get; }
        public SubmodFileKind Kind { get; }

        public string TextContent { get; }
        public XDocument XmlContent { get; }
        public byte[] ByteContent { get; }

        private SubmodFile(
            string relativePath,
            SubmodFileKind kind,
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

        public static SubmodFile Text(string relativePath, string content) =>
            new(relativePath, SubmodFileKind.Text, content ?? string.Empty, null, null);

        public static SubmodFile Xml(string relativePath, XDocument doc) =>
            new(relativePath, SubmodFileKind.Xml, null, doc, null);

        public static SubmodFile Bytes(string relativePath, byte[] bytes) =>
            new(relativePath, SubmodFileKind.Bytes, null, null, bytes ?? []);

        public string GetAbsolutePath(string moduleRoot) =>
            Path.Combine(moduleRoot ?? string.Empty, RelativePath ?? string.Empty);
    }

    public enum SubmodFileKind
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
