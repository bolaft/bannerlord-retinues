using System.Collections.Generic;
using Retinues.Domain.Factions.Wrappers;

namespace Retinues.Game.Retinues
{
    public partial class RetinuesBehavior
    {
        /// <summary>
        /// Snapshot of in-progress retinue unlocks (not yet unlocked).
        /// </summary>
        public static IEnumerable<(WCulture Culture, int Progress)> GetSnapshot()
        {
            if (Instance == null)
                yield break;

            for (int i = 0; i < Instance._progressCultureIds.Count; i++)
            {
                var cultureId = Instance._progressCultureIds[i];
                if (Instance.IsUnlocked(cultureId))
                    continue;

                var culture = WCulture.Get(cultureId);
                if (culture == null)
                    continue;

                if (i < 0 || i >= Instance._progressValues.Count)
                    continue;

                var progress = Instance._progressValues[i];
                if (progress <= 0)
                    continue;

                yield return (culture, progress);
            }
        }
    }
}
