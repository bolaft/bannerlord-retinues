using System.Collections.Generic;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    public sealed class DoctrineCategoryDefinition(
        string id,
        TextObject name,
        TextObject description,
        IReadOnlyList<string> doctrineIds
    )
    {
        public string Id { get; } = id ?? string.Empty;
        public TextObject Name { get; } = name;
        public TextObject Description { get; } = description;

        // Ordered doctrine ids (index 0 is always available).
        public IReadOnlyList<string> DoctrineIds { get; } = doctrineIds ?? [];
    }
}
