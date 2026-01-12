using System.Collections.Generic;
using Retinues.Configuration;
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

    public sealed class DoctrineDefinition(
        string id,
        string categoryId,
        int indexInCategory,
        TextObject name,
        TextObject description,
        int goldCost,
        int influenceCost,
        IReadOnlyList<DoctrineFeatLink> feats
    )
    {
        public const int UnlockProgressTarget = 100;

        public string Id { get; } = id ?? string.Empty;
        public string CategoryId { get; } = categoryId ?? string.Empty;
        public int IndexInCategory { get; } = indexInCategory;

        public TextObject Name { get; } = name;
        public TextObject Description { get; } = description;

        // Always 100 for simplicity.
        public int ProgressTarget => UnlockProgressTarget;

        public int GoldCost { get; } = goldCost < 0 ? 0 : goldCost;
        public int InfluenceCost { get; } = influenceCost < 0 ? 0 : influenceCost;

        readonly IReadOnlyList<DoctrineFeatLink> _feats = feats;
        public IReadOnlyList<DoctrineFeatLink> Feats =>
            Settings.EnableFeatRequirements ? _feats ?? [] : [];
    }

    public sealed class FeatDefinition(
        string id,
        TextObject name,
        TextObject description,
        int target,
        bool repeatable = false
    )
    {
        public string Id { get; } = id ?? string.Empty;

        public TextObject Name { get; } = name;
        public TextObject Description { get; } = description;

        public int Target { get; } = target < 1 ? 1 : target;

        public bool Repeatable { get; } = repeatable;
    }
}
