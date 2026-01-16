using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Retinues.Framework.Modules.Mods
{
    /// <summary>
    /// A mod "project": manifest + a set of files to write relative to the module root.
    /// </summary>
    public sealed class ModProject(ModManifest manifest)
    {
        public ModManifest Manifest { get; } =
            manifest ?? throw new ArgumentNullException(nameof(manifest));
        public List<ModFile> Files { get; } = [];

        public ModProject AddText(string relativePath, string content)
        {
            Files.Add(ModFile.Text(relativePath, content ?? string.Empty));
            return this;
        }

        public ModProject AddXml(string relativePath, XDocument doc)
        {
            Files.Add(ModFile.Xml(relativePath, doc));
            return this;
        }

        public ModProject AddBytes(string relativePath, byte[] bytes)
        {
            Files.Add(ModFile.Bytes(relativePath, bytes));
            return this;
        }
    }

    /// <summary>
    /// One output file in a mod project.
    /// </summary>
    public sealed class ModFile
    {
        public string RelativePath { get; }
        public ModFileKind Kind { get; }

        public string TextContent { get; }
        public XDocument XmlContent { get; }
        public byte[] ByteContent { get; }

        private ModFile(
            string relativePath,
            ModFileKind kind,
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

        public static ModFile Text(string relativePath, string content) =>
            new(relativePath, ModFileKind.Text, content ?? string.Empty, null, null);

        public static ModFile Xml(string relativePath, XDocument doc) =>
            new(relativePath, ModFileKind.Xml, null, doc, null);

        public static ModFile Bytes(string relativePath, byte[] bytes) =>
            new(relativePath, ModFileKind.Bytes, null, null, bytes ?? []);

        public string GetAbsolutePath(string moduleRoot) =>
            Path.Combine(moduleRoot ?? string.Empty, RelativePath ?? string.Empty);
    }

    public enum ModFileKind
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
