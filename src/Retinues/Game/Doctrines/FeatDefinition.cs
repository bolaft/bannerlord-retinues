using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    public sealed class FeatDefinition(
        string id,
        TextObject name,
        TextObject description,
        int target
    )
    {
        public string Id { get; } = id ?? string.Empty;

        public TextObject Name { get; } = name;
        public TextObject Description { get; } = description;

        public int Target { get; } = target < 1 ? 1 : target;
    }
}
