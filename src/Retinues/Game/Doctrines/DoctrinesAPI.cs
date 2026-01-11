using System.Collections.Generic;
using Retinues.Framework.Runtime;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Public API for querying doctrine states and acquiring doctrines.
    /// </summary>
    [SafeClass]
    public static class DoctrinesAPI
    {
        public static IReadOnlyDictionary<string, DoctrineCategoryDefinition> Categories
        {
            get
            {
                DoctrinesCatalog.EnsureBuilt();
                return DoctrinesCatalog.Categories;
            }
        }

        public static IReadOnlyDictionary<string, DoctrineDefinition> Doctrines
        {
            get
            {
                DoctrinesCatalog.EnsureBuilt();
                return DoctrinesCatalog.Doctrines;
            }
        }

        public static DoctrineState GetState(string doctrineId)
        {
            var b = DoctrinesBehavior.Instance;
            return b != null ? b.GetDoctrineState(doctrineId) : DoctrineState.Locked;
        }

        public static int GetProgress(string doctrineId)
        {
            var b = DoctrinesBehavior.Instance;
            return b != null ? b.GetDoctrineProgress(doctrineId) : 0;
        }

        public static bool IsAcquired(string doctrineId)
        {
            var b = DoctrinesBehavior.Instance;
            return b != null && b.IsDoctrineAcquired(doctrineId);
        }

        public static bool TryAcquire(string doctrineId, out TextObject error)
        {
            var b = DoctrinesBehavior.Instance;
            if (b == null)
            {
                error = new TextObject("DoctrinesBehavior not available.");
                return false;
            }

            return b.TryAcquireDoctrine(doctrineId, out error);
        }
    }
}
