using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Retinues.Framework.Model.Exports;
using Retinues.Utilities;

namespace Retinues.Editor.Services.Library.NPCCharacters
{
    public static class LibraryExportPayloadReader
    {
        public readonly struct ModelPayload(string payload, string modelStringId)
        {
            public string Payload { get; } = payload;
            public string ModelStringId { get; } = modelStringId;
        }

        public static bool TryExtractModelCharacterPayloads(
            MLibrary.Item item,
            out List<ModelPayload> payloads
        )
        {
            payloads = [];

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

                    var payload = MImportExport.ExtractPayload(el);
                    if (string.IsNullOrWhiteSpace(payload))
                        continue;

                    var modelStringId = TryGetModelStringId(el);

                    payloads.Add(new ModelPayload(payload, modelStringId));

                    // For pure character exports, only one payload is expected.
                    if (item.Kind == MLibraryKind.Character)
                        break;
                }

                return payloads.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Exception(
                    ex,
                    "LibraryExportPayloadReader.TryExtractModelCharacterPayloads failed."
                );
                return false;
            }
        }

        private static string TryGetModelStringId(XElement el)
        {
            if (el == null)
                return null;

            return (string)el.Attribute("stringId") ?? (string)el.Attribute("id");
        }
    }
}
