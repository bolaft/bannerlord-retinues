using System;
using System.Linq;
using System.Xml.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;

namespace Retinues.Exports
{
    /// <summary>
    /// Applies character exports to an existing in-game troop.
    /// </summary>
    public static class CharacterImporter
    {
        /// <summary>
        /// Applies a serialized character export to the target troop, preserving existing upgrade targets.
        /// </summary>
        public static bool TryApplyCharacterExport(
            WCharacter target,
            CharacterExportEntry entry,
            out string error
        )
        {
            error = null;

            try
            {
                if (target == null)
                {
                    error = "target troop is null.";
                    return false;
                }

                if (entry == null || string.IsNullOrWhiteSpace(entry.PayloadXml))
                {
                    error = "missing export payload.";
                    return false;
                }

                var existing = target.UpgradeTargets?.ToList() ?? [];

                var rewritten = RewriteCharacterPayload(
                    entry.PayloadXml,
                    keepUpgradeTargets: false
                );
                rewritten = ForceCharacterIdentity(rewritten, target.StringId);

                target.Deserialize(rewritten);
                target.UpgradeTargets = existing;

                WCharacter.InvalidateTroopSourceCaches();

                Log.Debug(
                    $"Applied character export to '{target.StringId}' (export source='{entry.SourceId ?? "unknown"}')."
                );

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "CharacterImportService.TryApplyCharacterExport failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

        /// <summary>
        /// Rewrites serialized character XML payload, optionally removing upgrade target data.
        /// </summary>
        private static string RewriteCharacterPayload(string xml, bool keepUpgradeTargets)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return string.Empty;

            try
            {
                var el = XElement.Parse(xml, LoadOptions.None);

                if (!keepUpgradeTargets)
                {
                    var up = el.Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "UpgradeTargetsAttribute");
                    up?.Remove();
                }

                return el.ToString(SaveOptions.DisableFormatting);
            }
            catch
            {
                return xml ?? string.Empty;
            }
        }

        /// <summary>
        /// Ensures the serialized character payload uses the specified string id.
        /// </summary>
        private static string ForceCharacterIdentity(string xml, string forcedStringId)
        {
            if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(forcedStringId))
                return xml ?? string.Empty;

            try
            {
                var el = XElement.Parse(xml, LoadOptions.None);

                if (el.Attribute("stringId") != null)
                    el.SetAttributeValue("stringId", forcedStringId);

                return el.ToString(SaveOptions.DisableFormatting);
            }
            catch
            {
                return xml ?? string.Empty;
            }
        }
    }
}
