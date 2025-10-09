using System.Collections.Generic;
using TaleWorlds.Library;

namespace Retinues.Troops
{
    /// <summary>
    /// Console cheats for troop XP. Allows adding XP to custom troops via command.
    /// </summary>
    public static class Cheats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Exports all custom troop roots to an XML file. Usage: retinues.export_custom_troops [fileName]
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("export_custom_troops", "retinues")]
        public static string ExportCustomTroops(List<string> args)
        {
            if (args.Count > 1)
                return "Usage: retinues.export_custom_troops [fileName]";
            
            var fileName = args.Count > 0 ? args[0] : "custom_troops.xml";
            var path = TroopImportExport.ExportAllToXml(fileName);
            if (path == null)
                return $"Export failed for '{fileName}'.";
            return $"Exported custom troops to '{path}'.";
        }

        /// <summary>
        /// Imports custom troop roots from an XML file. Usage: retinues.import_custom_troops [fileName]
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("import_custom_troops", "retinues")]
        public static string ImportCustomTroops(List<string> args)
        {
            if (args.Count != 1)
                return "Usage: retinues.import_custom_troops [fileName]";
            var fileName = args[0];
            int count = TroopImportExport.ImportFromXml(fileName);
            if (count == 0)
                return $"Import failed or no troops found in '{fileName}'.";
            return $"Imported {count} custom troop roots from '{fileName}'.";
        }
    }
}
