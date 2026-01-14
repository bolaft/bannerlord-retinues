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

        public static bool CanProgress(string featId)
        {
            var b = DoctrinesBehavior.Instance;
            return b != null && b.CanProgressFeat(featId);
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

        public static bool TryAddProgress(string featId, int amount)
        {
            if (amount <= 0)
                return false;

            if (!Settings.EnableDoctrines || !Settings.EnableFeatRequirements)
                return false;

            var b = DoctrinesBehavior.Instance;
            return b != null && b.TryAddFeatProgress(featId, amount);
        }

        public static bool TryReset(string featId)
        {
            if (!Settings.EnableDoctrines || !Settings.EnableFeatRequirements)
                return false;

            var b = DoctrinesBehavior.Instance;
            return b != null && b.TryResetFeat(featId);
        }

        public static bool TrySet(string featId, int amount)
        {
            if (amount < 0)
                return false;

            if (!Settings.EnableDoctrines || !Settings.EnableFeatRequirements)
                return false;

            var b = DoctrinesBehavior.Instance;
            return b != null && b.TrySetFeatProgress(featId, amount);
        }

        public static bool TryComplete(string featId)
        {
            if (!Settings.EnableDoctrines || !Settings.EnableFeatRequirements)
                return false;

            var b = DoctrinesBehavior.Instance;
            return b != null && b.TryCompleteFeat(featId);
        }
    }
}
