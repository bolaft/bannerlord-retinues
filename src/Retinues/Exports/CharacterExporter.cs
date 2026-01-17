using System.Xml.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.GUI.Services;

namespace Retinues.Exports
{
    /// <summary>
    /// Builds Retinues exports for characters.
    /// </summary>
    public static class CharacterExporter
    {
        /// <summary>
        /// Attempts to build an export XDocument for the given character, returning success and an error message if any.
        /// </summary>
        public static bool TryBuildExport(WCharacter character, out XDocument doc, out string error)
        {
            doc = null;
            error = null;

            if (character == null)
            {
                error = L.S("character_is_null", "Character is null.");
                return false;
            }

            if (character.IsHero)
            {
                error = L.S("heroes_cannot_be_exported", "Heroes cannot be exported.");
                return false;
            }

            var root = ExportXML.BuildRoot(kind: "character", sourceId: character.StringId);
            ExportXML.AddSerialized(root, character.UniqueId, character.SerializeAll());

            doc = ExportXML.ToDocument(root);
            return true;
        }
    }
}
