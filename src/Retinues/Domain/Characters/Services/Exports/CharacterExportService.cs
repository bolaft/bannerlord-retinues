// File: src/Retinues/Domain/Characters/Services/Exports/CharacterExportService.cs

using System.Xml.Linq;
using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Domain.Characters.Services.Exports
{
    /// <summary>
    /// Builds Retinues exports for characters.
    /// </summary>
    public static class CharacterExportService
    {
        public static bool TryBuildExport(WCharacter character, out XDocument doc, out string error)
        {
            doc = null;
            error = null;

            if (character == null)
            {
                error = "character is null.";
                return false;
            }

            if (character.IsHero)
            {
                error = "heroes cannot be exported.";
                return false;
            }

            var root = RetinuesExportXml.BuildRoot(kind: "character", sourceId: character.StringId);
            RetinuesExportXml.AddSerialized(root, character.UniqueId, character.SerializeAll());

            doc = RetinuesExportXml.ToDocument(root);
            return true;
        }
    }
}
