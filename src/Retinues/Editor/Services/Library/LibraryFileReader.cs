using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Retinues.Framework.Model.Exports;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.Editor.Services.Library
{
    public static class LibraryFileReader
    {
        public static bool TryReadTroopNames(MLibrary.Item item, out List<string> troopNames)
        {
            troopNames = [];

            try
            {
                if (item == null)
                    return false;

                if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
                    return false;

                var doc = XDocument.Load(item.FilePath, LoadOptions.None);
                var root = doc.Root;

                if (root == null || root.Name.LocalName != "Retinues")
                    return false;

                foreach (var el in root.Elements())
                {
                    if (!MImportExport.IsCharacterElement(el, loose: true))
                        continue;

                    var name = ReadCharacterName(el);
                    if (!string.IsNullOrWhiteSpace(name))
                        troopNames.Add(name);

                    // For pure character exports, only one payload is expected.
                    if (item.Kind == MLibraryKind.Character)
                        break;
                }

                troopNames = [.. troopNames.Where(s => !string.IsNullOrWhiteSpace(s))];
                return troopNames.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryFileReader.TryReadTroopNames failed.");
                return false;
            }
        }

        private static string ReadCharacterName(XElement el)
        {
            if (el == null)
                return string.Empty;

            // Prefer NameAttribute inner value if present.
            var nameEl = el.Elements().FirstOrDefault(x => x.Name.LocalName == "NameAttribute");
            if (nameEl != null)
            {
                var raw = (string)nameEl.Value ?? string.Empty;
                raw = ResolveTextObjectString(raw);
                if (!string.IsNullOrWhiteSpace(raw))
                    return raw;
            }

            // Fallback to raw attribute name if present (older formats).
            var attrName = (string)el.Attribute("name") ?? string.Empty;
            attrName = ResolveTextObjectString(attrName);

            return attrName ?? string.Empty;
        }

        public static string ResolveTextObjectString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            // If it's already a plain string (most common), keep it.
            // If it looks like a TextObject literal, evaluate it for the current locale.
            if (raw.StartsWith("{=", StringComparison.Ordinal) && raw.Contains("}"))
            {
                try
                {
                    return new TextObject(raw).ToString();
                }
                catch
                {
                    return raw;
                }
            }

            return raw;
        }
    }
}
