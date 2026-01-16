using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines.Catalogs
{
    /// <summary>
    /// Feat definitions for the Equipments category.
    /// </summary>
    public static partial class FeatCatalog
    {
        /// <summary>
        /// Defines data for a doctrine feat.
        /// </summary>
        public struct FeatData
        {
            public string Id;
            public TextObject Name;
            public TextObject Description;
            public int Target;
            public int Worth;
            public bool Repeatable;
        }
    }
}
