using System.Collections.Generic;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    public sealed class DoctrineDefinition(
        string id,
        string categoryId,
        int indexInCategory,
        TextObject name,
        TextObject description,
        int progressTarget,
        int goldCost,
        int influenceCost,
        IReadOnlyList<DoctrineFeatLink> feats
    )
    {
        public string Id { get; } = id ?? string.Empty;
        public string CategoryId { get; } = categoryId ?? string.Empty;
        public int IndexInCategory { get; } = indexInCategory;

        public TextObject Name { get; } = name;
        public TextObject Description { get; } = description;

        public int ProgressTarget { get; } = progressTarget < 1 ? 1 : progressTarget;
        public int GoldCost { get; } = goldCost < 0 ? 0 : goldCost;
        public int InfluenceCost { get; } = influenceCost < 0 ? 0 : influenceCost;

        public IReadOnlyList<DoctrineFeatLink> Feats { get; } = feats ?? [];
    }
}
