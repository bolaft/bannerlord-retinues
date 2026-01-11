using System;

namespace Retinues.Game.Doctrines
{
    public readonly struct DoctrineFeatLink(string featId, int progress, bool required = true)
    {
        public readonly string FeatId = featId ?? string.Empty;
        public readonly int Progress = Math.Max(0, progress);
        public readonly bool Required = required;
    }
}
