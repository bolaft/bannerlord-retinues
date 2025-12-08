using System.Collections.Generic;

namespace Retinues.Model.Characters
{
    internal static class UpgradeCache
    {
        private static readonly Dictionary<WCharacter, List<WCharacter>> _sources = [];
        private static bool _built;

        private static readonly IReadOnlyList<WCharacter> _empty = [];

        private static void EnsureBuilt()
        {
            if (_built)
                return;

            _sources.Clear();

            // Ensure all characters are wrapped once and build child -> parents map.
            foreach (var parent in WCharacter.All)
            {
                var targets = parent.UpgradeTargets;
                if (targets == null || targets.Count == 0)
                    continue;

                for (int i = 0; i < targets.Count; i++)
                {
                    var child = targets[i];
                    if (child == null)
                        continue;

                    if (!_sources.TryGetValue(child, out var list))
                    {
                        list = [];
                        _sources[child] = list;
                    }

                    if (!list.Contains(parent))
                        list.Add(parent);
                }
            }

            _built = true;
        }

        public static IReadOnlyList<WCharacter> GetSources(WCharacter character)
        {
            if (character == null)
                return _empty;

            EnsureBuilt();

            if (_sources.TryGetValue(character, out var list))
                return list;

            return _empty;
        }

        /// <summary>
        /// Recomputes the upgrade map after a change in UpgradeTargets.
        /// For now this is a global rebuild; we can optimize it to per-line if needed.
        /// </summary>
        public static void Recompute(WCharacter changed)
        {
            _built = false;
            _sources.Clear();
            EnsureBuilt();
        }
    }
}
