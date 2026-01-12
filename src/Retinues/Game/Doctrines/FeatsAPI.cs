using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Framework.Runtime;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Public API for querying and mutating feat progress.
    /// </summary>
    [SafeClass]
    public static class FeatsAPI
    {
        public static IReadOnlyDictionary<string, FeatDefinition> Feats
        {
            get
            {
                DoctrinesCatalog.EnsureBuilt();
                return DoctrinesCatalog.Feats;
            }
        }

        public static int GetProgress(string featId)
        {
            var b = DoctrinesBehavior.Instance;
            return b != null ? b.GetFeatProgress(featId) : 0;
        }

        // "Completed" means one-time feat consumed.
        public static bool IsCompleted(string featId)
        {
            var b = DoctrinesBehavior.Instance;
            return b != null && b.IsFeatCompleted(featId);
        }

        public static int GetTimesCompleted(string featId)
        {
            var b = DoctrinesBehavior.Instance;
            return b != null ? b.GetFeatTimesCompleted(featId) : 0;
        }

        public static bool TryAddProgress(string featId, int amount, TextObject source = null)
        {
            if (!Settings.EnableDoctrines || !Settings.EnableFeatRequirements)
                return false;

            var b = DoctrinesBehavior.Instance;
            return b != null && b.TryAddFeatProgress(featId, amount, source);
        }

        public static bool TryReset(string featId)
        {
            if (!Settings.EnableDoctrines || !Settings.EnableFeatRequirements)
                return false;

            var b = DoctrinesBehavior.Instance;
            return b != null && b.TryResetFeat(featId);
        }

        public static bool TryComplete(string featId, TextObject source = null)
        {
            if (!Settings.EnableDoctrines || !Settings.EnableFeatRequirements)
                return false;

            var b = DoctrinesBehavior.Instance;
            return b != null && b.TryCompleteFeat(featId, source);
        }

        public static int TryCompleteMany(IReadOnlyList<string> featIds, TextObject source = null)
        {
            if (featIds == null || featIds.Count == 0)
                return 0;

            if (!Settings.EnableDoctrines || !Settings.EnableFeatRequirements)
                return 0;

            var b = DoctrinesBehavior.Instance;
            if (b == null)
                return 0;

            var done = 0;

            for (var i = 0; i < featIds.Count; i++)
            {
                var id = featIds[i];
                if (b.TryCompleteFeat(id, source))
                    done++;
            }

            return done;
        }
    }
}
