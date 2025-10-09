using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Retinues.Troops.Persistence;
using Retinues.Utils;

namespace Retinues.Troops
{
    /// <summary>
    /// Export/import all defined custom troops (roots only) to/from a single XML file.
    /// </summary>
    [SafeClass]
    public static class TroopImportExport
    {
        static readonly string DocumentsDir = Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments
        );
        static readonly string DefaultDir = Path.Combine(DocumentsDir, "Retinues");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Export                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Exports all current custom troop roots to an XML file.
        /// Returns the absolute path used (for logging/UX).
        /// </summary>
        public static string ExportAllToXml(string fileName)
        {
            try
            {
                string safeFileName = fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                    ? fileName
                    : fileName + ".xml";
                string filePath = Path.Combine(DefaultDir, safeFileName);

                var payload = TroopBehavior.CollectAllDefinedCustomTroops();

                var serializer = new XmlSerializer(
                    typeof(List<TroopSaveData>),
                    new XmlRootAttribute("Troops")
                );
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                };

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
                using (var fs = File.Create(filePath))
                using (var writer = XmlWriter.Create(fs, settings))
                {
                    serializer.Serialize(writer, payload);
                }

                Log.Info($"Exported {payload.Count} root troop definitions to '{filePath}'.");
                return Path.GetFullPath(filePath);
            }
            catch (Exception e)
            {
                Log.Exception(e, "ExportAllToXml failed");
                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Import                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Imports custom troop roots from an XML file and rebuilds their trees.
        /// Returns the number of root definitions imported.
        /// </summary>
        public static int ImportFromXml(string fileName)
        {
            try
            {
                string filePath = Path.Combine(DefaultDir, fileName);

                // If file doesn't exist and doesn't end with .xml, try appending .xml and check again
                if (!File.Exists(filePath) && !fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    string xmlFileName = fileName + ".xml";
                    string xmlFilePath = Path.Combine(DefaultDir, xmlFileName);
                    if (File.Exists(xmlFilePath))
                    {
                        filePath = xmlFilePath;
                    }
                }

                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    Log.Warn($"ImportFromXml: file not found '{filePath ?? "<null>"}'.");
                    return 0;
                }

                var serializer = new XmlSerializer(
                    typeof(List<TroopSaveData>),
                    new XmlRootAttribute("Troops")
                );
                List<TroopSaveData> payload;

                using (var fs = File.OpenRead(filePath))
                {
                    payload = (List<TroopSaveData>)serializer.Deserialize(fs);
                }

                if (payload == null || payload.Count == 0)
                {
                    Log.Warn($"ImportFromXml: no troops in '{filePath}'.");
                    return 0;
                }

                int built = 0;
                foreach (var root in payload)
                {
                    // Rebuild each tree via the existing loader
                    TroopLoader.Load(root);
                    built++;
                }

                Log.Info($"Imported and rebuilt {built} root troop definitions from '{filePath}'.");
                return built;
            }
            catch (Exception e)
            {
                Log.Exception(e, "ImportFromXml failed");
                return 0;
            }
        }
    }
}
