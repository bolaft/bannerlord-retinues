using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

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

        public struct FactionExportData
        {
            public FactionSaveData clanData;
            public FactionSaveData kingdomData;
        }

        /// <summary>
        /// Exports all current custom troop roots to an XML file.
        /// Returns the absolute path used (for logging/UX).
        /// </summary>
        public static string ExportCustomTroopsToXml(string fileName)
        {
            string safeFileName = fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : fileName + ".xml";
            string filePath = Path.Combine(DefaultDir, safeFileName);

            FactionExportData payload = new()
            {
                clanData = new FactionSaveData(Player.Clan),
                kingdomData = new FactionSaveData(Player.Kingdom),
            };

            var serializer = new XmlSerializer(
                typeof(FactionExportData),
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

        /// <summary>
        /// Exports all current culture troop roots to an XML file.
        /// </summary>
        public static string ExportCultureTroopsToXml(string fileName)
        {
            Log.Info("Exporting culture troops to XML...");

            string safeFileName = fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : fileName + ".xml";
            string filePath = Path.Combine(DefaultDir, safeFileName);

            List<FactionSaveData> payload = [];

            // Collect all base cultures
            var cultures =
                MBObjectManager
                    .Instance.GetObjectTypeList<CultureObject>()
                    ?.OrderBy(c => c?.Name?.ToString())
                    .ToList()
                ?? [];

            // Save each culture's troop data
            foreach (var culture in cultures)
                payload.Add(new FactionSaveData(new WCulture(culture)));

            var serializer = new XmlSerializer(
                typeof(List<FactionSaveData>),
                new XmlRootAttribute("Cultures")
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
        public static void ImportCustomTroopsFromXml(string fileName)
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
                typeof(FactionExportData),
                new XmlRootAttribute("Factions")
            );
            FactionExportData payload;

            using (var fs = File.OpenRead(filePath))
            {
                payload = (FactionExportData)serializer.Deserialize(fs);
            }

            payload.clanData?.Apply(Player.Clan);
            payload.kingdomData?.Apply(Player.Kingdom);

            Log.Message($"Imported and rebuilt root troop definitions from '{filePath}'.");
        }

        /// <summary>
        /// Imports culture troop roots from an XML file and rebuilds their trees.
        /// </summary>
        public static void ImportCultureTroopsFromXml(string fileName)
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
                typeof(List<FactionSaveData>),
                new XmlRootAttribute("Cultures")
            );

            List<FactionSaveData> payload;

            using (var fs = File.OpenRead(filePath))
            {
                payload = (List<FactionSaveData>)serializer.Deserialize(fs);
            }

            if (payload == null || payload.Count == 0)
                Log.Message($"ImportFromXml: no troops in '{filePath}'.");

            foreach (var f in payload)
                f.DeserializeTroops();

            Log.Message($"Imported and rebuilt culture troop definitions from '{filePath}'.");
        }
    }
}
