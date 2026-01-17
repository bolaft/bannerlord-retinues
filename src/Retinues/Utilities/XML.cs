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

        /// <summary>
        /// Tries to parse the root XElement from an XML string.
        /// </summary>
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

        /// <summary>
        /// Saves an XDocument to the specified path as UTF-8 without BOM.
        /// </summary>
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
