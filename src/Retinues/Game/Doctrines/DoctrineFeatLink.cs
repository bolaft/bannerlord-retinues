using System;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Links a feat to a doctrine, defining how much doctrine progress is awarded on each feat completion.
    /// </summary>
    public readonly struct DoctrineFeatLink(string featId, int worth)
    {
        public readonly string FeatId = featId ?? string.Empty;

        // Doctrine progress awarded when this feat completes (per completion).
        public readonly int Worth = Math.Max(0, worth);
    }
}
