using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Retinues.Game;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.ModuleManager;

namespace Retinues.Troops
{
    /// <summary>
    /// Export/import all defined custom troops (roots only) to/from a single XML file.
    /// </summary>
    [SafeClass]
    public static class TroopImportExport
    {
        const string ClanKey = "Clan";
        const string KingdomKey = "Kingdom";

        public static readonly string DefaultDir = Path.Combine(
            ModuleHelper.GetModuleFullPath("Retinues"),
            "Exports"
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Export                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Exports all current custom troop roots to an XML file.
        /// Returns the absolute path used (for logging/UX).
        /// </summary>
        public static string ExportAllToXml(string fileName)
        {
            string safeFileName = fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : fileName + ".xml";
            string filePath = Path.Combine(DefaultDir, safeFileName);

            Dictionary<string, FactionSaveData> payload = new()
            {
                { ClanKey, new FactionSaveData(Player.Clan) },
                { KingdomKey, new FactionSaveData(Player.Kingdom) },
            };

            var serializer = new XmlSerializer(
                typeof(Dictionary<string, FactionSaveData>),
                new XmlRootAttribute("Factions")
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

            return Path.GetFullPath(filePath);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Import                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Imports custom troop roots from an XML file and rebuilds their trees.
        /// Returns the number of root definitions imported.
        /// </summary>
        public static void ImportFromXml(string fileName)
        {
            string filePath = Path.Combine(DefaultDir, fileName);

            // If file doesn't exist and doesn't end with .xml, try appending .xml and check again
            if (
                !File.Exists(filePath)
                && !fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
            )
            {
                string xmlFileName = fileName + ".xml";
                string xmlFilePath = Path.Combine(DefaultDir, xmlFileName);
                if (File.Exists(xmlFilePath))
                    filePath = xmlFilePath;
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                Log.Message($"ImportFromXml: file not found '{filePath ?? "<null>"}'.");

            var serializer = new XmlSerializer(
                typeof(Dictionary<string, FactionSaveData>),
                new XmlRootAttribute("Troops")
            );
            Dictionary<string, FactionSaveData> payload;

            using (var fs = File.OpenRead(filePath))
            {
                payload = (Dictionary<string, FactionSaveData>)serializer.Deserialize(fs);
            }

            if (payload == null || payload.Count == 0)
                Log.Message($"ImportFromXml: no troops in '{filePath}'.");

            payload[ClanKey]?.Apply(Player.Clan);
            payload[KingdomKey]?.Apply(Player.Kingdom);

            Log.Message($"Imported and rebuilt root troop definitions from '{filePath}'.");
        }
    }
}
