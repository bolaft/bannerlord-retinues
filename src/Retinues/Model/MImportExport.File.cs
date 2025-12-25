using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Retinues.Model
{
    public static partial class MImportExport
    {
        public sealed class File
        {
            public const int CurrentVersion = 1;

            public int Version { get; private set; } = CurrentVersion;
            public string Kind { get; private set; } // "character" | "faction"
            public string SourceId { get; private set; }
            public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

            public List<Entry> Entries { get; private set; } = [];

            public sealed class Entry
            {
                public string TypeName { get; set; }
                public string StringId { get; set; }
                public string PayloadXml { get; set; }
            }

            public static File Create(string kind, string sourceId, IEnumerable<Entry> entries)
            {
                if (string.IsNullOrWhiteSpace(kind))
                    throw new ArgumentException("kind cannot be null/empty.", nameof(kind));

                if (string.IsNullOrWhiteSpace(sourceId))
                    throw new ArgumentException("sourceId cannot be null/empty.", nameof(sourceId));

                return new File
                {
                    Kind = kind.Trim().ToLowerInvariant(),
                    SourceId = sourceId.Trim(),
                    CreatedUtc = DateTime.UtcNow,
                    Entries = entries?.Where(e => e != null).ToList() ?? [],
                };
            }

            public string ToXmlString()
            {
                var doc = new XDocument(
                    new XElement(
                        "RetinuesExport",
                        new XAttribute("v", Version),
                        new XAttribute("kind", Kind ?? string.Empty),
                        new XAttribute("source", SourceId ?? string.Empty),
                        new XAttribute("createdUtc", CreatedUtc.ToString("o")),
                        new XElement(
                            "Entries",
                            Entries.Select(e => new XElement(
                                "Entry",
                                new XAttribute("type", e.TypeName ?? string.Empty),
                                new XAttribute("id", e.StringId ?? string.Empty),
                                new XElement("Payload", new XCData(e.PayloadXml ?? string.Empty))
                            ))
                        )
                    )
                );

                using var sw = new StringWriter();
                doc.Save(sw);
                return sw.ToString();
            }

            public static File FromXmlString(string xml)
            {
                if (string.IsNullOrWhiteSpace(xml))
                    throw new ArgumentException("xml cannot be null/empty.", nameof(xml));

                var doc = XDocument.Parse(xml);
                var root = doc.Root;
                if (root == null || root.Name.LocalName != "RetinuesExport")
                    throw new InvalidDataException(
                        "Invalid export file: missing RetinuesExport root."
                    );

                int.TryParse(root.Attribute("v")?.Value, out var v);

                var kind = root.Attribute("kind")?.Value ?? string.Empty;
                var source = root.Attribute("source")?.Value ?? string.Empty;

                var createdUtc = DateTime.UtcNow;
                var createdAttr = root.Attribute("createdUtc")?.Value;
                if (!string.IsNullOrWhiteSpace(createdAttr))
                    DateTime.TryParse(
                        createdAttr,
                        null,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out createdUtc
                    );

                var entries = new List<Entry>();
                var entriesEl = root.Element("Entries");
                if (entriesEl != null)
                {
                    foreach (var e in entriesEl.Elements("Entry"))
                    {
                        entries.Add(
                            new Entry
                            {
                                TypeName = e.Attribute("type")?.Value ?? string.Empty,
                                StringId = e.Attribute("id")?.Value ?? string.Empty,
                                PayloadXml = e.Element("Payload")?.Value ?? string.Empty,
                            }
                        );
                    }
                }

                return new File
                {
                    Version = v,
                    Kind = kind.Trim().ToLowerInvariant(),
                    SourceId = source,
                    CreatedUtc = createdUtc,
                    Entries = entries,
                };
            }

            public static void SaveToPath(string path, File file)
            {
                if (file == null)
                    throw new ArgumentNullException(nameof(file));

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                System.IO.File.WriteAllText(path, file.ToXmlString(), new UTF8Encoding(false));
            }

            public static File LoadFromPath(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("path cannot be null/empty.", nameof(path));

                var xml = System.IO.File.ReadAllText(path, Encoding.UTF8);
                return FromXmlString(xml);
            }
        }
    }
}
