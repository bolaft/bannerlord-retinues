using System.Collections.Generic;
using TaleWorlds.Localization;
using static Retinues.Behaviors.Doctrines.Catalogs.FeatCatalog;

namespace Retinues.Behaviors.Doctrines.Catalogs
{
    /// <summary>
    /// Registers built-in doctrine categories and doctrines.
    /// </summary>
    public static partial class DoctrineCatalog
    {
        /// <summary>
        /// Defines data for a doctrine category.
        /// </summary>
        public class DoctrineCategoryData()
        {
            public string Id;
            public TextObject Name;
            public List<DoctrineData> Doctrines;
        }

        /// <summary>
        /// Defines data for a doctrine.
        /// </summary>
        public struct DoctrineData
        {
            public string Id;
            public TextObject Name;
            public TextObject Description;
            public string Sprite;
            public List<FeatData> Feats;
        }
    }
}
