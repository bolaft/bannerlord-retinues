using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Retinues.Utilities
{
    /// <summary>
    /// Small XML helpers to avoid repeating parsing and writer boilerplate.
    /// Intentionally minimal: no behavior changes, only centralization.
    /// </summary>
    public static class XML
    {
        static readonly UTF8Encoding Utf8NoBom = new(false);

        public static bool TryParseRoot(string xml, out XElement root)
        {
            root = null;

            if (string.IsNullOrWhiteSpace(xml))
                return false;

            var trimmed = xml.TrimStart();
            if (!trimmed.StartsWith("<", StringComparison.Ordinal))
                return false;

            try
            {
                var doc = XDocument.Parse(xml, LoadOptions.None);
                root = doc.Root;
                return root != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryLoad(string path, out XDocument doc)
        {
            doc = null;

            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return false;

                doc = XDocument.Load(path, LoadOptions.None);
                return doc?.Root != null;
            }
            catch
            {
                return false;
            }
        }

        public static void WriteAllTextUtf8NoBom(string path, string content)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, content ?? string.Empty, Utf8NoBom);
        }

        public static void SaveDocumentUtf8NoBom(
            string path,
            XDocument doc,
            bool indent,
            bool omitXmlDeclaration
        )
        {
            if (string.IsNullOrWhiteSpace(path) || doc == null)
                return;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            using var fs = File.Create(path);
            using var xw = XmlWriter.Create(
                fs,
                new XmlWriterSettings
                {
                    Indent = indent,
                    OmitXmlDeclaration = omitXmlDeclaration,
                    Encoding = Utf8NoBom,
                }
            );

            doc.Save(xw);
        }
    }
}
