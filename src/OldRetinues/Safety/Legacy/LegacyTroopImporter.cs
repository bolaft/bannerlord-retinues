using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Retinues.Troops;
using Retinues.Utils;

namespace OldRetinues.Safety.Legacy
{
    /// <summary>
    /// Container for legacy troop save data in XML format.
    /// </summary>
    [XmlRoot("Troops")]
    public class LegacyTroopsContainer
    {
        [XmlElement("TroopSaveData")]
        public List<LegacyTroopSaveData> Troops { get; set; } = [];

        public LegacyTroopsContainer() { }
    }

    /// <summary>
    /// Handles the legacy (pre-unified) troop export format.
    /// </summary>
    [SafeClass]
    internal static class LegacyTroopImporter
    {
        private const string LegacyRoot = "Troops";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Queries                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Detects if a file is an old-style legacy export.
        /// </summary>
        public static bool IsLegacyExport(string absPath)
        {
            if (string.IsNullOrWhiteSpace(absPath) || !File.Exists(absPath))
                return false;

            try
            {
                using var fs = File.OpenRead(absPath);
                using var xr = XmlReader.Create(
                    fs,
                    new XmlReaderSettings { IgnoreComments = true }
                );

                while (xr.Read())
                {
                    if (xr.NodeType == XmlNodeType.Element)
                    {
                        if (xr.Name == LegacyRoot)
                        {
                            Log.Debug($"IsLegacyExport: detected legacy export '{absPath}'.");
                            return true;
                        }
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Debug($"IsLegacyExport: unable to probe '{absPath}': {e.Message}");
            }

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Load Package                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Loads a legacy export and converts it into a RetinuesTroopsPackage.
        /// The caller (unified import flow) then applies it using the same logic.
        /// </summary>
        public static TroopImportExport.RetinuesTroopsPackage LoadLegacyPackage(
            string fileNameOrPath
        )
        {
            try
            {
                var absPath = ResolvePath(fileNameOrPath);
                if (absPath == null)
                {
                    Log.Warn($"LoadLegacyPackage: file not found '{fileNameOrPath}'.");
                    return null;
                }

                var serializer = new XmlSerializer(typeof(LegacyTroopsContainer));

                LegacyTroopsContainer container;
                using (var fs = File.OpenRead(absPath))
                {
                    container = (LegacyTroopsContainer)serializer.Deserialize(fs);
                }

                var raw = container?.Troops ?? [];

                var (clanSaveData, kingdomSaveData) =
                    LegacyTroopSaveConverter.ConvertLegacyFactionData(raw);

                var pkg = new TroopImportExport.RetinuesTroopsPackage
                {
                    Factions = new TroopImportExport.FactionExportData
                    {
                        clanData = clanSaveData,
                        kingdomData = kingdomSaveData,
                    },
                };

                return pkg;
            }
            catch (Exception e)
            {
                Log.Exception(e, "LoadLegacyPackage failed");
                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string ResolvePath(string fileNameOrPath)
        {
            if (File.Exists(fileNameOrPath))
                return Path.GetFullPath(fileNameOrPath);

            var p = Path.Combine(TroopImportExport.DefaultDir, fileNameOrPath);
            if (File.Exists(p))
                return Path.GetFullPath(p);

            if (!p.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                p += ".xml";
                if (File.Exists(p))
                    return Path.GetFullPath(p);
            }

            return null;
        }
    }
}
