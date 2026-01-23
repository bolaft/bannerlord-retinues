using System;
using System.Collections.Generic;
using Retinues.Interface.Services;
using Retinues.Settings;
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
            public Func<bool> Overridden;
            public TextObject OverriddenHint;
            public List<FeatData> Feats;
        }

        /// <summary>
        /// Generates an overridden hint based on the given option.
        /// </summary>
        private static TextObject OverriddenByOption(IOption option)
        {
            var optionName = option?.Name ?? string.Empty;

            return L.T("doctrine_is_overridden", "The option '{OPTION}' is disabled.")
                .SetTextVariable("OPTION", optionName);
        }
    }
}
